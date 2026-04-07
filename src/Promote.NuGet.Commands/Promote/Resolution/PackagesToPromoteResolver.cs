using CSharpFunctionalExtensions;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using Promote.NuGet.Commands.Core;
using Promote.NuGet.Commands.Licensing;
using Promote.NuGet.Feeds;

namespace Promote.NuGet.Commands.Promote.Resolution;

public class PackagesToPromoteResolver
{
    private readonly INuGetRepository _sourceRepository;
    private readonly INuGetRepository _destinationRepository;
    private readonly IPackagesToPromoteResolverLogger _logger;
    private readonly TimeSpan? _minimumReleaseAge;
    private readonly TimeProvider _timeProvider;

    public PackagesToPromoteResolver(INuGetRepository sourceRepository,
                                     INuGetRepository destinationRepository,
                                     IPackagesToPromoteResolverLogger logger,
                                     TimeSpan? minimumReleaseAge,
                                     TimeProvider timeProvider)
    {
        _sourceRepository = sourceRepository ?? throw new ArgumentNullException(nameof(sourceRepository));
        _destinationRepository = destinationRepository ?? throw new ArgumentNullException(nameof(destinationRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _minimumReleaseAge = minimumReleaseAge;
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
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
        using var packageInfoAccessor = new CachedNuGetPackageInfoAccessor(_sourceRepository.Packages, disposeInner: false);

        var resolvedPackages = new Dictionary<PackageIdentity, PackageInfo>();
        var packagesAlreadyInTarget = new HashSet<PackageIdentity>();
        var resolvedDependencies = new HashSet<(PackageIdentity Dependant, PackageIdentity Dependency)>();

        var context = new ResolutionContext(packagesToResolveQueue, packageInfoAccessor, resolvedPackages, packagesAlreadyInTarget, resolvedDependencies);

        while (packagesToResolveQueue.TryDequeue(out var packageIdentity))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var processingResult = await ProcessPackage(context, packageIdentity, options, cancellationToken);
            if (processingResult.IsFailure)
            {
                return Result.Failure<PackageResolutionTree>(processingResult.Error);
            }
        }

        var packageResolutionTree = PackageResolutionTree.CreateTree(resolvedPackages.Values, identities, packagesAlreadyInTarget, resolvedDependencies);

        _logger.LogResolvedPackageTree(packageResolutionTree);

        return packageResolutionTree;
    }

    private async Task<Result> ProcessPackage(ResolutionContext context,
                                              PackageIdentity identity,
                                              PromotePackageCommandOptions options,
                                              CancellationToken cancellationToken)
    {
        _logger.LogProcessingPackage(identity);

        var metadataResult = await _sourceRepository.Packages.GetPackageMetadata(identity, cancellationToken);
        if (metadataResult.IsFailure)
        {
            return Result.Failure(metadataResult.Error);
        }

        var metadata = metadataResult.Value;

        var licenseInfo = PackageLicenseInfoConverter.FromPackageSearchMetadata(metadata);
        _logger.LogPackageLicense(identity, licenseInfo);

        context.ResolvedPackages.Add(identity, new PackageInfo(identity, licenseInfo));

        var packageExistInDestinationResult = await _destinationRepository.Packages.DoesPackageExist(identity, cancellationToken);
        if (packageExistInDestinationResult.IsFailure)
        {
            return Result.Failure(packageExistInDestinationResult.Error);
        }

        var packageExistInDestination = packageExistInDestinationResult.Value;
        if (packageExistInDestination)
        {
            _logger.LogPackagePresentInDestination(identity);
            context.PackagesAlreadyInTarget.Add(identity);
        }
        else
        {
            _logger.LogPackageNotInDestination(identity);
        }

        if (!packageExistInDestination || options.AlwaysResolveDeps)
        {
            var processDependenciesResult = await ProcessPackageDependencies(context, metadata, cancellationToken);
            if (processDependenciesResult.IsFailure)
            {
                return Result.Failure(processDependenciesResult.Error);
            }
        }
        else
        {
            _logger.LogPackageDependenciesSkipped(identity);
        }

        return Result.Success();
    }

    private async Task<Result> ProcessPackageDependencies(ResolutionContext context, IPackageSearchMetadata metadata, CancellationToken cancellationToken)
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

            var processingResult = await ProcessDependency(context, metadata.Identity, dependency, cancellationToken);
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

    private async Task<Result<PackageIdentity>> ProcessDependency(ResolutionContext context,
                                                                  PackageIdentity source,
                                                                  DependencyDescriptor dependency,
                                                                  CancellationToken cancellationToken)
    {
        var dependencyId = dependency.Identity.Id;
        var dependencyVersionRange = dependency.VersionRange;

        _logger.LogResolvingDependency(source, dependency);

        var allVersionsOfDepResult = await context.PackageInfoAccessor.GetAllVersions(dependencyId, cancellationToken);
        if (allVersionsOfDepResult.IsFailure)
        {
            return Result.Failure<PackageIdentity>(allVersionsOfDepResult.Error);
        }

        var allVersions = allVersionsOfDepResult.Value;
        var bestMatchVersion = dependencyVersionRange.FindBestMatch(allVersions);

        if (_minimumReleaseAge is not { } minimumAge)
        {
            return await EnqueueResolvedDependency(context, source, dependencyId, bestMatchVersion);
        }

        var filteredVersionsResult = await FilterVersionsByAge(context, source, dependencyId, dependencyVersionRange, allVersions, minimumAge, cancellationToken);
        if (filteredVersionsResult.IsFailure)
        {
            return Result.Failure<PackageIdentity>(filteredVersionsResult.Error);
        }

        var filteredVersions = filteredVersionsResult.Value;
        var filteredBestMatch = dependencyVersionRange.FindBestMatch(filteredVersions);

        if (filteredBestMatch is null)
        {
            return Result.Failure<PackageIdentity>(
                $"Dependency {dependencyId} of {source.Id} {source.Version} has no version satisfying {dependencyVersionRange.PrettyPrint()} that meets the minimum release age.");
        }

        if (bestMatchVersion is not null && filteredBestMatch != bestMatchVersion)
        {
            _logger.LogDependencyResolvedToOlderVersionDueToAge(source, dependencyId, bestMatchVersion, filteredBestMatch);
        }

        return await EnqueueResolvedDependency(context, source, dependencyId, filteredBestMatch);
    }

    private async Task<Result<IReadOnlyCollection<NuGetVersion>>> FilterVersionsByAge(
        ResolutionContext context,
        PackageIdentity source,
        string dependencyId,
        VersionRange dependencyVersionRange,
        IReadOnlyCollection<NuGetVersion> allVersions,
        TimeSpan minimumAge,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var filtered = new List<NuGetVersion>();

        foreach (var version in allVersions)
        {
            if (!dependencyVersionRange.Satisfies(version))
            {
                continue;
            }

            var identity = new PackageIdentity(dependencyId, version);
            var metadataResult = await _sourceRepository.Packages.GetPackageMetadata(identity, cancellationToken);
            if (metadataResult.IsFailure)
            {
                return Result.Failure<IReadOnlyCollection<NuGetVersion>>(metadataResult.Error);
            }

            var published = metadataResult.Value.Published;
            if (published is null)
            {
                return Result.Failure<IReadOnlyCollection<NuGetVersion>>(
                    $"Package {dependencyId} {version} has no publish date. Cannot apply minimum release age filter.");
            }

            if ((now - published.Value) < minimumAge)
            {
                _logger.LogDependencyVersionSkippedDueToAge(source, dependencyId, version, published.Value);
                continue;
            }

            filtered.Add(version);
        }

        return filtered;
    }

    private async Task<Result<PackageIdentity>> EnqueueResolvedDependency(ResolutionContext context,
                                                                          PackageIdentity source,
                                                                          string dependencyId,
                                                                          NuGetVersion? bestMatchVersion)
    {
        var resolvedPackage = new PackageIdentity(dependencyId, bestMatchVersion);

        var enqueued = context.PackagesToResolveQueue.Enqueue(resolvedPackage);

        _logger.LogResolvedDependency(resolvedPackage, enqueued);

        context.ResolvedDependencies.Add((source, resolvedPackage));

        return Result.Success(resolvedPackage);
    }

    private record ResolutionContext(
        DistinctQueue<PackageIdentity> PackagesToResolveQueue,
        CachedNuGetPackageInfoAccessor PackageInfoAccessor,
        IDictionary<PackageIdentity, PackageInfo> ResolvedPackages,
        HashSet<PackageIdentity> PackagesAlreadyInTarget,
        HashSet<(PackageIdentity Dependant, PackageIdentity Dependency)> ResolvedDependencies
    );
}
