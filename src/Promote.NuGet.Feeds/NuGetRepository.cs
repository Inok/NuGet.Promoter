using NuGet.Common;
using NuGet.Configuration;
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

        var packageSource = new PackageSource(descriptor.Source);

        if (!string.IsNullOrEmpty(descriptor.Username))
        {
            packageSource.Credentials = new PackageSourceCredential(descriptor.Source, descriptor.Username, descriptor.Password, true, null);
        }

        var sourceRepository = Repository.Factory.GetCoreV3(packageSource);
        Packages = new NuGetPackageInfoAccessor(descriptor, sourceRepository, cacheContext, logger);
    }

    public void Dispose()
    {
        Packages.Dispose();
    }
}
