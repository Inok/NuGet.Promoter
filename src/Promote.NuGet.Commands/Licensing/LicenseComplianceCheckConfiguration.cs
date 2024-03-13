namespace Promote.NuGet.Commands.Licensing;

public class LicenseComplianceSettings
{
    public bool Enabled { get; init; }

    public IReadOnlyCollection<string> AcceptExpressions { get; init; } = default!;

    public IReadOnlyCollection<string> AcceptUrls { get; init; } = default!;

    public IReadOnlyCollection<string> AcceptFiles { get; init; } = default!;

    public static LicenseComplianceSettings Disabled { get; } = new()
                                                                {
                                                                    Enabled = false,
                                                                    AcceptExpressions = [],
                                                                    AcceptUrls = [],
                                                                    AcceptFiles = [],
                                                                };
}
