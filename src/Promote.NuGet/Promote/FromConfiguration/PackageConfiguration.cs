using FluentValidation;
using Promote.NuGet.Commands.Requests;

namespace Promote.NuGet.Promote.FromConfiguration;

public class PackageConfiguration
{
    public string Id { get; init; } = default!;

    public IPackageVersionPolicy[] Versions { get; init; } = default!;
}

public class PackageConfigurationValidator : AbstractValidator<PackageConfiguration>
{
    public PackageConfigurationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Versions).NotEmpty().ForEach(x => x.NotNull());
    }
}
