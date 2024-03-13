using CSharpFunctionalExtensions;
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
        _logger.LogLicenseSummary(packages);

        if (!settings.Enabled)
        {
            _logger.LogComplianceChecksDisabled();
            return Result.Success();
        }

        var violations = new List<LicenseComplianceViolation>();

        foreach (var package in packages)
        {
            using var resource = await _repository.Packages.GetPackageResource(package.Id, cancellationToken);

            if (resource.PackageReader == null)
            {
                violations.Add(new LicenseComplianceViolation(package.Id, new PackageLicenseInfo("<unknown>", null)));
                continue;
            }

            var nuspecReader = resource.PackageReader.NuspecReader;

            if (nuspecReader.GetLicenseMetadata() is { } licenseMetadata)
            {
                violations.Add(new LicenseComplianceViolation(package.Id, new PackageLicenseInfo($"NOT IMPLEMENTED: {licenseMetadata.License}", null)));
                continue;
            }

            if (nuspecReader.GetLicenseUrl() is { } licenseUrl)
            {
                var uri = Uri.TryCreate(licenseUrl, UriKind.Absolute, out var url);
                violations.Add(new LicenseComplianceViolation(package.Id, new PackageLicenseInfo("NOT IMPLEMENTED", uri ? url : null)));
                continue;
            }

            violations.Add(new LicenseComplianceViolation(package.Id, new PackageLicenseInfo("<unknown>", null)));
        }

        if (violations.Count == 0)
        {
            _logger.LogNoLicenseViolations();
            return Result.Success();
        }

        _logger.LogLicenseViolationsSummary(violations);
        return Result.Failure("License violations found.");
    }
}
