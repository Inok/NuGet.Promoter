namespace Promote.NuGet.Commands.Requests;

public interface IPackageVersionPolicy
{
    Task<T> Accept<T>(IPackageVersionPolicyVisitor<T> visitor, CancellationToken cancellationToken);
}
