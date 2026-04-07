using CSharpFunctionalExtensions;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using Promote.NuGet.Feeds;

namespace Promote.NuGet.Commands.Requests.Resolution;

internal sealed class ResolvePackageVersionPolicyVisitor : IPackageVersionPolicyVisitor<Result<IReadOnlySet<PackageIdentity>>>
{
    private readonly string _packageId;
    private readonly INuGetRepository _repository;
    private readonly IPackageRequestResolverLogger _logger;
    private readonly TimeSpan? _minimumReleaseAge;
    private readonly TimeProvider _timeProvider;

    public ResolvePackageVersionPolicyVisitor(string packageId,
                                              INuGetRepository repository,
                                              IPackageRequestResolverLogger logger,
                                              TimeSpan? minimumReleaseAge,
                                              TimeProvider timeProvider)
    {
        _packageId = packageId ?? throw new ArgumentNullException(nameof(packageId));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _minimumReleaseAge = minimumReleaseAge;
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<Result<IReadOnlySet<PackageIdentity>>> Visit(ExactPackageVersionPolicy versionPolicy, CancellationToken cancellationToken = default)
    {
        if (versionPolicy == null) throw new ArgumentNullException(nameof(versionPolicy));

        var identity = new PackageIdentity(_packageId, versionPolicy.Version);

        var getPackageMetaResult = await _repository.Packages.GetPackageMetadata(identity, cancellationToken);
        if (getPackageMetaResult.IsFailure)
        {
            return getPackageMetaResult.ConvertFailure<IReadOnlySet<PackageIdentity>>();
        }

        return new HashSet<PackageIdentity>(capacity: 1) { identity };
    }

    public async Task<Result<IReadOnlySet<PackageIdentity>>> Visit(VersionRangePackageVersionPolicy versionPolicy, CancellationToken cancellationToken = default)
    {
        if (versionPolicy == null) throw new ArgumentNullException(nameof(versionPolicy));

        var allVersionsResult = await _repository.Packages.GetAllVersions(_packageId, cancellationToken);
        if (allVersionsResult.IsFailure)
        {
            return allVersionsResult.ConvertFailure<IReadOnlySet<PackageIdentity>>();
        }

        var matchingPackages = new HashSet<PackageIdentity>();

        foreach (var version in allVersionsResult.Value)
        {
            if (version.IsPrerelease)
            {
                continue;
            }

            if (!versionPolicy.VersionRange.Satisfies(version))
            {
                continue;
            }

            var packageIdentity = new PackageIdentity(_packageId, version);

            var packageMetadata = await _repository.Packages.GetPackageMetadata(packageIdentity, cancellationToken);
            if (packageMetadata.IsFailure)
            {
                return packageMetadata.ConvertFailure<IReadOnlySet<PackageIdentity>>();
            }

            if (!packageMetadata.Value.IsListed)
            {
                continue;
            }

            var ageCheckResult = CheckReleaseAge(packageMetadata.Value);
            if (ageCheckResult.IsFailure)
            {
                return Result.Failure<IReadOnlySet<PackageIdentity>>(ageCheckResult.Error);
            }

            if (!ageCheckResult.Value)
            {
                continue;
            }

            matchingPackages.Add(packageIdentity);
        }

        return matchingPackages;
    }

    public async Task<Result<IReadOnlySet<PackageIdentity>>> Visit(LatestPackageVersionPolicy versionPolicy, CancellationToken cancellationToken = default)
    {
        if (versionPolicy == null) throw new ArgumentNullException(nameof(versionPolicy));

        var allVersions = await _repository.Packages.GetAllVersions(_packageId, cancellationToken);
        if (allVersions.IsFailure)
        {
            return allVersions.ConvertFailure<IReadOnlySet<PackageIdentity>>();
        }

        foreach (var version in allVersions.Value.Where(v => !v.IsPrerelease).OrderByDescending(x => x))
        {
            var identity = new PackageIdentity(_packageId, version);

            var packageMetadata = await _repository.Packages.GetPackageMetadata(identity, cancellationToken);
            if (packageMetadata.IsFailure)
            {
                return packageMetadata.ConvertFailure<IReadOnlySet<PackageIdentity>>();
            }

            if (!packageMetadata.Value.IsListed)
            {
                continue;
            }

            var ageCheckResult = CheckReleaseAge(packageMetadata.Value);
            if (ageCheckResult.IsFailure)
            {
                return Result.Failure<IReadOnlySet<PackageIdentity>>(ageCheckResult.Error);
            }

            if (!ageCheckResult.Value)
            {
                continue;
            }

            return new HashSet<PackageIdentity>(capacity: 1) { identity };
        }

        return Result.Failure<IReadOnlySet<PackageIdentity>>($"Package {_packageId} has no released versions");
    }

    /// <summary>
    /// Checks whether the package meets the minimum release age requirement.
    /// Returns <c>Result.Success(true)</c> if old enough or no filter is set,
    /// <c>Result.Success(false)</c> if too new (and logs the skip),
    /// or <c>Result.Failure</c> if the published date is missing.
    /// </summary>
    private Result<bool> CheckReleaseAge(IPackageSearchMetadata metadata)
    {
        if (_minimumReleaseAge is not { } minimumAge)
        {
            return true;
        }

        var published = metadata.Published;
        if (published is null)
        {
            return Result.Failure<bool>($"Package {_packageId} {metadata.Identity.Version} has no publish date. Cannot apply minimum release age filter.");
        }

        var age = _timeProvider.GetUtcNow() - published.Value;
        if (age < minimumAge)
        {
            _logger.LogPackageVersionSkippedDueToAge(_packageId, metadata.Identity.Version, published.Value);
            return false;
        }

        return true;
    }
}
