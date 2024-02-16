namespace Promote.NuGet.Commands.Requests;

public class PackageRequest
{
    public string Id { get; }

    public IReadOnlyCollection<IPackageVersionPolicy> VersionPolicies { get; }

    public PackageRequest(string id, IPackageVersionPolicy versionPolicy)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentException("Value cannot be null or empty.", nameof(id));
        if (versionPolicy == null) throw new ArgumentNullException(nameof(versionPolicy));

        Id = id;
        VersionPolicies = new[] { versionPolicy };
    }

    public PackageRequest(string id, IReadOnlyCollection<IPackageVersionPolicy> versionPolicies)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentException("Value cannot be null or empty.", nameof(id));
        if (versionPolicies == null) throw new ArgumentNullException(nameof(versionPolicies));
        if (versionPolicies.Count == 0) throw new ArgumentException("Value cannot be an empty collection.", nameof(versionPolicies));

        Id = id;
        VersionPolicies = versionPolicies;
    }

    public override string ToString()
    {
        return $"{Id} {string.Join(", ", VersionPolicies)}";
    }
}
