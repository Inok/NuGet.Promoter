using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace Promote.NuGet.Commands.Requests.Resolution;

public interface IPackageRequestResolverLogger
{
    void LogResolvingPackageRequests();

    void LogResolvingPackageRequest(PackageRequest request);

    void LogPackageRequestResolution(PackageRequest request, IReadOnlyCollection<PackageIdentity> matchingPackages);

    void LogPackageVersionSkippedDueToAge(string packageId, NuGetVersion version, DateTimeOffset publishedDate);
}
