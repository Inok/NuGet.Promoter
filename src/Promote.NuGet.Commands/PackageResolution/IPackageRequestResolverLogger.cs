using NuGet.Packaging.Core;
using Promote.NuGet.Commands.Requests;

namespace Promote.NuGet.Commands.PackageResolution;

public interface IPackageRequestResolverLogger
{
    void LogResolvingMatchingPackages(IReadOnlyCollection<IPackageRequest> requests);

    void LogPackageRequestResolution(IPackageRequest request, IReadOnlyCollection<PackageIdentity> matchingPackages);
}
