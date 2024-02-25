using CSharpFunctionalExtensions;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using Promote.NuGet.Commands.Core;
using Promote.NuGet.Feeds;

namespace Promote.NuGet.Commands.Promote.Resolution;

public class PackagesToPromoteResolver
{
    private readonly INuGetRepository _sourceRepository;
    private readonly INuGetRepository _destinationRepository;
    private readonly IPackagesToPromoteResolverLogger _logger;

    public PackagesToPromoteResolver(INuGetRepository sourceRepository,
                                     INuGetRepository destinationRepository,
                                     IPackagesToPromoteResolverLogger logger)
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

        _logger.LogResolvingPackagesToPromote(identities);

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

        var packageResolutionTree = PackageResolutionTree.CreateTree(resolvedPackages, identities, packagesAlreadyInTarget, resolvedDependencies);

        _logger.LogResolvedPackageTree(packageResolutionTree);

        return packageResolutionTree;
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
            EnqueuePackageDependencies(metadata, dependencyResolutionQueue);
        }

        return Result.Success();
    }

    private void EnqueuePackageDependencies(IPackageSearchMetadata metadata, DistinctQueue<DependencyRequest> dependencyResolutionQueue)
    {
        var dependencies = metadata.DependencySets.SelectMany(x => x.Packages);

        var dependencyRequests = new HashSet<DependencyDescriptor>();
        foreach (var dependency in dependencies)
        {
            var dependencyId = new PackageIdentity(dependency.Id, null);
            var dependencyDescriptor = new DependencyDescriptor(dependencyId, dependency.VersionRange);

            dependencyRequests.Add(dependencyDescriptor);
        }

        foreach (var descriptor in dependencyRequests)
        {
            dependencyResolutionQueue.Enqueue(new DependencyRequest(metadata.Identity, descriptor.Identity, descriptor.VersionRange));
        }

        _logger.LogPackageDependenciesQueuedForResolving(metadata.Identity, dependencyRequests);
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

        _logger.LogResolvingDependency(source, dependencyId, dependencyVersionRange);

        var allVersionsOfDepResult = await packageInfoAccessor.GetAllVersions(dependencyId, cancellationToken);
        if (allVersionsOfDepResult.IsFailure)
        {
            return Result.Failure<PackageIdentity>(allVersionsOfDepResult.Error);
        }

        var bestMatchVersion = dependencyVersionRange.FindBestMatch(allVersionsOfDepResult.Value);
        var resolvedPackage = new PackageIdentity(dependencyId, bestMatchVersion);

        _logger.LogResolvedDependency(resolvedPackage);

        if (packageResolutionQueue.Enqueue(resolvedPackage))
        {
            _logger.LogNewPackageQueuedForProcessing(resolvedPackage);
        }

        resolvedDependencies.Add((source, resolvedPackage));

        return Result.Success(resolvedPackage);
    }

    public sealed record DependencyRequest(PackageIdentity Source, PackageIdentity Identity, VersionRange VersionRange);
}
