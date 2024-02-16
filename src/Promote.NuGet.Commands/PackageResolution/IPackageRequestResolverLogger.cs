using NuGet.Packaging.Core;
using Promote.NuGet.Commands.Requests;

namespace Promote.NuGet.Commands.PackageResolution;

public interface IPackageRequestResolverLogger
{
    void LogResolvingMatchingPackages(IReadOnlyCollection<PackageRequest> requests);

    void LogPackageRequestResolution(PackageRequest request, IReadOnlyCollection<PackageIdentity> matchingPackages);
}
