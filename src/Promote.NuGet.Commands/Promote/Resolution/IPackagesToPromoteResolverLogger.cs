using NuGet.Packaging.Core;
using Promote.NuGet.Commands.Licensing;

namespace Promote.NuGet.Commands.Promote.Resolution;

public interface IPackagesToPromoteResolverLogger
{
    void LogResolvingPackagesToPromote(IReadOnlyCollection<PackageIdentity> identities);

    void LogProcessingPackage(PackageIdentity identity);

    void LogPackageLicense(PackageIdentity identity, PackageLicenseInfo license);

    void LogPackagePresentInDestination(PackageIdentity identity);

    void LogPackageNotInDestination(PackageIdentity identity);

    void LogResolvingDependency(PackageIdentity source, DependencyDescriptor dependency);

    void LogResolvedDependency(PackageIdentity identity, bool enqueuedForProcessing);

    void LogNoDependencies(PackageIdentity identity);

    void LogPackageDependenciesSkipped(PackageIdentity identity);

    void LogResolvedPackageTree(PackageResolutionTree packageTree);
}
