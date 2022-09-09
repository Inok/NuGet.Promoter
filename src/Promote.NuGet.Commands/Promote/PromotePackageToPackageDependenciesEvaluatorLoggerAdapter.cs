﻿using NuGet.Packaging.Core;
using Promote.NuGet.Commands.Core;

namespace Promote.NuGet.Commands.Promote;

internal class PromotePackageToPackageDependenciesEvaluatorLoggerAdapter : IPackageDependenciesEvaluatorLogger
{
    private readonly IPromotePackageLogger _logger;

    public PromotePackageToPackageDependenciesEvaluatorLoggerAdapter(IPromotePackageLogger logger)
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