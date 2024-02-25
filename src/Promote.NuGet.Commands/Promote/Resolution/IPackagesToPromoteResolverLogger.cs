using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace Promote.NuGet.Commands.Promote.Resolution;

public interface IPackagesToPromoteResolverLogger
{
    void LogResolvingPackagesToPromote(IReadOnlyCollection<PackageIdentity> identities);

    void LogProcessingPackage(PackageIdentity identity);

    void LogPackagePresentInDestination(PackageIdentity identity);

    void LogPackageNotInDestination(PackageIdentity identity);

    void LogPackageDependenciesToResolve(PackageIdentity source, IReadOnlySet<DependencyDescriptor> dependencies);

    void LogPackageDependenciesSkipped(PackageIdentity identity);

    void LogResolvingDependency(PackageIdentity source, string dependencyPackageId, VersionRange dependencyVersionRange);

    void LogResolvedDependency(PackageIdentity identity);

    void LogNewPackageQueuedForProcessing(PackageIdentity identity);

    void LogPackageIsAlreadyProcessedOrQueued(PackageIdentity identity);

    void LogResolvedPackageTree(PackageResolutionTree packageTree);
}
