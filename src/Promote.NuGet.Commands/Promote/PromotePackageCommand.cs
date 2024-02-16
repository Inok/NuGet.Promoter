using CSharpFunctionalExtensions;
using NuGet.Packaging.Core;
using Promote.NuGet.Commands.PackageResolution;
using Promote.NuGet.Commands.Requests;
using Promote.NuGet.Feeds;

namespace Promote.NuGet.Commands.Promote;

public class PromotePackageCommand
{
    private readonly PackageRequestResolver _sourcePackageRequestResolver;
    private readonly PackagesToPromoteEvaluator _packagesToPromoteEvaluator;
    private readonly SinglePackagePromoter _singlePackagePromoter;
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
        _singlePackagePromoter = new SinglePackagePromoter(sourceRepository, destinationRepository);
    }

    public async Task<Result> Promote(IReadOnlyCollection<IPackageRequest> requests,
                                      PromotePackageCommandOptions options,
                                      CancellationToken cancellationToken = default)
    {
        if (requests == null) throw new ArgumentNullException(nameof(requests));
        if (options == null) throw new ArgumentNullException(nameof(options));

        var packages = await _sourcePackageRequestResolver.ResolvePackageRequests(requests, cancellationToken);
        if (packages.IsFailure)
        {
            return Result.Failure(packages.Error);
        }

        return await Promote(packages.Value, options, cancellationToken);
    }

    public async Task<Result> Promote(IReadOnlySet<PackageIdentity> identities,
                                      PromotePackageCommandOptions options,
                                      CancellationToken cancellationToken = default)
    {
        if (identities == null) throw new ArgumentNullException(nameof(identities));
        if (options == null) throw new ArgumentNullException(nameof(options));

        if (identities.Count == 0)
        {
            _promotePackageLogger.LogNoPackagesToPromote();
            return Result.Success();
        }

        var resolvedPackagesResult = await ResolvePackagesToPromote(identities, options, cancellationToken);
        if (resolvedPackagesResult.IsFailure)
        {
            return Result.Failure(resolvedPackagesResult.Error);
        }

        if (options.DryRun)
        {
            return Result.Success();
        }

        return await PromotePackages(resolvedPackagesResult.Value, cancellationToken);
    }

    private async Task<Result<IReadOnlyCollection<PackageIdentity>>> ResolvePackagesToPromote(IReadOnlyCollection<PackageIdentity> identities,
                                                                                                      PromotePackageCommandOptions options,
                                                                                                      CancellationToken cancellationToken)
    {
        _promotePackageLogger.LogResolvingPackagesToPromote(identities);

        var packagesToPromote = await _packagesToPromoteEvaluator.ListPackagesToPromote(identities, options, cancellationToken);
        if (packagesToPromote.IsFailure)
        {
            return Result.Failure<IReadOnlyCollection<PackageIdentity>>(packagesToPromote.Error);
        }

        var resolvedPackages = packagesToPromote.Value.OrderBy(x => x).ToList();

        if (resolvedPackages.Count > 0)
        {
            _promotePackageLogger.LogPackagesToPromote(resolvedPackages);
        }
        else
        {
            _promotePackageLogger.LogNoPackagesToPromote();
        }

        return resolvedPackages;
    }

    private async Task<Result> PromotePackages(IReadOnlyCollection<PackageIdentity> packages, CancellationToken cancellationToken)
    {
        if (packages.Count == 0)
        {
            return Result.Success();
        }

        var current = 0;
        var total = packages.Count;

        foreach (var package in packages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            current++;
            _promotePackageLogger.LogPromotePackage(package, current, total);

            var promotionResult = await _singlePackagePromoter.Promote(package, skipDuplicate: true, cancellationToken);
            if (promotionResult.IsFailure)
            {
                return promotionResult;
            }
        }

        _promotePackageLogger.LogPromotedPackagesCount(total);

        return Result.Success();
    }
}
