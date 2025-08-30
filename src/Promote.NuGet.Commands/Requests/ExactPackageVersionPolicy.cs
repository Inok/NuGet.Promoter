using NuGet.Versioning;

namespace Promote.NuGet.Commands.Requests;

public sealed class ExactPackageVersionPolicy(NuGetVersion version) : IPackageVersionPolicy
{
    public NuGetVersion Version { get; } = version ?? throw new ArgumentNullException(nameof(version));

    public Task<T> Accept<T>(IPackageVersionPolicyVisitor<T> visitor, CancellationToken cancellationToken)
    {
        return visitor.Visit(this, cancellationToken);
    }

    public override string ToString()
    {
        return new VersionRange(Version, true, Version, true).PrettyPrint();
    }
}
