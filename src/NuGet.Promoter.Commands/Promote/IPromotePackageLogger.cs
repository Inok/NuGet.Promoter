using NuGet.Packaging.Core;

namespace NuGet.Promoter.Commands.Promote;

public interface IPromotePackageLogger
{
    void LogResolvingMatchingPackages(IReadOnlyCollection<PackageDependency> dependencies);

    void LogResolvingDependencies(IReadOnlyCollection<PackageIdentity> identities);

    void LogProcessingDependenciesOfPackage(PackageIdentity identity);

    void LogNewDependencyFound(PackageIdentity identity);

    void LogPackagesToPromote(IReadOnlyCollection<PackageIdentity> identities);

    void LogPromotePackage(PackageIdentity identity, int current, int total);

    void LogPromotedPackagesCount(int count);
}