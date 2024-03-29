﻿using NuGet.Packaging.Core;
using Promote.NuGet.Commands.Core;

namespace Promote.NuGet.Commands.Promote.Resolution;

public sealed class PackageResolutionTree
{
    private readonly Dictionary<PackageIdentity, PackageInfo> _allPackages;
    private readonly HashSet<PackageIdentity> _roots;
    private readonly HashSet<PackageIdentity> _packagesInTargetFeed;
    private readonly Dictionary<PackageIdentity, HashSet<PackageIdentity>> _dependencies;

    public IReadOnlyCollection<PackageInfo> AllPackages => _allPackages.Values;
    public IReadOnlySet<PackageIdentity> Roots => _roots;

    private PackageResolutionTree()
    {
        _roots = new HashSet<PackageIdentity>();
        _allPackages = new Dictionary<PackageIdentity, PackageInfo>();
        _dependencies = new Dictionary<PackageIdentity, HashSet<PackageIdentity>>();
        _packagesInTargetFeed = new HashSet<PackageIdentity>();
    }

    public IReadOnlySet<PackageIdentity> GetDependencies(PackageIdentity identity)
    {
        if (!_allPackages.ContainsKey(identity)) throw new ArgumentException("The package is not in the tree");

        return _dependencies.TryGetValue(identity, out var deps) ? deps : new HashSet<PackageIdentity>();
    }

    public bool IsInTargetFeed(PackageIdentity identity)
    {
        if (!_allPackages.ContainsKey(identity)) throw new ArgumentException("The package is not in the tree");

        return _packagesInTargetFeed.Contains(identity);
    }

    public static PackageResolutionTree CreateTree(IReadOnlyCollection<PackageInfo> allPackages,
                                                   IReadOnlySet<PackageIdentity> roots,
                                                   IReadOnlySet<PackageIdentity> packagesInTargetFeed,
                                                   IReadOnlySet<(PackageIdentity Dependant, PackageIdentity Dependency)> dependencies)
    {
        var tree = new PackageResolutionTree();

        /* Add all packages */
        foreach (var package in allPackages)
        {
            tree._allPackages.Add(package.Id, package);
        }

        /* Setup roots */
        if (!roots.IsSubsetOf(allPackages.Select(x => x.Id)))
        {
            throw new InvalidOperationException("Roots are not a subset of all packages.");
        }

        foreach (var root in roots)
        {
            tree._roots.Add(root);
        }

        /* Setup packages in target feed */
        if (!packagesInTargetFeed.IsSubsetOf(allPackages.Select(x => x.Id)))
        {
            throw new InvalidOperationException("Packages in the target feed are not a subset of all packages.");
        }

        foreach (var packageInTargetFeed in packagesInTargetFeed)
        {
            tree._packagesInTargetFeed.Add(packageInTargetFeed);
        }

        /* Setup dependencies */
        foreach (var (dependant, dependency) in dependencies)
        {
            if (!tree._allPackages.ContainsKey(dependant) || !tree._allPackages.ContainsKey(dependency))
            {
                throw new InvalidOperationException("A dependency is pointing to a package that is not included in all packages.");
            }

            if (!tree._dependencies.TryGetValue(dependant, out var deps))
            {
                deps = new HashSet<PackageIdentity>();
                tree._dependencies.Add(dependant, deps);
            }

            deps.Add(dependency);
        }

        /* Check reachability */
        var reachable = new HashSet<PackageIdentity>();

        var toProcess = new DistinctQueue<PackageIdentity>(tree._roots);
        while (toProcess.TryDequeue(out var next))
        {
            if (!reachable.Add(next))
            {
                continue;
            }

            if (tree._dependencies.TryGetValue(next, out var nextDeps))
            {
                foreach (var nextDep in nextDeps)
                {
                    toProcess.Enqueue(nextDep);
                }
            }
        }

        if (!reachable.SetEquals(tree._allPackages.Keys))
        {
            throw new InvalidOperationException("The tree has packages unreachable from roots.");
        }

        return tree;
    }
}
