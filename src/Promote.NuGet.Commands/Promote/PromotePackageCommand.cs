using CSharpFunctionalExtensions;
using NuGet.Packaging.Core;
using Promote.NuGet.Commands.Licensing;
using Promote.NuGet.Commands.Mirroring;
using Promote.NuGet.Commands.Promote.Resolution;
using Promote.NuGet.Commands.Requests.Resolution;
using Promote.NuGet.Feeds;

namespace Promote.NuGet.Commands.Promote;

public class PromotePackageCommand
{
    private readonly INuGetRepository _sourceRepository;
    private readonly INuGetRepository _destinationRepository;
    private readonly LicenseComplianceValidator _licenseComplianceValidator;
    private readonly PackageMirroringExecutor _packageMirroringExecutor;
    private readonly IPromotePackageLogger _promotePackageLogger;
    private readonly TimeProvider _timeProvider;

    public PromotePackageCommand(INuGetRepository sourceRepository,
                                 INuGetRepository destinationRepository,
                                 IPromotePackageLogger promotePackageLogger,
                                 TimeProvider timeProvider)
    {
        if (sourceRepository == null) throw new ArgumentNullException(nameof(sourceRepository));
        if (destinationRepository == null) throw new ArgumentNullException(nameof(destinationRepository));
        if (promotePackageLogger == null) throw new ArgumentNullException(nameof(promotePackageLogger));

        _sourceRepository = sourceRepository;
        _destinationRepository = destinationRepository;
        _promotePackageLogger = promotePackageLogger;
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _packageMirroringExecutor = new PackageMirroringExecutor(sourceRepository, destinationRepository, promotePackageLogger);
        _licenseComplianceValidator = new LicenseComplianceValidator(sourceRepository, promotePackageLogger);
    }

    public async Task<Result> Promote(PromotePackageCommandArguments arguments,
                                      PromotePackageCommandOptions options,
                                      CancellationToken cancellationToken = default)
    {
        if (arguments == null) throw new ArgumentNullException(nameof(arguments));
        if (options == null) throw new ArgumentNullException(nameof(options));

        var sourcePackageRequestResolver = new PackageRequestResolver(_sourceRepository, _promotePackageLogger, arguments.MinimumReleaseAge, _timeProvider);

        var requests = arguments.Requests;
        var resolvePackagesResult = await sourcePackageRequestResolver.ResolvePackageRequests(requests, cancellationToken);
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

        var packagesToPromoteResult = await ResolvePackagesToPromote(identities, arguments.MinimumReleaseAge, options, cancellationToken);
        if (packagesToPromoteResult.IsFailure)
        {
            return Result.Failure(packagesToPromoteResult.Error);
        }

        var packagesToPromote = packagesToPromoteResult.Value;
        if (packagesToPromote.Count == 0)
        {
            return Result.Success();
        }

        var complianceResult = await _licenseComplianceValidator.CheckCompliance(packagesToPromote, arguments.LicenseComplianceSettings, cancellationToken);
        if (complianceResult.IsFailure)
        {
            return Result.Failure(complianceResult.Error);
        }

        if (options.DryRun)
        {
            _promotePackageLogger.LogDryRun();
            return Result.Success();
        }

        var idsToPromote = packagesToPromote.Select(x => x.Id).ToList();
        return await _packageMirroringExecutor.MirrorPackages(idsToPromote, cancellationToken);
    }

    private async Task<Result<IReadOnlyCollection<PackageInfo>>> ResolvePackagesToPromote(IReadOnlySet<PackageIdentity> identities,
                                                                                          TimeSpan? minimumReleaseAge,
                                                                                          PromotePackageCommandOptions options,
                                                                                          CancellationToken cancellationToken)
    {
        var packagesToPromoteResolver = new PackagesToPromoteResolver(_sourceRepository, _destinationRepository, _promotePackageLogger, minimumReleaseAge, _timeProvider);

        var packageTreeResult = await packagesToPromoteResolver.ResolvePackageTree(identities, options, cancellationToken);
        if (packageTreeResult.IsFailure)
        {
            return Result.Failure<IReadOnlyCollection<PackageInfo>>(packageTreeResult.Error);
        }

        var packageTree = packageTreeResult.Value;

        var packagesToPromote = packageTree.AllPackages
                                           .Where(x => options.ForcePush || !packageTree.IsInTargetFeed(x.Id))
                                           .OrderBy(x => x.Id)
                                           .ToList();

        if (packagesToPromote.Count > 0)
        {
            _promotePackageLogger.LogPackagesToPromote(packagesToPromote);
            _promotePackageLogger.LogLicenseSummary(packagesToPromote);
        }
        else
        {
            _promotePackageLogger.LogNoPackagesToPromote();
        }

        return packagesToPromote;
    }
}
