using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace Promote.NuGet.Commands.Promote;

public interface IPromotePackageLogger
{
    void LogResolvingMatchingPackages(IReadOnlyCollection<PackageDependency> dependencies);

    void LogMatchingPackagesResolved(string packageId, IReadOnlyCollection<VersionRange> versionRanges, IReadOnlyCollection<PackageIdentity> matchingPackages);

    void LogResolvingDependencies(IReadOnlyCollection<PackageIdentity> identities);

    void LogProcessingDependenciesOfPackage(PackageIdentity identity);

    void LogNewDependencyFound(PackageIdentity identity);

    void LogPackagesToPromote(IReadOnlyCollection<PackageIdentity> identities);

    void LogPromotePackage(PackageIdentity identity, int current, int total);

    void LogPromotedPackagesCount(int count);
}