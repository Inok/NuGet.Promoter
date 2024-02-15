namespace Promote.NuGet.Commands.Requests;

public interface IPackageRequest
{
    string Id { get; }

    public Task<T> Accept<T>(IPackageRequestVisitor<T> visitor, CancellationToken cancellationToken);

    string ToString();
}
