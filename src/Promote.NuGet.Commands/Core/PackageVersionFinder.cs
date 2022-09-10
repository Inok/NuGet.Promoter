using CSharpFunctionalExtensions;
using NuGet.Common;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;

namespace Promote.NuGet.Commands.Core;

public sealed class PackageVersionFinder
{
    private readonly NuGetRepository _repository;
    private readonly SourceCacheContext _cacheContext;
    private readonly ILogger _logger;

    public PackageVersionFinder(NuGetRepository repository, SourceCacheContext cacheContext, ILogger logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _cacheContext = cacheContext ?? throw new ArgumentNullException(nameof(cacheContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<PackageIdentity, string>> FindLatestVersion(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentException("Value cannot be null or empty.", nameof(id));

        var findResource = await _repository.Repository.GetResourceAsync<FindPackageByIdResource>(cancellationToken);

        var allVersions = await findResource.GetAllVersionsAsync(id, _cacheContext, _logger, cancellationToken);
        var allVersionsCollection = allVersions?.ToList();

        if (allVersionsCollection == null || !allVersionsCollection.Any())
        {
            return $"Package {id} not found";
        }

        var maxVersion = allVersionsCollection.Where(v => !v.IsPrerelease).Max();

        return new PackageIdentity(id, maxVersion);
    }

    public async Task<Result<IReadOnlySet<PackageIdentity>, string>> FindMatchingPackages(
        IReadOnlyCollection<PackageDependency> dependencies,
        IFindMatchingPackagesLogger logger,
        CancellationToken cancellationToken = default
    )
    {
        if (dependencies == null) throw new ArgumentNullException(nameof(dependencies));
        if (logger == null) throw new ArgumentNullException(nameof(logger));

        var findResource = await _repository.Repository.GetResourceAsync<FindPackageByIdResource>(cancellationToken);

        var identities = new HashSet<PackageIdentity>();
        var matchingPackages = new HashSet<PackageIdentity>();

        foreach (var packageRanges in dependencies.GroupBy(d => d.Id, StringComparer.OrdinalIgnoreCase))
        {
            var packageId = packageRanges.Key;
            var versionRanges = packageRanges.Select(x => x.VersionRange).ToList();

            var allVersions = await findResource.GetAllVersionsAsync(packageId, _cacheContext, _logger, cancellationToken);
            if (allVersions == null)
            {
                return $"Package {packageId} not found.";
            }

            foreach (var version in allVersions)
            {
                if (version.IsPrerelease)
                {
                    continue;
                }

                if (!versionRanges.Any(range => range.Satisfies(version)))
                {
                    continue;
                }

                matchingPackages.Add(new PackageIdentity(packageId, version));
            }

            logger.LogMatchingPackagesResolved(packageId, versionRanges, matchingPackages);

            identities.UnionWith(matchingPackages);
            matchingPackages.Clear();
        }

        return identities;
    }
}