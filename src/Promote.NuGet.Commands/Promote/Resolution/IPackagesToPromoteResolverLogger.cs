using NuGet.Packaging.Core;

namespace Promote.NuGet.Commands.Promote.Resolution;

public interface IPackagesToPromoteResolverLogger
{
    void LogResolvingPackagesToPromote(IReadOnlyCollection<PackageIdentity> identities);

    void LogProcessingPackage(PackageIdentity identity);

    void LogPackagePresentInDestination(PackageIdentity identity);

    void LogPackageNotInDestination(PackageIdentity identity);

    void LogResolvingDependency(PackageIdentity source, DependencyDescriptor dependency);

    void LogResolvedDependency(PackageIdentity identity);

    void LogNewPackageQueuedForProcessing(PackageIdentity identity);

    void LogPackageIsAlreadyProcessedOrQueued(PackageIdentity identity);

    void LogNoDependencies(PackageIdentity identity);

    void LogPackageDependenciesSkipped(PackageIdentity identity);

    void LogResolvedPackageTree(PackageResolutionTree packageTree);
}
