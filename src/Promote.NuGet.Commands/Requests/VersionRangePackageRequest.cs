using NuGet.Versioning;

namespace Promote.NuGet.Commands.Requests;

public class VersionRangePackageRequest : IPackageRequest
{
    public string Id { get; }

    public IReadOnlyCollection<VersionRange> Versions { get; }

    public VersionRangePackageRequest(string id, VersionRange version)
        : this(id, new[] { version })
    {
    }

    public VersionRangePackageRequest(string id, IReadOnlyCollection<VersionRange> versions)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentException("Value cannot be null or empty.", nameof(id));
        if (versions.Count == 0) throw new ArgumentException("Value cannot be an empty collection.", nameof(versions));

        Id = id;
        Versions = versions;
    }

    public Task<T> Accept<T>(IPackageRequestVisitor<T> visitor, CancellationToken cancellationToken)
    {
        return visitor.Visit(this, cancellationToken);
    }

    public override string ToString()
    {
        return $"{Id} {string.Join(", ", Versions.Select(r => r.PrettyPrint()))}";
    }
}
