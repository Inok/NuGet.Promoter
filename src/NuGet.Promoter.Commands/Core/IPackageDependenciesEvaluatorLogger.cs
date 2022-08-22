using NuGet.Packaging.Core;

namespace NuGet.Promoter.Commands.Core;

public interface IPackageDependenciesEvaluatorLogger
{
    void LogProcessingDependenciesOfPackage(PackageIdentity identity);

    void LogNewDependencyFound(PackageIdentity identity);
}