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
    private readonly PackagesToPromoteEvaluator _packagesToPromoteEvaluator;
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
        _packagesToPromoteEvaluator = new PackagesToPromoteEvaluator(sourceRepository, destinationRepository, promotePackageLogger);
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
            _promotePackageLogger.LogNoPackagesToPromote();
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
        _promotePackageLogger.LogResolvingPackagesToPromote(identities);

        var packageTreeResult = await _packagesToPromoteEvaluator.ResolvePackageTree(identities, options, cancellationToken);
        if (packageTreeResult.IsFailure)
        {
            return Result.Failure<IReadOnlyCollection<PackageIdentity>>(packageTreeResult.Error);
        }

        var packageTree = packageTreeResult.Value;

        _promotePackageLogger.LogPackageResolutionTree(packageTree);

        var packagesToPromote = ListPackagesToPromote(packageTree, options).OrderBy(x => x).ToList();
        _promotePackageLogger.LogPackagesToPromote(packagesToPromote);

        return packagesToPromote;
    }

    private static IReadOnlySet<PackageIdentity> ListPackagesToPromote(PackageResolutionTree packageTree, PromotePackageCommandOptions options)
    {
        var packages = new HashSet<PackageIdentity>();
        foreach (var package in packageTree.AllPackages)
        {
            if (options.ForcePush || !packageTree.IsInTargetFeed(package))
            {
                packages.Add(package);
            }
        }

        return packages;
    }
}
