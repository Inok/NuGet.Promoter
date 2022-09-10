using CSharpFunctionalExtensions;
using NuGet.Packaging.Core;
using Promote.NuGet.Commands.Core;
using Promote.NuGet.Feeds;

namespace Promote.NuGet.Commands.Promote;

public class PromotePackageCommand
{
    private readonly PackageVersionFinder _packageVersionFinder;
    private readonly PackageDependenciesEvaluator _dependenciesEvaluator;
    private readonly SinglePackagePromoter _singlePackagePromoter;
    private readonly IPromotePackageLogger _promotePackageLogger;
    private readonly PromotePackageToFindMatchingPackagesLoggerAdapter _promotePackageToFindMatchingPackagesLoggerAdapter;

    public PromotePackageCommand(INuGetRepository sourceRepository,
                                 INuGetRepository destinationRepository,
                                 IPromotePackageLogger promotePackageLogger)
    {
        _promotePackageLogger = promotePackageLogger ?? throw new ArgumentNullException(nameof(promotePackageLogger));
        _packageVersionFinder = new PackageVersionFinder(sourceRepository);

        var packageDependenciesEvaluatorLoggerAdapter = new PromotePackageToPackageDependenciesEvaluatorLoggerAdapter(promotePackageLogger);
        _dependenciesEvaluator = new PackageDependenciesEvaluator(sourceRepository, packageDependenciesEvaluatorLoggerAdapter);

        _singlePackagePromoter = new SinglePackagePromoter(sourceRepository, destinationRepository);

        _promotePackageToFindMatchingPackagesLoggerAdapter = new PromotePackageToFindMatchingPackagesLoggerAdapter(promotePackageLogger);
    }

    public async Task<UnitResult<string>> Promote(IReadOnlySet<PackageDependency> dependencies, bool dryRun, CancellationToken cancellationToken = default)
    {
        _promotePackageLogger.LogResolvingMatchingPackages(dependencies);

        var resolvedPackagesResult =
            await _packageVersionFinder.FindMatchingPackages(dependencies, _promotePackageToFindMatchingPackagesLoggerAdapter, cancellationToken);
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

        return UnitResult.Success<string>();
    }
}