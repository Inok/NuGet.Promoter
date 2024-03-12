using Promote.NuGet.Commands.Mirroring;
using Promote.NuGet.Commands.Promote.Resolution;
using Promote.NuGet.Commands.Requests.Resolution;

namespace Promote.NuGet.Commands.Promote;

public interface IPromotePackageLogger : IPackageRequestResolverLogger, IPackageMirroringExecutorLogger, IPackagesToPromoteResolverLogger
{
    void LogPackagesToPromote(IReadOnlyCollection<PackageInfo> packages);

    void LogNoPackagesToPromote();

    void LogDryRun();
}
