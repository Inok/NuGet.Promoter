using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace Promote.NuGet.Commands.Promote.Resolution;

public interface IPackagesToPromoteResolverLogger
{
    void LogPackagePresentInDestination(PackageIdentity identity);

    void LogProcessingPackage(PackageIdentity identity);

    void LogNewDependencyToProcess(PackageIdentity source, string dependencyPackageId, VersionRange dependencyVersionRange);

    void LogProcessingDependency(PackageIdentity source, string dependencyPackageId, VersionRange dependencyVersionRange);

    void LogResolvedDependency(PackageIdentity source, PackageIdentity resolvedDependency);

    void LogNewDependencyFound(PackageIdentity identity);

    void LogPackageResolutionTree(PackageResolutionTree packageTree);
}
