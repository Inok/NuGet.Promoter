using System.IO;
using CSharpFunctionalExtensions;
using NuGet.Packaging.Core;
using Promote.NuGet.Feeds;

namespace Promote.NuGet.Commands.Mirroring;

public class PackageMirroringExecutor
{
    private readonly INuGetRepository _sourceRepository;
    private readonly INuGetRepository _destinationRepository;
    private readonly IPackageMirroringExecutorLogger _logger;

    public PackageMirroringExecutor(INuGetRepository sourceRepository, INuGetRepository destinationRepository, IPackageMirroringExecutorLogger logger)
    {
        _sourceRepository = sourceRepository ?? throw new ArgumentNullException(nameof(sourceRepository));
        _destinationRepository = destinationRepository ?? throw new ArgumentNullException(nameof(destinationRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> MirrorPackages(IReadOnlyCollection<PackageIdentity> packages, CancellationToken cancellationToken)
    {
        var total = packages.Count;

        if (total == 0)
        {
            return Result.Success();
        }

        _logger.LogStartMirroringPackagesCount(total);

        var current = 0;
        foreach (var package in packages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            current++;
            _logger.LogMirrorPackage(package, current, total);

            var promotionResult = await MirrorPackage(package, skipDuplicate: true, cancellationToken);
            if (promotionResult.IsFailure)
            {
                return promotionResult;
            }
        }

        _logger.LogMirroredPackagesCount(total);

        return Result.Success();
    }

    private async Task<Result> MirrorPackage(PackageIdentity identity, bool skipDuplicate, CancellationToken cancellationToken)
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

            var pushResult = await PushPackage(tempFilePath, skipDuplicate, cancellationToken);
            if (pushResult.IsFailure)
            {
                return pushResult;
            }
        }
        finally
        {
            File.Delete(tempFilePath);
        }

        return Result.Success();
    }

    private async Task<Result> DownloadPackage(PackageIdentity identity, string filePath, CancellationToken cancellationToken)
    {
        await using var packageStream = new FileStream(filePath, FileMode.Truncate, FileAccess.Write);

        var copyNupkgToStreamResult = await _sourceRepository.Packages.CopyNupkgToStream(identity, packageStream, cancellationToken);

        return copyNupkgToStreamResult;
    }

    private async Task<Result> PushPackage(string filePath, bool skipDuplicate, CancellationToken cancellationToken)
    {
        return await _destinationRepository.Packages.PushPackage(filePath, skipDuplicate, cancellationToken);
    }
}
