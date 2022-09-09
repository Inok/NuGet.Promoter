using NuGet.Packaging.Core;

namespace Promote.NuGet.Commands.Core;

public interface IPackageDependenciesEvaluatorLogger
{
    void LogProcessingDependenciesOfPackage(PackageIdentity identity);

    void LogNewDependencyFound(PackageIdentity identity);
}