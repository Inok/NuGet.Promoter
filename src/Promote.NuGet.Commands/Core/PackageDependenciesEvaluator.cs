using CSharpFunctionalExtensions;
using NuGet.Common;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Promote.NuGet.Commands.Core;

public class PackageDependenciesEvaluator
{
    private readonly NuGetRepository _repository;
    private readonly SourceCacheContext _cacheContext;
    private readonly ILogger _nugetLogger;
    private readonly IPackageDependenciesEvaluatorLogger _logger;

    public PackageDependenciesEvaluator(NuGetRepository repository, SourceCacheContext cacheContext, ILogger nugetLogger, IPackageDependenciesEvaluatorLogger logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _cacheContext = cacheContext ?? throw new ArgumentNullException(nameof(cacheContext));
        _nugetLogger = nugetLogger ?? throw new ArgumentNullException(nameof(nugetLogger));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<IReadOnlySet<PackageIdentity>, string>> ResolvePackageAndDependencies(IReadOnlyCollection<PackageIdentity> identities,
                                                                                                   CancellationToken cancellationToken)
    {
        if (identities == null) throw new ArgumentNullException(nameof(identities));
        if (identities.Any(i => !i.HasVersion)) throw new ArgumentException("Version of a package must be specified.", nameof(identities));

        var findResource = await _repository.Repository.GetResourceAsync<FindPackageByIdResource>(cancellationToken);
        var packageMetadataResource = await _repository.Repository.GetResourceAsync<PackageMetadataResource>(cancellationToken);

        var resolvedPackages = new HashSet<PackageIdentity>();

        var packagesToResolve = new HashSet<PackageIdentity>(identities);

        var packageVersionsCache = new Dictionary<PackageIdentity, IReadOnlyCollection<NuGetVersion>>();
        var resolvedDeps = new HashSet<(PackageIdentity, VersionRange)>();

        while (packagesToResolve.Count > 0)
        {
            var packageToResolve = packagesToResolve.First();
            packagesToResolve.Remove(packageToResolve);
            resolvedPackages.Add(packageToResolve);

            _logger.LogProcessingDependenciesOfPackage(packageToResolve);

            var meta = await packageMetadataResource.GetMetadataAsync(packageToResolve, _cacheContext, _nugetLogger, cancellationToken);
            if (meta == null)
            {
                return $"Package {packageToResolve} not found.";
            }

            var depsById = meta.DependencySets.SelectMany(x => x.Packages).GroupBy(x => x.Id, x => x.VersionRange);

            foreach (var depAndRanges in depsById)
            {
                var dependencyIdentity = new PackageIdentity(depAndRanges.Key, null);
                var dependencyRanges = depAndRanges.ToHashSet();

                foreach (var dependencyRange in dependencyRanges.ToList())
                {
                    var dep = (packageIdentity: dependencyIdentity, dependencyRange);
                    if (!resolvedDeps.Add(dep))
                    {
                        dependencyRanges.Remove(dependencyRange);
                    }
                }

                if (dependencyRanges.Count == 0)
                {
                    continue;
                }

                if (!packageVersionsCache.TryGetValue(dependencyIdentity, out var allVersionsOfDep))
                {
                    allVersionsOfDep = (await findResource.GetAllVersionsAsync(dependencyIdentity.Id, _cacheContext, _nugetLogger, cancellationToken)).ToList();
                    packageVersionsCache.Add(dependencyIdentity, allVersionsOfDep);
                }

                var requiredVersions = dependencyRanges.Select(r => r.FindBestMatch(allVersionsOfDep))
                                                       .Select(x => new PackageIdentity(dependencyIdentity.Id, x))
                                                       .ToList();

                foreach (var requiredVersion in requiredVersions)
                {
                    if (resolvedPackages.Contains(requiredVersion))
                    {
                        continue;
                    }

                    if (packagesToResolve.Add(requiredVersion))
                    {
                        _logger.LogNewDependencyFound(requiredVersion);
                    }
                }
            }
        }

        return resolvedPackages;
    }
}