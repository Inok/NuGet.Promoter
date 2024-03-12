using NuGet.Packaging.Core;
using Promote.NuGet.Commands.Licensing;

namespace Promote.NuGet.Commands.Promote.Resolution;

public sealed record PackageInfo(PackageIdentity Id, PackageLicenseInfo License);
