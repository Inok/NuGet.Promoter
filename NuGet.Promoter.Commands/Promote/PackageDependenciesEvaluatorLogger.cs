using NuGet.Packaging.Core;

namespace NuGet.Promoter.Commands.Promote;

internal class PackageDependenciesEvaluatorLogger : IPackageDependenciesEvaluatorLogger
{
    private readonly IPromotePackageLogger _logger;

    public PackageDependenciesEvaluatorLogger(IPromotePackageLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void LogProcessingDependenciesOfPackage(PackageIdentity identity)
    {
        _logger.LogProcessingDependenciesOfPackage(identity);
    }

    public void LogNewDependencyFound(PackageIdentity identity)
    {
        _logger.LogNewDependencyFound(identity);
    }
}