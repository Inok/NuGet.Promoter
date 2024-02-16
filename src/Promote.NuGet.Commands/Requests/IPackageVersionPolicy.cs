namespace Promote.NuGet.Commands.Requests;

public interface IPackageVersionPolicy
{
    public Task<T> Accept<T>(IPackageVersionPolicyVisitor<T> visitor, CancellationToken cancellationToken);
}
