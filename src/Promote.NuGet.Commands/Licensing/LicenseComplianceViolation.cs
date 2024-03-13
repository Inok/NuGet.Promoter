using NuGet.Packaging.Core;

namespace Promote.NuGet.Commands.Licensing;

public record LicenseComplianceViolation(PackageIdentity PackageId, PackageLicenseType LicenseType, string License, string Explanation);
