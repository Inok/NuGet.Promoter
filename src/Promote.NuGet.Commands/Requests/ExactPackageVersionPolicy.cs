using NuGet.Versioning;

namespace Promote.NuGet.Commands.Requests;

public class ExactPackageVersionPolicy : IPackageVersionPolicy
{
    public NuGetVersion Version { get; }

    public ExactPackageVersionPolicy(NuGetVersion version)
    {
        Version = version ?? throw new ArgumentNullException(nameof(version));
    }

    public Task<T> Accept<T>(IPackageVersionPolicyVisitor<T> visitor, CancellationToken cancellationToken)
    {
        return visitor.Visit(this, cancellationToken);
    }

    public override string ToString()
    {
        return Version.ToNormalizedString();
    }
}
