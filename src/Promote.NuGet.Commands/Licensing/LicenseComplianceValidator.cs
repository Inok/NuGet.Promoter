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

    public Result CheckCompliance(IReadOnlyCollection<PackageInfo> packages, LicenseComplianceSettings settings)
    {
        _logger.LogLicenseSummary(packages);

        if (!settings.Enabled)
        {
            _logger.LogComplianceChecksDisabled();
            return Result.Success();
        }

        return Result.Failure("NOT IMPLEMENTED!");
    }
}
