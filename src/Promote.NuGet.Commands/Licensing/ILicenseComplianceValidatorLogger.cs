using Promote.NuGet.Commands.Promote.Resolution;

namespace Promote.NuGet.Commands.Licensing;

public interface ILicenseComplianceValidatorLogger
{
    void LogLicenseSummary(IReadOnlyCollection<PackageInfo> packages);
    void LogComplianceChecksDisabled();
}
