using CSharpFunctionalExtensions;
using NuGet.Packaging.Core;
using Promote.NuGet.Commands.Core;
using Promote.NuGet.Feeds;

namespace Promote.NuGet.Commands.Promote;

public class PromotePackageCommand
{
    private readonly PackageVersionFinder _sourcePackageVersionFinder;
    private readonly PackageDependenciesEvaluator _dependenciesEvaluator;
    private readonly SinglePackagePromoter _singlePackagePromoter;
    private readonly IPromotePackageLogger _promotePackageLogger;
    private readonly PromotePackageToFindMatchingPackagesLoggerAdapter _promotePackageToFindMatchingPackagesLoggerAdapter;
    private readonly PackageVersionFinder _destinationPackageVersionFinder;

    public PromotePackageCommand(INuGetRepository sourceRepository,
                                 INuGetRepository destinationRepository,
                                 IPromotePackageLogger promotePackageLogger)
    {
        _promotePackageLogger = promotePackageLogger ?? throw new ArgumentNullException(nameof(promotePackageLogger));
        _sourcePackageVersionFinder = new PackageVersionFinder(sourceRepository);
        _destinationPackageVersionFinder = new PackageVersionFinder(destinationRepository);

        var packageDependenciesEvaluatorLoggerAdapter = new PromotePackageToPackageDependenciesEvaluatorLoggerAdapter(promotePackageLogger);
        _dependenciesEvaluator = new PackageDependenciesEvaluator(sourceRepository, packageDependenciesEvaluatorLoggerAdapter);

        _singlePackagePromoter = new SinglePackagePromoter(sourceRepository, destinationRepository);

        _promotePackageToFindMatchingPackagesLoggerAdapter = new PromotePackageToFindMatchingPackagesLoggerAdapter(promotePackageLogger);
    }

    public async Task<UnitResult<string>> Promote(IReadOnlySet<PackageDependency> dependencies,
                                                  bool dryRun,
                                                  bool force,
                                                  CancellationToken cancellationToken = default)
    {
        _promotePackageLogger.LogResolvingMatchingPackages(dependencies);

        var packages = await _sourcePackageVersionFinder.FindMatchingPackages(dependencies, _promotePackageToFindMatchingPackagesLoggerAdapter,
                                                                              cancellationToken);
        if (packages.IsFailure)
        {
            return packages.Error;
        }

        return await Promote(packages.Value, dryRun, force, cancellationToken);
    }

    public async Task<UnitResult<string>> Promote(PackageIdentity identity,
                                                  bool dryRun,
                                                  bool force,
                                                  CancellationToken cancellationToken = default)
    {
        if (identity == null) throw new ArgumentNullException(nameof(identity));

        var identities = new HashSet<PackageIdentity> { identity };
        return await Promote(identities, dryRun, force, cancellationToken);
    }

    public async Task<UnitResult<string>> Promote(IReadOnlySet<PackageIdentity> identities,
                                                  bool dryRun,
                                                  bool force,
                                                  CancellationToken cancellationToken = default)
    {
        if (identities == null) throw new ArgumentNullException(nameof(identities));

        if (!force)
        {
            var filteredPackages = await FilterAlreadyPresentPackages(identities, cancellationToken);
            if (filteredPackages.IsFailure)
            {
                return filteredPackages.Error;
            }

            identities = filteredPackages.Value;
        }

        if (identities.Count == 0)
        {
            _promotePackageLogger.LogNoPackagesToPromote();
            return UnitResult.Success<string>();
        }

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

    private async Task<Result<IReadOnlySet<PackageIdentity>, string>> FilterAlreadyPresentPackages(IReadOnlySet<PackageIdentity> identities,
                                                                                                   CancellationToken cancellationToken)
    {
        _promotePackageLogger.LogFilteringPresentPackages(identities);

        var result = new HashSet<PackageIdentity>();
        foreach (var identity in identities)
        {
            var packageExistResult = await _destinationPackageVersionFinder.DoesPackageExist(identity, cancellationToken);
            if (packageExistResult.IsFailure)
            {
                return packageExistResult.Error;
            }

            var packageExists = packageExistResult.Value;
            if (packageExists)
            {
                _promotePackageLogger.LogPackagePresentInDestination(identity);
                continue;
            }

            result.Add(identity);
        }

        return result;
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