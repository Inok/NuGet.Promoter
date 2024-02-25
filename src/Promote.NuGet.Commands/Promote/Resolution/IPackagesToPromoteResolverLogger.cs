using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace Promote.NuGet.Commands.Promote.Resolution;

public interface IPackagesToPromoteResolverLogger
{
    void LogResolvingPackagesToPromote(IReadOnlyCollection<PackageIdentity> identities);

    void LogProcessingPackage(PackageIdentity identity);

    void LogPackagePresentInDestination(PackageIdentity identity);

    void LogProcessingDependency(PackageIdentity source, string dependencyPackageId, VersionRange dependencyVersionRange);

    void LogResolvedDependency(PackageIdentity source, PackageIdentity resolvedDependency);

    void LogNewDependencyFound(PackageIdentity identity);

    void LogNewDependencyToProcess(PackageIdentity source, string dependencyPackageId, VersionRange dependencyVersionRange);

    void LogResolvedPackageTree(PackageResolutionTree packageTree);
}
