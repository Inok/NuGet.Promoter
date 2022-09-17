using System.Diagnostics.CodeAnalysis;
using NuGet.Packaging.Core;

namespace Promote.NuGet.Commands.Promote;

internal class PackageResolutionQueue
{
    private readonly HashSet<PackageIdentity> _packagesToResolve;
    private readonly HashSet<PackageIdentity> _resolvedPackages;

    public bool HasPackagesToProcess => _packagesToResolve.Count > 0;

    public PackageResolutionQueue(IReadOnlyCollection<PackageIdentity> packageIdentities)
    {
        if (packageIdentities == null) throw new ArgumentNullException(nameof(packageIdentities));

        _packagesToResolve = new HashSet<PackageIdentity>(packageIdentities);
        _resolvedPackages = new HashSet<PackageIdentity>();
    }

    public bool EnqueueIfNotResolvedAlready(PackageIdentity identity)
    {
        if (identity == null) throw new ArgumentNullException(nameof(identity));

        if (_resolvedPackages.Contains(identity))
        {
            return false;
        }

        return _packagesToResolve.Add(identity);
    }

    public bool TryDequeueAsResolved([NotNullWhen(true)] out PackageIdentity? identity)
    {
        if (_packagesToResolve.Count == 0)
        {
            identity = null;
            return false;
        }

        identity = _packagesToResolve.First();

        _packagesToResolve.Remove(identity);
        _resolvedPackages.Add(identity);

        return true;
    }
}