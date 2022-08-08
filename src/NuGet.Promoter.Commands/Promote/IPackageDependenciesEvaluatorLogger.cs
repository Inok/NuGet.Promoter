using NuGet.Packaging.Core;

namespace NuGet.Promoter.Commands.Promote;

public interface IPackageDependenciesEvaluatorLogger
{
    void LogProcessingDependenciesOfPackage(PackageIdentity identity);

    void LogNewDependencyFound(PackageIdentity identity);
}