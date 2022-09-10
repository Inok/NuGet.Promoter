using NuGet.Packaging.Core;
using NuGet.Versioning;
using Promote.NuGet.Commands.Core;

namespace Promote.NuGet.Commands.Promote;

internal class PromotePackageToFindMatchingPackagesLoggerAdapter : IFindMatchingPackagesLogger
{
    private readonly IPromotePackageLogger _logger;

    public PromotePackageToFindMatchingPackagesLoggerAdapter(IPromotePackageLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void LogMatchingPackagesResolved(string packageId,
                                            IReadOnlyCollection<VersionRange> versionRanges,
                                            IReadOnlyCollection<PackageIdentity> matchingPackages)
    {
        _logger.LogMatchingPackagesResolved(packageId, versionRanges, matchingPackages);
    }
}