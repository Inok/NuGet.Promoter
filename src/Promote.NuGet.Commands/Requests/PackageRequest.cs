namespace Promote.NuGet.Commands.Requests;

public class PackageRequest
{
    public string Id { get; }

    public IReadOnlyCollection<IPackageVersionPolicy> VersionRequests { get; }

    public PackageRequest(string id, IPackageVersionPolicy versionPolicy)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentException("Value cannot be null or empty.", nameof(id));
        if (versionPolicy == null) throw new ArgumentNullException(nameof(versionPolicy));

        Id = id;
        VersionRequests = new[] { versionPolicy };
    }

    public PackageRequest(string id, IReadOnlyCollection<IPackageVersionPolicy> versionRequests)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentException("Value cannot be null or empty.", nameof(id));
        if (versionRequests == null) throw new ArgumentNullException(nameof(versionRequests));
        if (versionRequests.Count == 0) throw new ArgumentException("Value cannot be an empty collection.", nameof(versionRequests));

        Id = id;
        VersionRequests = versionRequests;
    }

    public override string ToString()
    {
        return $"{Id} {string.Join(", ", VersionRequests)}";
    }
}
