using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace Promote.NuGet.Commands.Core;

public interface IFindMatchingPackagesLogger
{
    public void LogMatchingPackagesResolved(string packageId,
                                    IReadOnlyCollection<VersionRange> versionRanges,
                                    IReadOnlyCollection<PackageIdentity> matchingPackages
    );
}