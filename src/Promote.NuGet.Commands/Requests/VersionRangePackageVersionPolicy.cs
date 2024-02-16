using NuGet.Versioning;

namespace Promote.NuGet.Commands.Requests;

public class VersionRangePackageVersionPolicy : IPackageVersionPolicy
{
    public VersionRange VersionRange { get; }

    public VersionRangePackageVersionPolicy(VersionRange versionRange)
    {
        VersionRange = versionRange ?? throw new ArgumentNullException(nameof(versionRange));
    }

    public Task<T> Accept<T>(IPackageVersionPolicyVisitor<T> visitor, CancellationToken cancellationToken)
    {
        return visitor.Visit(this, cancellationToken);
    }

    public override string ToString()
    {
        return VersionRange.PrettyPrint();
    }
}
