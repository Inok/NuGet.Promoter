using System.IO;
using System.Text;
using CSharpFunctionalExtensions;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Packaging.Licenses;
using Promote.NuGet.Commands.Promote.Resolution;
using Promote.NuGet.Feeds;

namespace Promote.NuGet.Commands.Licensing;

public class LicenseComplianceValidator
{
    private readonly INuGetRepository _repository;
    private readonly ILicenseComplianceValidatorLogger _logger;

    public LicenseComplianceValidator(INuGetRepository repository, ILicenseComplianceValidatorLogger logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result> CheckCompliance(
        IReadOnlyCollection<PackageInfo> packages,
        LicenseComplianceSettings settings,
        CancellationToken cancellationToken)
    {
        if (!settings.Enabled)
        {
            _logger.LogComplianceChecksDisabled();
            return Result.Success();
        }

        _logger.LogCheckingLicenseCompliance();

        var violations = new List<LicenseComplianceViolation>();

        foreach (var package in packages)
        {
            _logger.LogCheckingLicenseComplianceForPackage(package.Id);

            using var resource = await _repository.Packages.GetPackageResource(package.Id, cancellationToken);

            if (resource.PackageReader == null)
            {
                _logger.LogFailedToDownloadPackage(package.Id);
                return Result.Failure("Failed to download package.");
            }

            var nuspecReader = resource.PackageReader.NuspecReader;

            if (nuspecReader.GetLicenseMetadata() is { } licenseMetadata)
            {
                await CheckLicenseMetadata(package, settings, licenseMetadata, resource.PackageReader, violations, cancellationToken);
            }
            else if (nuspecReader.GetLicenseUrl() is { } licenseUrl)
            {
                CheckLicenseUrl(package, settings, licenseUrl, violations);
            }
            else
            {
                var violation = new LicenseComplianceViolation(package.Id, PackageLicenseType.None, "<not set>", "License in not configured for the package.");
                _logger.LogLicenseViolation(violation);
                violations.Add(violation);
            }
        }

        if (violations.Count == 0)
        {
            _logger.LogNoLicenseViolations();
            return Result.Success();
        }

        _logger.LogLicenseViolationsSummary(violations);
        return Result.Failure("License violations found.");
    }

    private void CheckLicenseUrl(PackageInfo package,
                                 LicenseComplianceSettings settings,
                                 string licenseUrl,
                                 List<LicenseComplianceViolation> violations)
    {
        _logger.LogPackageLicense(PackageLicenseType.Url, licenseUrl);

        var isLicenseWhitelisted = settings.AcceptUrls.Any(x => string.Equals(x, licenseUrl, StringComparison.Ordinal));
        if (isLicenseWhitelisted)
        {
            _logger.LogLicenseCompliance("The license url is in whitelist.");
            return;
        }

        var violation = new LicenseComplianceViolation(package.Id, PackageLicenseType.Url, licenseUrl, "The license url is not whitelisted.");
        _logger.LogLicenseViolation(violation);
        violations.Add(violation);
    }

    private async Task CheckLicenseMetadata(PackageInfo package,
                                            LicenseComplianceSettings settings,
                                            LicenseMetadata licenseMetadata,
                                            PackageReaderBase packageReader,
                                            List<LicenseComplianceViolation> violations,
                                            CancellationToken cancellationToken)
    {
        var licenseType = licenseMetadata.Type switch
        {
            LicenseType.File       => PackageLicenseType.File,
            LicenseType.Expression => PackageLicenseType.Expression,
            _                      => throw new InvalidOperationException(),
        };

        _logger.LogPackageLicense(licenseType, licenseMetadata.License, licenseMetadata.WarningsAndErrors);

        if (licenseMetadata.Type == LicenseType.Expression)
        {
            var result = CheckExpressionMatches(package.Id, licenseMetadata.License, settings.AcceptExpressions);
            if (result.IsSuccess)
            {
                _logger.LogLicenseCompliance(result.Value);
            }
            else
            {
                _logger.LogLicenseViolation(result.Error);
                violations.Add(result.Error);
            }

            return;
        }

        if (licenseMetadata.Type == LicenseType.File)
        {
            var result = await CheckFileMatches(package.Id, licenseMetadata.License, settings.AcceptFiles, packageReader, cancellationToken);
            if (result.IsSuccess)
            {
                _logger.LogLicenseCompliance(result.Value);
            }
            else
            {
                _logger.LogLicenseViolation(result.Error);
                violations.Add(result.Error);
            }

            return;
        }

        throw new InvalidOperationException("Unreachable.");
    }

    private Result<string, LicenseComplianceViolation> CheckExpressionMatches(
        PackageIdentity packageId,
        string license,
        IReadOnlyCollection<string> acceptedExpressions)
    {
        var isExpressionWhitelisted = acceptedExpressions.Contains(license);
        if (isExpressionWhitelisted)
        {
            return "The license expression is in whitelist.";
        }

        return new LicenseComplianceViolation(packageId, PackageLicenseType.Expression, license, "The license expression is not whitelisted.");
    }

    private async Task<Result<string, LicenseComplianceViolation>> CheckFileMatches(
        PackageIdentity packageId,
        string license,
        IReadOnlyCollection<string> acceptedFiles,
        PackageReaderBase packageReader,
        CancellationToken cancellationToken)
    {
        var filesInPackage = await packageReader.GetFilesAsync(cancellationToken);
        if (!filesInPackage.Contains(license))
        {
            return new LicenseComplianceViolation(packageId, PackageLicenseType.File, license, "There is no such file in the package.");
        }

        string actualLicenseText;
        try
        {
            await using var stream = await packageReader.GetStreamAsync(license, cancellationToken);
            using var reader = new StreamReader(stream);
            actualLicenseText = await reader.ReadToEndAsync();
        }
        catch (Exception ex)
        {
            return new LicenseComplianceViolation(packageId, PackageLicenseType.File, license, $"Failed to open the license file: {ex.Message}");
        }

        var normalizedActualLicense = NormalizeLicenseText(actualLicenseText);

        foreach (var acceptFile in acceptedFiles)
        {
            string acceptedText;
            try
            {
                acceptedText = await File.ReadAllTextAsync(acceptFile, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogAcceptedLicenseFileReadFailure(acceptFile, ex);
                continue;
            }

            var normalizedAcceptedText = NormalizeLicenseText(acceptedText);

            if (normalizedAcceptedText.Equals(normalizedActualLicense))
            {
                return $"Matching accepted license file found: {Path.GetFileName(acceptFile)}.";
            }
        }

        return new LicenseComplianceViolation(packageId, PackageLicenseType.File, license, "No matching license files found in the whitelist.");
    }

    private string NormalizeLicenseText(string license)
    {
        var normalized = new StringBuilder(license);

        normalized.Replace("\r\n", " ");
        normalized.Replace("\r", " ");
        normalized.Replace("\n", " ");

        for (var i = 0; i < normalized.Length; i++)
        {
            var ch = normalized[i];
            if (char.IsWhiteSpace(ch))
            {
                normalized[i] = ' ';
                continue;
            }

            if (char.IsUpper(ch))
            {
                normalized[i] = char.ToLowerInvariant(ch);
            }
        }

        int lengthBefore;
        do
        {
            lengthBefore = normalized.Length;
            normalized.Replace("  ", " ");
        }
        while (lengthBefore != normalized.Length);

        return normalized.ToString();
    }
}
