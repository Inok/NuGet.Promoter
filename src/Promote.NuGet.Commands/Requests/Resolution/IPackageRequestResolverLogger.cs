using NuGet.Packaging.Core;

namespace Promote.NuGet.Commands.Requests.Resolution;

public interface IPackageRequestResolverLogger
{
    void LogResolvingMatchingPackages(IReadOnlyCollection<PackageRequest> requests);

    void LogPackageRequestResolution(PackageRequest request, IReadOnlyCollection<PackageIdentity> matchingPackages);
}
