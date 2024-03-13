using FluentValidation;

namespace Promote.NuGet.Promote.FromConfiguration;

public class LicenseComplianceCheckConfiguration
{
    public bool Enabled { get; init; }

    public string[]? AcceptExpressions { get; init; }

    public string[]? AcceptUrls { get; init; }

    public string[]? AcceptFiles { get; init; }
}

public class LicenseComplianceCheckConfigurationValidator : AbstractValidator<LicenseComplianceCheckConfiguration>
{
    public LicenseComplianceCheckConfigurationValidator()
    {
        RuleFor(x => x.AcceptExpressions).ForEach(x => x.NotEmpty());
        RuleFor(x => x.AcceptUrls).ForEach(x => x.NotEmpty());
        RuleFor(x => x.AcceptFiles).ForEach(x => x.NotEmpty());
    }
}

