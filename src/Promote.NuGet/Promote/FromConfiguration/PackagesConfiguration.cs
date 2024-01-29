using FluentValidation;

namespace Promote.NuGet.Promote.FromConfiguration;

public class PackagesConfiguration
{
    public PackageConfiguration[] Packages { get; init; } = default!;
}

public class PackagesConfigurationValidator : AbstractValidator<PackagesConfiguration>
{
    public PackagesConfigurationValidator()
    {
        RuleFor(x => x.Packages).NotNull().ForEach(x => x.NotNull().SetValidator(new PackageConfigurationValidator()));
    }
}
