﻿using CSharpFunctionalExtensions;
using NuGet.Packaging.Core;
using Promote.NuGet.Feeds;

namespace Promote.NuGet.Commands.Requests.Resolution;

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
        IReadOnlyCollection<PackageRequest> requests,
        CancellationToken cancellationToken)
    {
        _logger.LogResolvingPackageRequests();

        var identities = new HashSet<PackageIdentity>();
        var requestIdentities = new HashSet<PackageIdentity>();

        foreach (var request in requests)
        {
            requestIdentities.Clear();

            _logger.LogResolvingPackageRequest(request);

            var visitor = new ResolvePackageVersionPolicyVisitor(request.Id, _repository);

            foreach (var versionRequest in request.VersionPolicies)
            {
                var result = await versionRequest.Accept(visitor, cancellationToken);
                if (result.IsFailure)
                {
                    return result.ConvertFailure<IReadOnlySet<PackageIdentity>>();
                }

                var matchingPackages = result.Value;

                requestIdentities.UnionWith(matchingPackages);
            }

            _logger.LogPackageRequestResolution(request, requestIdentities);

            identities.UnionWith(requestIdentities);
        }

        return identities;
    }
}
