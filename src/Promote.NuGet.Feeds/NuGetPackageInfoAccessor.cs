using System.IO;
using CSharpFunctionalExtensions;
using NuGet.Common;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Promote.NuGet.Feeds;

internal class NuGetPackageInfoAccessor : INuGetPackageInfoAccessor
{
    private readonly NuGetRepositoryDescriptor _repositoryDescriptor;
    private readonly SourceRepository _sourceRepository;
    private readonly SourceCacheContext _cacheContext;
    private readonly PackageDownloadContext _downloadContext;
    private readonly string _globalPackagesFolder;
    private readonly string _directDownloadDirectory;
    private readonly ILogger _logger;

    public NuGetPackageInfoAccessor(NuGetRepositoryDescriptor repositoryDescriptor,
                                    SourceRepository sourceRepository,
                                    SourceCacheContext cacheContext,
                                    ILogger logger)
    {
        _repositoryDescriptor = repositoryDescriptor;
        _sourceRepository = sourceRepository;
        _cacheContext = cacheContext;
        _directDownloadDirectory = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        _globalPackagesFolder = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        _downloadContext = new PackageDownloadContext(_cacheContext, _directDownloadDirectory, directDownload: false);
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

    public async Task<DownloadResourceResult> GetPackageResource(PackageIdentity identity, CancellationToken cancellationToken = default)
    {
        var downloadResource = await _sourceRepository.GetResourceAsync<DownloadResource>(cancellationToken);

        var result = await downloadResource.GetDownloadResourceResultAsync(identity, _downloadContext, _globalPackagesFolder, _logger, cancellationToken);

        return result;
    }

    public void Dispose()
    {
        if (Directory.Exists(_globalPackagesFolder))
        {
            Directory.Delete(_globalPackagesFolder, recursive: true);
        }

        if (Directory.Exists(_directDownloadDirectory))
        {
            Directory.Delete(_directDownloadDirectory, recursive: true);
        }
    }
}
