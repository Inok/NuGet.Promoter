using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace Promote.NuGet.Commands.Promote;

public interface IPromotePackageLogger
{
    void LogResolvingMatchingPackages(IReadOnlyCollection<PackageDependency> dependencies);

    void LogMatchingPackagesResolved(string packageId, IReadOnlyCollection<VersionRange> versionRanges, IReadOnlyCollection<PackageIdentity> matchingPackages);

    void LogPackagePresentInDestination(PackageIdentity identity);

    void LogNoPackagesToPromote();

    void LogResolvingPackagesToPromote(IReadOnlyCollection<PackageIdentity> identities);

    void LogProcessingPackage(PackageIdentity identity);

    void LogProcessingDependency(string packageId, VersionRange versionRange);

    void LogNewDependencyToProcess(string packageId, VersionRange versionRange);

    void LogNewDependencyFound(PackageIdentity identity);

    void LogPackagesToPromote(IReadOnlyCollection<PackageIdentity> identities);

    void LogPromotePackage(PackageIdentity identity, int current, int total);

    void LogPromotedPackagesCount(int count);
}