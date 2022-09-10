using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace Promote.NuGet.Feeds;

public class NuGetRepository : INuGetRepository
{
    public INuGetPackageInfoAccessor Packages { get; }

    public NuGetRepository(NuGetRepositoryDescriptor descriptor, SourceCacheContext cacheContext, ILogger logger)
    {
        if (descriptor == null) throw new ArgumentNullException(nameof(descriptor));
        if (cacheContext == null) throw new ArgumentNullException(nameof(cacheContext));
        if (logger == null) throw new ArgumentNullException(nameof(logger));

        var sourceRepository = Repository.Factory.GetCoreV3(descriptor.Source);
        Packages = new NuGetPackageInfoAccessor(descriptor, sourceRepository, cacheContext, logger);
    }
}