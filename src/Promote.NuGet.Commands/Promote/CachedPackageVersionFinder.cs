using CSharpFunctionalExtensions;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using Promote.NuGet.Feeds;

namespace Promote.NuGet.Commands.Promote;

internal class CachedPackageVersionFinder
{
    private readonly INuGetRepository _repository;
    private readonly Dictionary<PackageIdentity, IReadOnlyCollection<NuGetVersion>> _packageVersionsCache;

    public CachedPackageVersionFinder(INuGetRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _packageVersionsCache = new Dictionary<PackageIdentity, IReadOnlyCollection<NuGetVersion>>();
    }

    public async Task<Result<IReadOnlyCollection<NuGetVersion>>> GetAllVersions(string packageId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(packageId)) throw new ArgumentException("Value cannot be null or empty.", nameof(packageId));

        var identity = new PackageIdentity(packageId, null);

        if (_packageVersionsCache.TryGetValue(identity, out var allVersionsOfDep))
        {
            return Result.Success(allVersionsOfDep);
        }

        var allVersionsResult = await _repository.Packages.GetAllVersions(identity.Id, cancellationToken);
        if (allVersionsResult.IsFailure)
        {
            return Result.Failure<IReadOnlyCollection<NuGetVersion>>(allVersionsResult.Error);
        }

        allVersionsOfDep = allVersionsResult.Value;
        _packageVersionsCache.Add(identity, allVersionsOfDep);

        return Result.Success(allVersionsOfDep);
    }
}