using NuGet.Packaging.Core;

namespace Promote.NuGet.Commands.Requests.Resolution;

public interface IPackageRequestResolverLogger
{
    void LogResolvingPackageRequests();

    void LogResolvingPackageRequest(PackageRequest request);

    void LogPackageRequestResolution(PackageRequest request, IReadOnlyCollection<PackageIdentity> matchingPackages);
}
