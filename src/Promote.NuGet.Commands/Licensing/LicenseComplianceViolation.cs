using NuGet.Packaging.Core;

namespace Promote.NuGet.Commands.Licensing;

public record LicenseComplianceViolation(PackageIdentity Id, PackageLicenseInfo License);
