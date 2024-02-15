namespace Promote.NuGet.Commands.Requests;

public interface IPackageRequestVisitor<T>
{
    Task<T> Visit(ExactPackageRequest request, CancellationToken cancellationToken = default);
    Task<T> Visit(VersionRangePackageRequest request, CancellationToken cancellationToken = default);
    Task<T> Visit(LatestPackageRequest request, CancellationToken cancellationToken = default);
}
