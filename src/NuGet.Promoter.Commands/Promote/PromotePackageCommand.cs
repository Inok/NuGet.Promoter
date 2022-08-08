using CSharpFunctionalExtensions;
using NuGet.Common;
using NuGet.Packaging.Core;
using NuGet.Promoter.Commands.Core;
using NuGet.Protocol.Core.Types;

namespace NuGet.Promoter.Commands.Promote;

public class PromotePackageCommand
{
    private readonly PackageDependenciesEvaluator _dependenciesEvaluator;
    private readonly PackageVersionFinder _packageVersionFinder;
    private readonly SinglePackagePromoter _singlePackagePromoter;
    private readonly IPromotePackageLogger _promotePackageLogger;

    public PromotePackageCommand(NuGetRepository sourceRepository,
                                 NuGetRepository destinationRepository,
                                 SourceCacheContext cacheContext,
                                 ILogger nugetLogger,
                                 IPromotePackageLogger promotePackageLogger)
    {
        _promotePackageLogger = promotePackageLogger ?? throw new ArgumentNullException(nameof(promotePackageLogger));
        _dependenciesEvaluator = new PackageDependenciesEvaluator(sourceRepository, cacheContext, nugetLogger, new PackageDependenciesEvaluatorLogger(promotePackageLogger));
        _packageVersionFinder = new PackageVersionFinder(sourceRepository, cacheContext, nugetLogger);
        _singlePackagePromoter = new SinglePackagePromoter(sourceRepository, destinationRepository, cacheContext, nugetLogger);
    }

    public async Task<UnitResult<string>> Promote(IReadOnlySet<PackageDependency> dependencies, bool dryRun, CancellationToken cancellationToken = default)
    {
        _promotePackageLogger.LogResolvingMatchingPackages(dependencies);

        var resolvedPackagesResult = await _packageVersionFinder.ResolvePackageAndDependencies(dependencies, cancellationToken);
        if (resolvedPackagesResult.IsFailure)
        {
            return resolvedPackagesResult.Error;
        }

        return await Promote(resolvedPackagesResult.Value, dryRun, cancellationToken);
    }

    public async Task<UnitResult<string>> Promote(PackageIdentity identity, bool dryRun, CancellationToken cancellationToken = default)
    {
        if (identity == null) throw new ArgumentNullException(nameof(identity));

        var identities = new HashSet<PackageIdentity> { identity };
        return await Promote(identities, dryRun, cancellationToken);
    }

    public async Task<UnitResult<string>> Promote(IReadOnlySet<PackageIdentity> identities, bool dryRun, CancellationToken cancellationToken = default)
    {
        if (identities == null) throw new ArgumentNullException(nameof(identities));

        var resolvedPackagesResult = await ResolvePackagesToPromote(identities, cancellationToken);
        if (resolvedPackagesResult.IsFailure)
        {
            return resolvedPackagesResult.Error;
        }

        if (dryRun)
        {
            return UnitResult.Success<string>();
        }

        return await PromotePackages(resolvedPackagesResult.Value, cancellationToken);
    }

    private async Task<Result<IReadOnlyCollection<PackageIdentity>, string>> ResolvePackagesToPromote(IReadOnlyCollection<PackageIdentity> identities,
                                                                                                      CancellationToken cancellationToken)
    {
        _promotePackageLogger.LogResolvingDependencies(identities);

        var packagesToPromote = await _dependenciesEvaluator.ResolvePackageAndDependencies(identities, cancellationToken);
        if (packagesToPromote.IsFailure)
        {
            return packagesToPromote.Error;
        }

        var resolvedPackages = packagesToPromote.Value.OrderBy(x => x).ToList();
        _promotePackageLogger.LogPackagesToPromote(resolvedPackages);

        return resolvedPackages;
    }

    private async Task<UnitResult<string>> PromotePackages(IReadOnlyCollection<PackageIdentity> packages, CancellationToken cancellationToken)
    {
        var current = 0;
        var total = packages.Count;

        foreach (var package in packages)
        {
            current++;
            _promotePackageLogger.LogPromotePackage(package, current, total);

            var promotionResult = await _singlePackagePromoter.Promote(package, skipDuplicate: true, cancellationToken);
            if (promotionResult.IsFailure)
            {
                return promotionResult;
            }
        }

        _promotePackageLogger.LogPromotedPackagesCount(total);

        return UnitResult.Success<string>();
    }
}