namespace Promote.NuGet.Commands.Requests;

public interface IPackageVersionPolicyVisitor<T>
{
    Task<T> Visit(ExactPackageVersionPolicy versionPolicy, CancellationToken cancellationToken = default);
    Task<T> Visit(VersionRangePackageVersionPolicy versionPolicy, CancellationToken cancellationToken = default);
    Task<T> Visit(LatestPackageVersionPolicy versionPolicy, CancellationToken cancellationToken = default);
}
