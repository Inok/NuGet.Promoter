using NuGet.Packaging.Core;
using NuGet.Versioning;
using Promote.NuGet.Commands.Mirroring;
using Promote.NuGet.Commands.Promote.Resolution;
using Promote.NuGet.Commands.Requests.Resolution;

namespace Promote.NuGet.Commands.Promote;

public interface IPromotePackageLogger : IPackageRequestResolverLogger, IPackageMirroringExecutorLogger
{
    void LogPackagePresentInDestination(PackageIdentity identity);

    void LogNoPackagesToPromote();

    void LogResolvingPackagesToPromote(IReadOnlyCollection<PackageIdentity> identities);

    void LogProcessingPackage(PackageIdentity identity);

    void LogNewDependencyToProcess(PackageIdentity source, string dependencyPackageId, VersionRange dependencyVersionRange);

    void LogProcessingDependency(PackageIdentity source, string dependencyPackageId, VersionRange dependencyVersionRange);

    void LogResolvedDependency(PackageIdentity source, PackageIdentity resolvedDependency);

    void LogNewDependencyFound(PackageIdentity identity);

    void LogPackageResolutionTree(PackageResolutionTree packageTree);

    void LogPackagesToPromote(IReadOnlyCollection<PackageIdentity> identities);

    void LogDryRun();
}
