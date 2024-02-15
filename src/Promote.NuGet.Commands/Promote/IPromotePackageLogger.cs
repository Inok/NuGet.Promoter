using NuGet.Packaging.Core;
using NuGet.Versioning;
using Promote.NuGet.Commands.Requests;

namespace Promote.NuGet.Commands.Promote;

public interface IPromotePackageLogger
{
    void LogResolvingMatchingPackages(IReadOnlyCollection<IPackageRequest> requests);

    void LogPackageRequestResolution(IPackageRequest request, IReadOnlyCollection<PackageIdentity> matchingPackages);

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
