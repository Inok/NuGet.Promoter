using NuGet.Versioning;

namespace Promote.NuGet.Promote.FromConfiguration;

public class PackageConfiguration
{
    public string Id { get; init; } = default!;

    public VersionRange[] Versions { get; init; } = default!;
}
