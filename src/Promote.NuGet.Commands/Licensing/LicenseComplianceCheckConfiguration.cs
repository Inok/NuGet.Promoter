using NuGet.Packaging.Core;

namespace Promote.NuGet.Commands.Licensing;

public class LicenseComplianceSettings
{
    public bool Enabled { get; init; }

    public required IReadOnlyCollection<string> AcceptExpressions { get; init; }

    public required IReadOnlyCollection<string> AcceptUrls { get; init; }

    public required IReadOnlyCollection<string> AcceptFiles { get; init; }

    public required IReadOnlyCollection<string> AcceptNoLicense { get; init; }

    public static LicenseComplianceSettings Disabled { get; } = new()
                                                                {
                                                                    Enabled = false,
                                                                    AcceptExpressions = [],
                                                                    AcceptUrls = [],
                                                                    AcceptFiles = [],
                                                                    AcceptNoLicense = [],
                                                                };
}
