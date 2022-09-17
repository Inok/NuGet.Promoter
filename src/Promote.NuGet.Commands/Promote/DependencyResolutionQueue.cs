using System.Diagnostics.CodeAnalysis;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace Promote.NuGet.Commands.Promote;

internal class DependencyResolutionQueue
{
    private sealed record Dependency(PackageIdentity Identity, VersionRange VersionRange);

    private readonly HashSet<Dependency> _depsToResolve;
    private readonly HashSet<Dependency> _resolvedDeps;

    public bool HasDependenciesToProcess => _depsToResolve.Count > 0;

    public DependencyResolutionQueue()
    {
        _depsToResolve = new HashSet<Dependency>();
        _resolvedDeps = new HashSet<Dependency>();
    }

    public bool EnqueueIfNotResolvedAlready(string packageId, VersionRange versionRange)
    {
        if (string.IsNullOrEmpty(packageId)) throw new ArgumentException("Value cannot be null or empty.", nameof(packageId));
        if (versionRange == null) throw new ArgumentNullException(nameof(versionRange));

        var identity = new PackageIdentity(packageId, null);
        var key = new Dependency(identity, versionRange);
        if (_resolvedDeps.Contains(key))
        {
            return false;
        }

        return _depsToResolve.Add(key);
    }

    public bool TryDequeueAsResolved([NotNullWhen(true)] out string? packageId, [NotNullWhen(true)] out VersionRange? versionRange)
    {
        if (_depsToResolve.Count == 0)
        {
            packageId = null;
            versionRange = null;
            return false;
        }

        var dependency = _depsToResolve.First();
        _depsToResolve.Remove(dependency);
        _resolvedDeps.Add(dependency);

        packageId = dependency.Identity.Id;
        versionRange = dependency.VersionRange;
        return true;
    }
}