using NuGet.Packaging.Core;
using NuGet.Versioning;
using Promote.NuGet.Commands.Mirroring;
using Promote.NuGet.Commands.Requests.Resolution;

namespace Promote.NuGet.Commands.Promote;

public interface IPromotePackageLogger : IPackageRequestResolverLogger, IPackageMirroringExecutorLogger
{
    void LogPackagePresentInDestination(PackageIdentity identity);

    void LogNoPackagesToPromote();

    void LogResolvingPackagesToPromote(IReadOnlyCollection<PackageIdentity> identities);

    void LogProcessingPackage(PackageIdentity identity);

    void LogProcessingDependency(string packageId, VersionRange versionRange);

    void LogNewDependencyToProcess(string packageId, VersionRange versionRange);

    void LogNewDependencyFound(PackageIdentity identity);

    void LogPackagesToPromote(IReadOnlyCollection<PackageIdentity> identities);

    void LogDryRun();
}
