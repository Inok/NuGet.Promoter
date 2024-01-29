using FluentValidation;
using NuGet.Versioning;

namespace Promote.NuGet.Promote.FromConfiguration;

public class PackageConfiguration
{
    public string Id { get; init; } = default!;

    public VersionRange[] Versions { get; init; } = default!;
}

public class PackageConfigurationValidator : AbstractValidator<PackageConfiguration>
{
    public PackageConfigurationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Versions).NotEmpty();
    }
}
