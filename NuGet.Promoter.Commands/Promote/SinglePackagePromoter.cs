using System.IO;
using CSharpFunctionalExtensions;
using NuGet.Common;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;

namespace NuGet.Promoter.Commands.Promote;

public class SinglePackagePromoter
{
    private readonly NuGetRepository _sourceRepository;
    private readonly NuGetRepository _destinationRepository;
    private readonly SourceCacheContext _cacheContext;
    private readonly ILogger _logger;

    public SinglePackagePromoter(NuGetRepository sourceRepository, NuGetRepository destinationRepository, SourceCacheContext cacheContext, ILogger logger)
    {
        _sourceRepository = sourceRepository ?? throw new ArgumentNullException(nameof(sourceRepository));
        _destinationRepository = destinationRepository ?? throw new ArgumentNullException(nameof(destinationRepository));
        _cacheContext = cacheContext ?? throw new ArgumentNullException(nameof(cacheContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<UnitResult<string>> Promote(PackageIdentity identity, bool skipDuplicate, CancellationToken cancellationToken)
    {
        if (identity == null) throw new ArgumentNullException(nameof(identity));
        if (!identity.HasVersion) throw new ArgumentException("Identity must have version.", nameof(identity));

        var tempFilePath = Path.GetTempFileName();

        try
        {
            var downloadResult = await DownloadPackage(identity, tempFilePath, cancellationToken);
            if (downloadResult.IsFailure)
            {
                return downloadResult;
            }

            await PushPackage(tempFilePath, skipDuplicate, cancellationToken);
        }
        finally
        {
            File.Delete(tempFilePath);
        }

        return UnitResult.Success<string>();
    }

    private async Task<UnitResult<string>> DownloadPackage(PackageIdentity identity, string filePath, CancellationToken cancellationToken)
    {
        var sourceFindResource = await _sourceRepository.Repository.GetResourceAsync<FindPackageByIdResource>(cancellationToken);

        await using var packageStream = new FileStream(filePath, FileMode.Truncate, FileAccess.Write);

        var result = await sourceFindResource.CopyNupkgToStreamAsync(identity.Id, identity.Version, packageStream, _cacheContext, _logger, cancellationToken);
        if (!result)
        {
            return UnitResult.Failure<string>($"Failed to download package {identity}");
        }

        return UnitResult.Success<string>();
    }

    private async Task PushPackage(string filePath, bool skipDuplicate, CancellationToken cancellationToken)
    {
        var destUpdateResource = await _destinationRepository.Repository.GetResourceAsync<PackageUpdateResource>(cancellationToken);
        var apiKey = _destinationRepository.ApiKey;

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
    }
}