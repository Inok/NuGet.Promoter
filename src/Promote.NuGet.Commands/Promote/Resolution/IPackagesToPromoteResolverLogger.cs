using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace Promote.NuGet.Commands.Promote.Resolution;

public interface IPackagesToPromoteResolverLogger
{
    void LogResolvingPackagesToPromote(IReadOnlyCollection<PackageIdentity> identities);

    void LogProcessingPackage(PackageIdentity identity);

    void LogPackagePresentInDestination(PackageIdentity identity);

    void LogResolvingDependency(PackageIdentity source, string dependencyPackageId, VersionRange dependencyVersionRange);

    void LogResolvedDependency(PackageIdentity identity);

    void LogNewPackageQueuedForProcessing(PackageIdentity identity);

    void LogPackageDependenciesQueuedForResolving(PackageIdentity source, IReadOnlySet<DependencyDescriptor> dependencies);

    void LogResolvedPackageTree(PackageResolutionTree packageTree);
}
