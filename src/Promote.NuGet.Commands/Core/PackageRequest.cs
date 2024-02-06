using NuGet.Versioning;

namespace Promote.NuGet.Commands.Core;

public class PackageRequest
{
    public string Id { get; }

    public IReadOnlyCollection<VersionRange> Versions { get; }

    public PackageRequest(string id, VersionRange version)
        : this(id, new[] { version })
    {
    }

    public PackageRequest(string id, IReadOnlyCollection<VersionRange> versions)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentException("Value cannot be null or empty.", nameof(id));
        if (versions.Count == 0) throw new ArgumentException("Value cannot be an empty collection.", nameof(versions));

        Id = id;
        Versions = versions;
    }
}
