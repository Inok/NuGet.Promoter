using NuGet.Packaging.Core;

namespace Promote.NuGet.Commands.Mirroring;

public interface IPackageMirroringExecutorLogger
{
    void LogStartMirroringPackagesCount(int count);

    void LogMirrorPackage(PackageIdentity identity, int current, int total);

    void LogMirroredPackagesCount(int count);
}
