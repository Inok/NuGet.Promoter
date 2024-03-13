using Promote.NuGet.Commands.Licensing;
using Promote.NuGet.Commands.Requests;

namespace Promote.NuGet.Commands.Promote;

public record PromotePackageCommandArguments(
    IReadOnlyCollection<PackageRequest> Requests,
    LicenseComplianceSettings LicenseComplianceSettings
);
