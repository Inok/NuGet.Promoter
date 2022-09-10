using CSharpFunctionalExtensions;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using Promote.NuGet.Feeds;

namespace Promote.NuGet.Commands.Core;

public class PackageDependenciesEvaluator
{
    private readonly INuGetRepository _repository;
    private readonly IPackageDependenciesEvaluatorLogger _logger;

    public PackageDependenciesEvaluator(INuGetRepository repository, IPackageDependenciesEvaluatorLogger logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<IReadOnlySet<PackageIdentity>, string>> ResolvePackageAndDependencies(IReadOnlyCollection<PackageIdentity> identities,
                                                                                                   CancellationToken cancellationToken)
    {
        if (identities == null) throw new ArgumentNullException(nameof(identities));
        if (identities.Any(i => !i.HasVersion)) throw new ArgumentException("Version of a package must be specified.", nameof(identities));

        var resolvedPackages = new HashSet<PackageIdentity>();

        var packagesToResolve = new HashSet<PackageIdentity>(identities);

        var packageVersionsCache = new Dictionary<PackageIdentity, IReadOnlyCollection<NuGetVersion>>();
        var resolvedDeps = new HashSet<(PackageIdentity, VersionRange)>();

        while (packagesToResolve.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var packageToResolve = packagesToResolve.First();
            packagesToResolve.Remove(packageToResolve);
            resolvedPackages.Add(packageToResolve);

            _logger.LogProcessingDependenciesOfPackage(packageToResolve);

            var metadataResult = await _repository.Packages.GetPackageMetadata(packageToResolve, cancellationToken);
            if (metadataResult.IsFailure)
            {
                return metadataResult.Error;
            }

            var metadata = metadataResult.Value;

            var depsById = metadata.DependencySets.SelectMany(x => x.Packages).GroupBy(x => x.Id, x => x.VersionRange);

            foreach (var depAndRanges in depsById)
            {
                cancellationToken.ThrowIfCancellationRequested();

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
                    var allVersionsResult = await _repository.Packages.GetAllVersions(dependencyIdentity.Id, cancellationToken);
                    if (allVersionsResult.IsFailure)
                    {
                        return allVersionsResult.Error;
                    }

                    allVersionsOfDep = allVersionsResult.Value;
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