using CSharpFunctionalExtensions;
using NuGet.Packaging.Core;
using Promote.NuGet.Commands.Mirroring;
using Promote.NuGet.Commands.Promote.Resolution;
using Promote.NuGet.Commands.Requests;
using Promote.NuGet.Commands.Requests.Resolution;
using Promote.NuGet.Feeds;

namespace Promote.NuGet.Commands.Promote;

public class PromotePackageCommand
{
    private readonly PackageRequestResolver _sourcePackageRequestResolver;
    private readonly PackagesToPromoteResolver _packagesToPromoteResolver;
    private readonly PackageMirroringExecutor _packageMirroringExecutor;
    private readonly IPromotePackageLogger _promotePackageLogger;

    public PromotePackageCommand(INuGetRepository sourceRepository,
                                 INuGetRepository destinationRepository,
                                 IPromotePackageLogger promotePackageLogger)
    {
        if (sourceRepository == null) throw new ArgumentNullException(nameof(sourceRepository));
        if (destinationRepository == null) throw new ArgumentNullException(nameof(destinationRepository));
        if (promotePackageLogger == null) throw new ArgumentNullException(nameof(promotePackageLogger));

        _promotePackageLogger = promotePackageLogger;
        _sourcePackageRequestResolver = new PackageRequestResolver(sourceRepository, promotePackageLogger);
        _packagesToPromoteResolver = new PackagesToPromoteResolver(sourceRepository, destinationRepository, promotePackageLogger);
        _packageMirroringExecutor = new PackageMirroringExecutor(sourceRepository, destinationRepository, promotePackageLogger);
    }

    public async Task<Result> Promote(IReadOnlyCollection<PackageRequest> requests,
                                      PromotePackageCommandOptions options,
                                      CancellationToken cancellationToken = default)
    {
        if (requests == null) throw new ArgumentNullException(nameof(requests));
        if (options == null) throw new ArgumentNullException(nameof(options));

        var resolvePackagesResult = await _sourcePackageRequestResolver.ResolvePackageRequests(requests, cancellationToken);
        if (resolvePackagesResult.IsFailure)
        {
            return Result.Failure(resolvePackagesResult.Error);
        }

        var identities = resolvePackagesResult.Value;
        if (identities.Count == 0)
        {
            _promotePackageLogger.LogNoPackagesToPromote();
            return Result.Success();
        }

        var packagesToPromoteResult = await ResolvePackagesToPromote(identities, options, cancellationToken);
        if (packagesToPromoteResult.IsFailure)
        {
            return Result.Failure(packagesToPromoteResult.Error);
        }

        var packagesToPromote = packagesToPromoteResult.Value;
        if (packagesToPromote.Count == 0)
        {
            return Result.Success();
        }

        if (options.DryRun)
        {
            _promotePackageLogger.LogDryRun();
            return Result.Success();
        }

        return await _packageMirroringExecutor.MirrorPackages(packagesToPromote, cancellationToken);
    }

    private async Task<Result<IReadOnlyCollection<PackageIdentity>>> ResolvePackagesToPromote(IReadOnlySet<PackageIdentity> identities,
                                                                                              PromotePackageCommandOptions options,
                                                                                              CancellationToken cancellationToken)
    {
        var packageTreeResult = await _packagesToPromoteResolver.ResolvePackageTree(identities, options, cancellationToken);
        if (packageTreeResult.IsFailure)
        {
            return Result.Failure<IReadOnlyCollection<PackageIdentity>>(packageTreeResult.Error);
        }

        var packageTree = packageTreeResult.Value;

        var packagesToPromote = packageTree.AllPackages
                                           .Where(x => options.ForcePush || !packageTree.IsInTargetFeed(x.Id))
                                           .OrderBy(x => x.Id)
                                           .ToList();

        if (packagesToPromote.Count > 0)
        {
            _promotePackageLogger.LogPackagesToPromote(packagesToPromote);
        }
        else
        {
            _promotePackageLogger.LogNoPackagesToPromote();
        }

        return packagesToPromote.Select(x => x.Id).ToHashSet();
    }
}
