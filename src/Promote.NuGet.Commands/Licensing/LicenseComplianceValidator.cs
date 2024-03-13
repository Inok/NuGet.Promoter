using CSharpFunctionalExtensions;
using Promote.NuGet.Commands.Promote.Resolution;
using Promote.NuGet.Feeds;

namespace Promote.NuGet.Commands.Licensing;

public class LicenseComplianceValidator
{
    private readonly INuGetRepository _repository;
    private readonly ILicenseComplianceValidatorLogger _logger;

    public LicenseComplianceValidator(INuGetRepository repository, ILicenseComplianceValidatorLogger logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public Result CheckCompliance(IReadOnlyCollection<PackageInfo> packages)
    {
        _logger.LogLicenseSummary(packages);

        return Result.Success();
    }
}
