using CSharpFunctionalExtensions;
using NuGet.Packaging.Core;
using Promote.NuGet.Commands.Requests;
using Promote.NuGet.Feeds;

namespace Promote.NuGet.Commands.PackageResolution;

internal sealed class ResolvePackageRequestVisitor : IPackageRequestVisitor<Result<IReadOnlySet<PackageIdentity>>>
{
    private readonly INuGetRepository _repository;

    public ResolvePackageRequestVisitor(INuGetRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<Result<IReadOnlySet<PackageIdentity>>> Visit(ExactPackageRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        var identity = new PackageIdentity(request.Id, request.Version);

        var getPackageMetaResult = await _repository.Packages.GetPackageMetadata(identity, cancellationToken);
        if (getPackageMetaResult.IsFailure)
        {
            return getPackageMetaResult.ConvertFailure<IReadOnlySet<PackageIdentity>>();
        }

        return new HashSet<PackageIdentity>(capacity: 1) { identity };
    }

    public async Task<Result<IReadOnlySet<PackageIdentity>>> Visit(VersionRangePackageRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        var allVersionsResult = await _repository.Packages.GetAllVersions(request.Id, cancellationToken);
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

            if (!request.Versions.Any(range => range.Satisfies(version)))
            {
                continue;
            }

            matchingPackages.Add(new PackageIdentity(request.Id, version));
        }

        return matchingPackages;
    }

    public async Task<Result<IReadOnlySet<PackageIdentity>>> Visit(LatestPackageRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        var allVersions = await _repository.Packages.GetAllVersions(request.Id, cancellationToken);
        if (allVersions.IsFailure)
        {
            return allVersions.ConvertFailure<IReadOnlySet<PackageIdentity>>();
        }

        var maxVersion = allVersions.Value.Where(v => !v.IsPrerelease).Max();
        if (maxVersion == null)
        {
            return Result.Failure<IReadOnlySet<PackageIdentity>>($"Package {request.Id} has no released versions");
        }

        var identity = new PackageIdentity(request.Id, maxVersion);

        return new HashSet<PackageIdentity>(capacity: 1) { identity };
    }
}
