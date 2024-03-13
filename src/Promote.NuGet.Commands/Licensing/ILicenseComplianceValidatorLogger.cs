using Promote.NuGet.Commands.Promote.Resolution;

namespace Promote.NuGet.Commands.Licensing;

public interface ILicenseComplianceValidatorLogger
{
    void LogLicenseSummary(IReadOnlyCollection<PackageInfo> packages);

    void LogComplianceChecksDisabled();

    void LogLicenseViolationsSummary(IReadOnlyCollection<LicenseComplianceViolation> violations);

    void LogNoLicenseViolations();
}
