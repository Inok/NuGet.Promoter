using NuGet.Versioning;

namespace Promote.NuGet.Commands.Requests;

public class ExactPackageRequest : IPackageRequest
{
    public string Id { get; }

    public NuGetVersion Version { get; }

    public ExactPackageRequest(string id, NuGetVersion version)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentException("Value cannot be null or empty.", nameof(id));
        if (version == null) throw new ArgumentNullException(nameof(version));

        Id = id;
        Version = version;
    }

    public Task<T> Accept<T>(IPackageRequestVisitor<T> visitor, CancellationToken cancellationToken)
    {
        return visitor.Visit(this, cancellationToken);
    }

    public override string ToString()
    {
        return $"{Id} {Version}";
    }
}
