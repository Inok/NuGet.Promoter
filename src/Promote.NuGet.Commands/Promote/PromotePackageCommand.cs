using CSharpFunctionalExtensions;
using NuGet.Packaging.Core;
using Promote.NuGet.Commands.Core;
using Promote.NuGet.Feeds;

namespace Promote.NuGet.Commands.Promote;

public class PromotePackageCommand
{
    private readonly PackageVersionFinder _sourcePackageVersionFinder;
    private readonly PackagesToPromoteEvaluator _packagesToPromoteEvaluator;
    private readonly SinglePackagePromoter _singlePackagePromoter;
    private readonly IPromotePackageLogger _promotePackageLogger;
    private readonly PromotePackageToFindMatchingPackagesLoggerAdapter _promotePackageToFindMatchingPackagesLoggerAdapter;

    public PromotePackageCommand(INuGetRepository sourceRepository,
                                 INuGetRepository destinationRepository,
                                 IPromotePackageLogger promotePackageLogger)
    {
        if (sourceRepository == null) throw new ArgumentNullException(nameof(sourceRepository));
        if (destinationRepository == null) throw new ArgumentNullException(nameof(destinationRepository));
        if (promotePackageLogger == null) throw new ArgumentNullException(nameof(promotePackageLogger));

        _promotePackageLogger = promotePackageLogger;
        _sourcePackageVersionFinder = new PackageVersionFinder(sourceRepository);
        _packagesToPromoteEvaluator = new PackagesToPromoteEvaluator(sourceRepository, destinationRepository, promotePackageLogger);
        _singlePackagePromoter = new SinglePackagePromoter(sourceRepository, destinationRepository);
        _promotePackageToFindMatchingPackagesLoggerAdapter = new PromotePackageToFindMatchingPackagesLoggerAdapter(promotePackageLogger);
    }

    public async Task<UnitResult<string>> Promote(IReadOnlySet<PackageDependency> dependencies,
                                                  PromotePackageCommandOptions options,
                                                  CancellationToken cancellationToken = default)
    {
        if (dependencies == null) throw new ArgumentNullException(nameof(dependencies));
        if (options == null) throw new ArgumentNullException(nameof(options));

        _promotePackageLogger.LogResolvingMatchingPackages(dependencies);

        var packages = await _sourcePackageVersionFinder.FindMatchingPackages(dependencies,
                                                                              _promotePackageToFindMatchingPackagesLoggerAdapter,
                                                                              cancellationToken);
        if (packages.IsFailure)
        {
            return packages.Error;
        }

        return await Promote(packages.Value, options, cancellationToken);
    }

    public async Task<UnitResult<string>> Promote(PackageIdentity identity,
                                                  PromotePackageCommandOptions options,
                                                  CancellationToken cancellationToken = default)
    {
        if (identity == null) throw new ArgumentNullException(nameof(identity));
        if (options == null) throw new ArgumentNullException(nameof(options));

        var identities = new HashSet<PackageIdentity> { identity };
        return await Promote(identities, options, cancellationToken);
    }

    public async Task<UnitResult<string>> Promote(IReadOnlySet<PackageIdentity> identities,
                                                  PromotePackageCommandOptions options,
                                                  CancellationToken cancellationToken = default)
    {
        if (identities == null) throw new ArgumentNullException(nameof(identities));
        if (options == null) throw new ArgumentNullException(nameof(options));

        if (identities.Count == 0)
        {
            _promotePackageLogger.LogNoPackagesToPromote();
            return UnitResult.Success<string>();
        }

        var resolvedPackagesResult = await ResolvePackagesToPromote(identities, options, cancellationToken);
        if (resolvedPackagesResult.IsFailure)
        {
            return resolvedPackagesResult.Error;
        }

        if (options.DryRun)
        {
            return UnitResult.Success<string>();
        }

        return await PromotePackages(resolvedPackagesResult.Value, cancellationToken);
    }

    private async Task<Result<IReadOnlyCollection<PackageIdentity>, string>> ResolvePackagesToPromote(IReadOnlyCollection<PackageIdentity> identities,
                                                                                                      PromotePackageCommandOptions options,
                                                                                                      CancellationToken cancellationToken)
    {
        _promotePackageLogger.LogResolvingPackagesToPromote(identities);

        var packagesToPromote = await _packagesToPromoteEvaluator.ListPackagesToPromote(identities, options, cancellationToken);
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
        if (packages.Count == 0)
        {
            _promotePackageLogger.LogNoPackagesToPromote();
            return UnitResult.Success<string>();
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

        return UnitResult.Success<string>();
    }
}