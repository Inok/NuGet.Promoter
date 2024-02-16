using CSharpFunctionalExtensions;
using NuGet.Packaging.Core;
using Promote.NuGet.Commands.Requests;
using Promote.NuGet.Feeds;

namespace Promote.NuGet.Commands.PackageResolution;

public sealed class PackageRequestResolver
{
    private readonly INuGetRepository _repository;
    private readonly IPackageRequestResolverLogger _logger;

    public PackageRequestResolver(INuGetRepository repository, IPackageRequestResolverLogger logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger;
    }

    public async Task<Result<IReadOnlySet<PackageIdentity>>> ResolvePackageRequests(
        IReadOnlyCollection<IPackageRequest> requests,
        CancellationToken cancellationToken)
    {
        _logger.LogResolvingMatchingPackages(requests);

        var visitor = new ResolvePackageRequestVisitor(_repository);

        var identities = new HashSet<PackageIdentity>();
        foreach (var request in requests)
        {
            var result = await request.Accept(visitor, cancellationToken);
            if (result.IsFailure)
            {
                return result.ConvertFailure<IReadOnlySet<PackageIdentity>>();
            }

            var matchingPackages = result.Value;

            _logger.LogPackageRequestResolution(request, matchingPackages);

            identities.UnionWith(matchingPackages);
        }

        return identities;
    }
}
