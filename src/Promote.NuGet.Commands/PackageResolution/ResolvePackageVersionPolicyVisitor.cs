using CSharpFunctionalExtensions;
using NuGet.Packaging.Core;
using Promote.NuGet.Commands.Requests;
using Promote.NuGet.Feeds;

namespace Promote.NuGet.Commands.PackageResolution;

internal sealed class ResolvePackageVersionPolicyVisitor : IPackageVersionPolicyVisitor<Result<IReadOnlySet<PackageIdentity>>>
{
    private readonly string _packageId;
    private readonly INuGetRepository _repository;

    public ResolvePackageVersionPolicyVisitor(string packageId, INuGetRepository repository)
    {
        _packageId = packageId ?? throw new ArgumentNullException(nameof(packageId));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
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

            matchingPackages.Add(new PackageIdentity(_packageId, version));
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

        var maxVersion = allVersions.Value.Where(v => !v.IsPrerelease).Max();
        if (maxVersion == null)
        {
            return Result.Failure<IReadOnlySet<PackageIdentity>>($"Package {_packageId} has no released versions");
        }

        var identity = new PackageIdentity(_packageId, maxVersion);

        return new HashSet<PackageIdentity>(capacity: 1) { identity };
    }
}
