using CSharpFunctionalExtensions;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using Promote.NuGet.Feeds;

namespace Promote.NuGet.Commands.Promote;

public class PackagesToPromoteEvaluator
{
    private readonly INuGetRepository _sourceRepository;
    private readonly INuGetRepository _destinationRepository;
    private readonly IPromotePackageLogger _logger;

    public PackagesToPromoteEvaluator(INuGetRepository sourceRepository,
                                      INuGetRepository destinationRepository,
                                      IPromotePackageLogger logger)
    {
        _sourceRepository = sourceRepository ?? throw new ArgumentNullException(nameof(sourceRepository));
        _destinationRepository = destinationRepository ?? throw new ArgumentNullException(nameof(destinationRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<IReadOnlySet<PackageIdentity>, string>> ListPackagesToPromote(IReadOnlyCollection<PackageIdentity> identities,
                                                                                           PromotePackageCommandOptions options,
                                                                                           CancellationToken cancellationToken)
    {
        if (identities == null) throw new ArgumentNullException(nameof(identities));
        if (identities.Any(i => !i.HasVersion)) throw new ArgumentException("Version of a package must be specified.", nameof(identities));
        if (options == null) throw new ArgumentNullException(nameof(options));

        var packageQueue = new PackageResolutionQueue(identities);
        var dependencyQueue = new DependencyResolutionQueue();
        var versionFinder = new CachedPackageVersionFinder(_sourceRepository);

        var packagesToPromote = new HashSet<PackageIdentity>();

        while (packageQueue.HasPackagesToProcess || dependencyQueue.HasDependenciesToProcess)
        {
            // Resolve all packages known at this point
            while (packageQueue.TryDequeueAsResolved(out var packageIdentity))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var processingResult = await ProcessPackage(packageIdentity, options, packagesToPromote, dependencyQueue, cancellationToken);
                if (processingResult.IsFailure)
                {
                    return processingResult.Error;
                }
            }

            // Resolve dependencies known at this point
            while (dependencyQueue.TryDequeueAsResolved(out var dependencyIdentity, out var dependencyVersionRange))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var processingResult = await ProcessDependency(dependencyIdentity, dependencyVersionRange, packageQueue, versionFinder, cancellationToken);
                if (processingResult.IsFailure)
                {
                    return processingResult.Error;
                }
            }
        }

        return packagesToPromote;
    }

    private async Task<UnitResult<string>> ProcessPackage(PackageIdentity packageIdentity,
                                                          PromotePackageCommandOptions options,
                                                          ISet<PackageIdentity> packagesToPromote,
                                                          DependencyResolutionQueue dependencyResolutionQueue,
                                                          CancellationToken cancellationToken)
    {
        _logger.LogProcessingPackage(packageIdentity);

        var metadataResult = await _sourceRepository.Packages.GetPackageMetadata(packageIdentity, cancellationToken);
        if (metadataResult.IsFailure)
        {
            return metadataResult.Error;
        }

        var metadata = metadataResult.Value;

        if (options.ForcePush)
        {
            packagesToPromote.Add(packageIdentity);
            ProcessPackageDependencies(metadata, dependencyResolutionQueue);

            return Result.Success();
        }

        var packageExistInDestinationResult = await _destinationRepository.Packages.DoesPackageExist(packageIdentity, cancellationToken);
        if (packageExistInDestinationResult.IsFailure)
        {
            return packageExistInDestinationResult.Error;
        }

        var packageExistInDestination = packageExistInDestinationResult.Value;
        if (!packageExistInDestination)
        {
            packagesToPromote.Add(packageIdentity);
            ProcessPackageDependencies(metadata, dependencyResolutionQueue);

            return Result.Success();
        }

        _logger.LogPackagePresentInDestination(packageIdentity);

        if (options.AlwaysResolveDeps)
        {
            ProcessPackageDependencies(metadata, dependencyResolutionQueue);
        }

        return Result.Success();
    }

    private void ProcessPackageDependencies(IPackageSearchMetadata metadata, DependencyResolutionQueue dependencyResolutionQueue)
    {
        var depsById = metadata.DependencySets
                               .SelectMany(x => x.Packages)
                               .GroupBy(x => x.Id, x => x.VersionRange);

        foreach (var depAndRanges in depsById)
        {
            var dependencyId = depAndRanges.Key;

            foreach (var dependencyRange in depAndRanges)
            {
                if (dependencyResolutionQueue.EnqueueIfNotResolvedAlready(dependencyId, dependencyRange))
                {
                    _logger.LogNewDependencyToProcess(dependencyId, dependencyRange);
                }
            }
        }
    }

    private async Task<UnitResult<string>> ProcessDependency(string packageId,
                                                             VersionRange versionRange,
                                                             PackageResolutionQueue packageResolutionQueue,
                                                             CachedPackageVersionFinder versionFinder,
                                                             CancellationToken cancellationToken)
    {
        _logger.LogProcessingDependency(packageId, versionRange);

        var allVersionsOfDepResult = await versionFinder.GetAllVersions(packageId, cancellationToken);
        if (allVersionsOfDepResult.IsFailure)
        {
            return allVersionsOfDepResult.Error;
        }

        var bestMatchVersion = versionRange.FindBestMatch(allVersionsOfDepResult.Value);
        var bestMatchIdentity = new PackageIdentity(packageId, bestMatchVersion);

        if (packageResolutionQueue.EnqueueIfNotResolvedAlready(bestMatchIdentity))
        {
            _logger.LogNewDependencyFound(bestMatchIdentity);
        }

        return Result.Success();
    }
}