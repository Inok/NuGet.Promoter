using CSharpFunctionalExtensions;
using NuGet.Packaging;
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
                CheckLicenseMetadata(package, settings, licenseMetadata, violations);
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

    private void CheckLicenseMetadata(PackageInfo package,
                                      LicenseComplianceSettings settings,
                                      LicenseMetadata licenseMetadata,
                                      List<LicenseComplianceViolation> violations)
    {
        var licenseType = licenseMetadata.Type switch
        {
            LicenseType.File       => PackageLicenseType.File,
            LicenseType.Expression => PackageLicenseType.Expression,
            _                      => throw new InvalidOperationException(),
        };

        _logger.LogPackageLicense(licenseType, licenseMetadata.License, licenseMetadata.WarningsAndErrors);

        var violation = new LicenseComplianceViolation(package.Id, licenseType, licenseMetadata.License, "NOT IMPLEMENTED");
        _logger.LogLicenseViolation(violation);
        violations.Add(violation);
    }
}
