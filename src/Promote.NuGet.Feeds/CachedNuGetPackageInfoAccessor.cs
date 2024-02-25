using System.IO;
using CSharpFunctionalExtensions;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Promote.NuGet.Feeds;

public class CachedNuGetPackageInfoAccessor : INuGetPackageInfoAccessor
{
    private readonly INuGetPackageInfoAccessor _inner;
    private readonly Dictionary<PackageIdentity, IReadOnlyCollection<NuGetVersion>> _packageVersionsCache;

    public CachedNuGetPackageInfoAccessor(INuGetPackageInfoAccessor inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _packageVersionsCache = new Dictionary<PackageIdentity, IReadOnlyCollection<NuGetVersion>>();
    }

    public async Task<Result<IReadOnlyCollection<NuGetVersion>>> GetAllVersions(string packageId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(packageId)) throw new ArgumentException("Value cannot be null or empty.", nameof(packageId));

        var packageIdentity = new PackageIdentity(packageId, null);

        if (_packageVersionsCache.TryGetValue(packageIdentity, out var allVersions))
        {
            return Result.Success(allVersions);
        }

        var allVersionsResult = await _inner.GetAllVersions(packageId, cancellationToken);
        if (allVersionsResult.IsFailure)
        {
            return allVersionsResult;
        }

        allVersions = allVersionsResult.Value;
        _packageVersionsCache.Add(packageIdentity, allVersions);

        return Result.Success(allVersions);
    }

    public Task<Result<IPackageSearchMetadata>> GetPackageMetadata(PackageIdentity identity, CancellationToken cancellationToken = default)
    {
        return _inner.GetPackageMetadata(identity, cancellationToken);
    }

    public Task<Result> CopyNupkgToStream(PackageIdentity identity, Stream stream, CancellationToken cancellationToken = default)
    {
        return _inner.CopyNupkgToStream(identity, stream, cancellationToken);
    }

    public Task<Result> PushPackage(string filePath, bool skipDuplicate, CancellationToken cancellationToken = default)
    {
        return _inner.PushPackage(filePath, skipDuplicate, cancellationToken);
    }

    public Task<Result<bool>> DoesPackageExist(PackageIdentity identity, CancellationToken cancellationToken = default)
    {
        return _inner.DoesPackageExist(identity, cancellationToken);
    }
}
