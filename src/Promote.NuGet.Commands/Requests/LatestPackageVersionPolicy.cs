namespace Promote.NuGet.Commands.Requests;

public sealed class LatestPackageVersionPolicy : IPackageVersionPolicy
{
    public Task<T> Accept<T>(IPackageVersionPolicyVisitor<T> visitor, CancellationToken cancellationToken)
    {
        return visitor.Visit(this, cancellationToken);
    }

    public override string ToString()
    {
        return "@latest";
    }
}
