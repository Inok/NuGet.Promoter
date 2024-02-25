using CSharpFunctionalExtensions;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
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
        var packageInfoAccessor = new CachedNuGetPackageInfoAccessor(_sourceRepository.Packages);

        var resolvedPackages = new HashSet<PackageIdentity>();
        var packagesAlreadyInTarget = new HashSet<PackageIdentity>();
        var resolvedDependencies = new HashSet<(PackageIdentity Dependant, PackageIdentity Dependency)>();

        while (packagesToResolveQueue.TryDequeue(out var packageIdentity))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var processingResult = await ProcessPackage(packageIdentity, options, resolvedPackages, packagesAlreadyInTarget, resolvedDependencies, packagesToResolveQueue, packageInfoAccessor, cancellationToken);
            if (processingResult.IsFailure)
            {
                return Result.Failure<PackageResolutionTree>(processingResult.Error);
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
                                              ISet<(PackageIdentity Dependant, PackageIdentity Dependency)> resolvedDependencies,
                                              DistinctQueue<PackageIdentity> packagesToResolveQueue,
                                              INuGetPackageInfoAccessor packageInfoAccessor,
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
        else
        {
            _logger.LogPackageNotInDestination(packageIdentity);
        }

        if (!packageExistInDestination || options.AlwaysResolveDeps)
        {
            var processDependenciesResult = await ProcessPackageDependencies(metadata, packagesToResolveQueue, resolvedDependencies, packageInfoAccessor, cancellationToken);
            if (processDependenciesResult.IsFailure)
            {
                return Result.Failure(processDependenciesResult.Error);
            }
        }
        else
        {
            _logger.LogPackageDependenciesSkipped(packageIdentity);
        }

        return Result.Success();
    }

    private async Task<Result> ProcessPackageDependencies(IPackageSearchMetadata metadata,
                                                          DistinctQueue<PackageIdentity> packagesToResolveQueue,
                                                          ISet<(PackageIdentity Dependant, PackageIdentity Dependency)> resolvedDependencies,
                                                          INuGetPackageInfoAccessor packageInfoAccessor,
                                                          CancellationToken cancellationToken)
    {
        var dependencies = ListPackageDependencies(metadata);
        if (dependencies.Count == 0)
        {
            _logger.LogNoDependencies(metadata.Identity);
            return Result.Success();
        }

        foreach (var dependency in dependencies)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var processingResult = await ProcessDependency(metadata.Identity, dependency, packagesToResolveQueue, resolvedDependencies, packageInfoAccessor, cancellationToken);
            if (processingResult.IsFailure)
            {
                return Result.Failure(processingResult.Error);
            }
        }

        return Result.Success();
    }

    private static IReadOnlySet<DependencyDescriptor> ListPackageDependencies(IPackageSearchMetadata metadata)
    {
        var dependencies = metadata.DependencySets.SelectMany(x => x.Packages);

        var dependencyDescriptors = new HashSet<DependencyDescriptor>();
        foreach (var dependency in dependencies)
        {
            var dependencyId = new PackageIdentity(dependency.Id, null);
            var dependencyDescriptor = new DependencyDescriptor(dependencyId, dependency.VersionRange);

            dependencyDescriptors.Add(dependencyDescriptor);
        }

        return dependencyDescriptors;
    }

    private async Task<Result<PackageIdentity>> ProcessDependency(PackageIdentity source,
                                                                  DependencyDescriptor dependency,
                                                                  DistinctQueue<PackageIdentity> packageResolutionQueue,
                                                                  ISet<(PackageIdentity Dependant, PackageIdentity Dependency)> resolvedDependencies,
                                                                  INuGetPackageInfoAccessor packageInfoAccessor,
                                                                  CancellationToken cancellationToken)
    {
        var dependencyId = dependency.Identity.Id;
        var dependencyVersionRange = dependency.VersionRange;

        _logger.LogResolvingDependency(source, dependency);

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
        else
        {
            _logger.LogPackageIsAlreadyProcessedOrQueued(resolvedPackage);
        }

        resolvedDependencies.Add((source, resolvedPackage));

        return Result.Success(resolvedPackage);
    }
}
