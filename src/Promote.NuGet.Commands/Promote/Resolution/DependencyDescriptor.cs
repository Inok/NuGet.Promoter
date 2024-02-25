using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace Promote.NuGet.Commands.Promote.Resolution;

public sealed record DependencyDescriptor(PackageIdentity Identity, VersionRange VersionRange);
