using System.IO;
using CSharpFunctionalExtensions;
using NuGet.Packaging.Core;
using Promote.NuGet.Feeds;

namespace Promote.NuGet.Commands.Promote;

public class SinglePackagePromoter
{
    private readonly INuGetRepository _sourceRepository;
    private readonly INuGetRepository _destinationRepository;

    public SinglePackagePromoter(INuGetRepository sourceRepository, INuGetRepository destinationRepository)
    {
        _sourceRepository = sourceRepository ?? throw new ArgumentNullException(nameof(sourceRepository));
        _destinationRepository = destinationRepository ?? throw new ArgumentNullException(nameof(destinationRepository));
    }

    public async Task<Result> Promote(PackageIdentity identity, bool skipDuplicate, CancellationToken cancellationToken)
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