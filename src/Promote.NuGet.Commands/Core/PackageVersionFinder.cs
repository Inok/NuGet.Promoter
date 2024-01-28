using CSharpFunctionalExtensions;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using Promote.NuGet.Feeds;

namespace Promote.NuGet.Commands.Core;

public sealed class PackageVersionFinder
{
    private readonly INuGetRepository _repository;

    public PackageVersionFinder(INuGetRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<Result<PackageIdentity>> FindLatestVersion(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentException("Value cannot be null or empty.", nameof(id));

        var allVersions = await _repository.Packages.GetAllVersions(id, cancellationToken);
        if (allVersions.IsFailure)
        {
            return Result.Failure<PackageIdentity>(allVersions.Error);
        }

        var maxVersion = allVersions.Value.Where(v => !v.IsPrerelease).Max();
        if (maxVersion == null)
        {
            return Result.Failure<PackageIdentity>($"Package {id} has no released versions");
        }

        return new PackageIdentity(id, maxVersion);
    }

    public async Task<Result<IReadOnlySet<PackageIdentity>>> FindMatchingPackages(
        IReadOnlyCollection<PackageRequest> requests,
        IFindMatchingPackagesLogger logger,
        CancellationToken cancellationToken = default
    )
    {
        if (requests == null) throw new ArgumentNullException(nameof(requests));
        if (logger == null) throw new ArgumentNullException(nameof(logger));

        var identities = new HashSet<PackageIdentity>();

        foreach (var packageRanges in requests.GroupBy(d => d.Id, StringComparer.OrdinalIgnoreCase))
        {
            var packageId = packageRanges.Key;
            var versionRanges = packageRanges.SelectMany(x => x.Versions).ToList();

            var matchingPackagesResult = await FindMatchingVersions(packageId, versionRanges, cancellationToken);
            if (matchingPackagesResult.IsFailure)
            {
                return Result.Failure<IReadOnlySet<PackageIdentity>>(matchingPackagesResult.Error);
            }

            var matchingPackages = matchingPackagesResult.Value;
            logger.LogMatchingPackagesResolved(packageId, versionRanges, matchingPackages);

            identities.UnionWith(matchingPackages);
        }

        return identities;
    }

    private async Task<Result<IReadOnlySet<PackageIdentity>>> FindMatchingVersions(
        string packageId,
        IReadOnlyCollection<VersionRange> versionRanges,
        CancellationToken cancellationToken)
    {
        var allVersionsResult = await _repository.Packages.GetAllVersions(packageId, cancellationToken);
        if (allVersionsResult.IsFailure)
        {
            return Result.Failure<IReadOnlySet<PackageIdentity>>(allVersionsResult.Error);
        }

        var matchingPackages = new HashSet<PackageIdentity>();

        foreach (var version in allVersionsResult.Value)
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

        return matchingPackages;
    }
}
