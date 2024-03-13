using NuGet.Packaging.Core;

namespace Promote.NuGet.Commands.Licensing;

public interface ILicenseComplianceValidatorLogger
{
    void LogCheckingLicenseCompliance();

    void LogComplianceChecksDisabled();

    void LogLicenseViolationsSummary(IReadOnlyCollection<LicenseComplianceViolation> violations);

    void LogNoLicenseViolations();

    void LogCheckingLicenseComplianceForPackage(PackageIdentity identity);

    void LogFailedToDownloadPackage(PackageIdentity identity);

    void LogPackageLicense(PackageLicenseType licenseType, string license, IReadOnlyList<string>? warningsAndErrors = null);

    void LogLicenseCompliance(string reason);

    void LogLicenseViolation(LicenseComplianceViolation violation);

    void LogAcceptedLicenseFileReadFailure(string acceptedFilePath, Exception exception);
}
