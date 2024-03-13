using FluentValidation;

namespace Promote.NuGet.Promote.FromConfiguration;

public class PromoteConfiguration
{
    public PackageConfiguration[] Packages { get; init; } = default!;
}

public class PackagesConfigurationValidator : AbstractValidator<PromoteConfiguration>
{
    public PackagesConfigurationValidator()
    {
        RuleFor(x => x.Packages).NotEmpty().ForEach(x => x.NotNull().SetValidator(new PackageConfigurationValidator()));
    }
}
