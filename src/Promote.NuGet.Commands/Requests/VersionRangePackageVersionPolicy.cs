using NuGet.Versioning;

namespace Promote.NuGet.Commands.Requests;

public sealed class VersionRangePackageVersionPolicy(VersionRange versionRange) : IPackageVersionPolicy
{
    public VersionRange VersionRange { get; } = versionRange ?? throw new ArgumentNullException(nameof(versionRange));

    public Task<T> Accept<T>(IPackageVersionPolicyVisitor<T> visitor, CancellationToken cancellationToken)
    {
        return visitor.Visit(this, cancellationToken);
    }

    public override string ToString()
    {
        return VersionRange.PrettyPrint();
    }
}
