using FluentValidation;

namespace Promote.NuGet.Promote.FromConfiguration;

public class PromoteConfiguration
{
    public int? MinimumReleaseAge { get; init; }

    public LicenseComplianceCheckConfiguration? LicenseComplianceCheck { get; init; }

    public PackageConfiguration[] Packages { get; init; } = default!;
}

public class PackagesConfigurationValidator : AbstractValidator<PromoteConfiguration>
{
    public PackagesConfigurationValidator()
    {
        RuleFor(x => x.MinimumReleaseAge).GreaterThan(0).When(x => x.MinimumReleaseAge.HasValue);
        RuleFor(x => x.LicenseComplianceCheck).SetValidator(new LicenseComplianceCheckConfigurationValidator()!);
        RuleFor(x => x.Packages).NotEmpty().ForEach(x => x.NotNull().SetValidator(new PackageConfigurationValidator()));
    }
}
