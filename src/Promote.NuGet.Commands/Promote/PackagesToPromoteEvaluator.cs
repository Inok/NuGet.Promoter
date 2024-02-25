using CSharpFunctionalExtensions;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using Promote.NuGet.Commands.Core;
using Promote.NuGet.Commands.Promote.Resolution;
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

    public async Task<Result<PackageResolutionTree>> ResolvePackageTree(IReadOnlySet<PackageIdentity> identities,
                                                                        PromotePackageCommandOptions options,
                                                                        CancellationToken cancellationToken)
    {
        if (identities == null) throw new ArgumentNullException(nameof(identities));
        if (identities.Any(i => !i.HasVersion)) throw new ArgumentException("Version of a package must be specified.", nameof(identities));
        if (options == null) throw new ArgumentNullException(nameof(options));

        var packagesToResolveQueue = new DistinctQueue<PackageIdentity>(identities);
        var dependenciesToResolveQueue = new DistinctQueue<DependencyRequest>();
        var packageInfoAccessor = new CachedNuGetPackageInfoAccessor(_sourceRepository.Packages);

        var resolvedPackages = new HashSet<PackageIdentity>();
        var packagesAlreadyInTarget = new HashSet<PackageIdentity>();
        var resolvedDependencies = new HashSet<(PackageIdentity Dependant, PackageIdentity Dependency)>();

        while (packagesToResolveQueue.HasItems || dependenciesToResolveQueue.HasItems)
        {
            // Resolve all packages known at this point
            while (packagesToResolveQueue.TryDequeue(out var packageIdentity))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var processingResult = await ProcessPackage(packageIdentity, options, resolvedPackages, packagesAlreadyInTarget, dependenciesToResolveQueue, cancellationToken);
                if (processingResult.IsFailure)
                {
                    return Result.Failure<PackageResolutionTree>(processingResult.Error);
                }
            }

            // Resolve dependencies known at this point
            while (dependenciesToResolveQueue.TryDequeue(out var dependency))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var processingResult = await ProcessDependencyRequest(dependency, packagesToResolveQueue, resolvedDependencies, packageInfoAccessor, cancellationToken);
                if (processingResult.IsFailure)
                {
                    return Result.Failure<PackageResolutionTree>(processingResult.Error);
                }
            }
        }

        return PackageResolutionTree.CreateTree(resolvedPackages, identities, packagesAlreadyInTarget, resolvedDependencies);
    }

    private async Task<Result> ProcessPackage(PackageIdentity packageIdentity,
                                              PromotePackageCommandOptions options,
                                              ISet<PackageIdentity> resolvedPackages,
                                              ISet<PackageIdentity> packagesAlreadyInTarget,
                                              DistinctQueue<DependencyRequest> dependencyResolutionQueue,
                                              CancellationToken cancellationToken)
    {
        _logger.LogProcessingPackage(packageIdentity);

        var metadataResult = await _sourceRepository.Packages.GetPackageMetadata(packageIdentity, cancellationToken);
        if (metadataResult.IsFailure)
        {
            return Result.Failure(metadataResult.Error);
        }

        resolvedPackages.Add(packageIdentity);

        var metadata = metadataResult.Value;

        var packageExistInDestinationResult = await _destinationRepository.Packages.DoesPackageExist(packageIdentity, cancellationToken);
        if (packageExistInDestinationResult.IsFailure)
        {
            return Result.Failure(packageExistInDestinationResult.Error);
        }

        var packageExistInDestination = packageExistInDestinationResult.Value;
        if (packageExistInDestination)
        {
            _logger.LogPackagePresentInDestination(packageIdentity);
            packagesAlreadyInTarget.Add(packageIdentity);
        }

        if (!packageExistInDestination || options.AlwaysResolveDeps)
        {
            ProcessPackageDependencies(metadata, dependencyResolutionQueue);
        }

        return Result.Success();
    }

    private void ProcessPackageDependencies(IPackageSearchMetadata metadata, DistinctQueue<DependencyRequest> dependencyResolutionQueue)
    {
        var dependencies = metadata.DependencySets.SelectMany(x => x.Packages);

        foreach (var dependency in dependencies)
        {
            var dependencyId = new PackageIdentity(dependency.Id, null);
            var dependencyRequest = new DependencyRequest(metadata.Identity, dependencyId, dependency.VersionRange);

            if (dependencyResolutionQueue.Enqueue(dependencyRequest))
            {
                _logger.LogNewDependencyToProcess(metadata.Identity, dependencyId.Id, dependency.VersionRange);
            }
        }
    }

    private async Task<Result<PackageIdentity>> ProcessDependencyRequest(DependencyRequest request,
                                                                         DistinctQueue<PackageIdentity> packageResolutionQueue,
                                                                         ISet<(PackageIdentity Dependant, PackageIdentity Dependency)> resolvedDependencies,
                                                                         INuGetPackageInfoAccessor packageInfoAccessor,
                                                                         CancellationToken cancellationToken)
    {
        var source = request.Source;
        var dependencyId = request.Identity.Id;
        var dependencyVersionRange = request.VersionRange;

        _logger.LogProcessingDependency(source, dependencyId, dependencyVersionRange);

        var allVersionsOfDepResult = await packageInfoAccessor.GetAllVersions(dependencyId, cancellationToken);
        if (allVersionsOfDepResult.IsFailure)
        {
            return Result.Failure<PackageIdentity>(allVersionsOfDepResult.Error);
        }

        var bestMatchVersion = dependencyVersionRange.FindBestMatch(allVersionsOfDepResult.Value);
        var resolvedPackage = new PackageIdentity(dependencyId, bestMatchVersion);

        _logger.LogResolvedDependency(source, resolvedPackage);

        if (packageResolutionQueue.Enqueue(resolvedPackage))
        {
            _logger.LogNewDependencyFound(resolvedPackage);
        }

        resolvedDependencies.Add((source, resolvedPackage));

        return Result.Success(resolvedPackage);
    }

    private sealed record DependencyRequest(PackageIdentity Source, PackageIdentity Identity, VersionRange VersionRange);
}
