using System.IO;
using CSharpFunctionalExtensions;
using NuGet.Common;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Promote.NuGet.Feeds;

internal class NuGetPackageInfoAccessor : INuGetPackageInfoAccessor
{
    private readonly NuGetRepositoryDescriptor _repositoryDescriptor;
    private readonly SourceRepository _sourceRepository;
    private readonly SourceCacheContext _cacheContext;
    private readonly ILogger _logger;

    public NuGetPackageInfoAccessor(NuGetRepositoryDescriptor repositoryDescriptor,
                                    SourceRepository sourceRepository,
                                    SourceCacheContext cacheContext,
                                    ILogger logger)
    {
        _repositoryDescriptor = repositoryDescriptor;
        _sourceRepository = sourceRepository;
        _cacheContext = cacheContext;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyCollection<NuGetVersion>>> GetAllVersions(string packageId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(packageId)) throw new ArgumentException("Value cannot be null or empty.", nameof(packageId));

        var findResource = await _sourceRepository.GetResourceAsync<FindPackageByIdResource>(cancellationToken);

        var allVersions = await findResource.GetAllVersionsAsync(packageId, _cacheContext, _logger, cancellationToken);
        var allVersionsCollection = allVersions?.ToList();

        if (allVersionsCollection == null || !allVersionsCollection.Any())
        {
            return Result.Failure<IReadOnlyCollection<NuGetVersion>>($"Package {packageId} not found");
        }

        return allVersionsCollection;
    }

    public async Task<Result<IPackageSearchMetadata>> GetPackageMetadata(PackageIdentity identity, CancellationToken cancellationToken)
    {
        if (identity == null) throw new ArgumentNullException(nameof(identity));

        var packageMetadataResource = await _sourceRepository.GetResourceAsync<PackageMetadataResource>(cancellationToken);

        var meta = await packageMetadataResource.GetMetadataAsync(identity, _cacheContext, _logger, cancellationToken);
        if (meta == null)
        {
            return Result.Failure<IPackageSearchMetadata>($"Package {identity} not found");
        }

        return Result.Success(meta);
    }

    public async Task<Result> CopyNupkgToStream(PackageIdentity identity, Stream stream, CancellationToken cancellationToken = default)
    {
        if (identity == null) throw new ArgumentNullException(nameof(identity));
        if (stream == null) throw new ArgumentNullException(nameof(stream));

        var findResource = await _sourceRepository.GetResourceAsync<FindPackageByIdResource>(cancellationToken);

        var result = await findResource.CopyNupkgToStreamAsync(identity.Id, identity.Version, stream, _cacheContext, _logger, cancellationToken);
        if (!result)
        {
            return Result.Failure($"Failed to download package {identity}");
        }

        return Result.Success();
    }

    public async Task<Result> PushPackage(string filePath, bool skipDuplicate, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(filePath)) throw new ArgumentException("Value cannot be null or empty.", nameof(filePath));

        var destUpdateResource = await _sourceRepository.GetResourceAsync<PackageUpdateResource>(cancellationToken);
        var apiKey = _repositoryDescriptor.ApiKey;

        await destUpdateResource.Push(
            new[] { filePath },
            symbolSource: null,
            timeoutInSecond: 60,
            disableBuffering: false,
            getApiKey: _ => apiKey,
            getSymbolApiKey: packageSource => null,
            noServiceEndpoint: false,
            skipDuplicate: skipDuplicate,
            symbolPackageUpdateResource: null,
            _logger
        );

        return Result.Success();
    }

    public async Task<Result<bool>> DoesPackageExist(PackageIdentity identity, CancellationToken cancellationToken = default)
    {
        if (identity == null) throw new ArgumentNullException(nameof(identity));
        if (!identity.HasVersion) throw new ArgumentException("Identity must have version.", nameof(identity));

        var findResource = await _sourceRepository.GetResourceAsync<FindPackageByIdResource>(cancellationToken);

        return await findResource.DoesPackageExistAsync(identity.Id, identity.Version, _cacheContext, _logger, cancellationToken);
    }
}
