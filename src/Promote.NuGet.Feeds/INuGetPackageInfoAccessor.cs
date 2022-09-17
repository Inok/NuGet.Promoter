using System.IO;
using CSharpFunctionalExtensions;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Promote.NuGet.Feeds;

public interface INuGetPackageInfoAccessor
{
    Task<Result<IReadOnlyCollection<NuGetVersion>>> GetAllVersions(string packageId, CancellationToken cancellationToken = default);

    Task<Result<IPackageSearchMetadata>> GetPackageMetadata(PackageIdentity identity, CancellationToken cancellationToken = default);

    Task<Result> CopyNupkgToStream(PackageIdentity identity, Stream stream, CancellationToken cancellationToken = default);

    Task<Result> PushPackage(string filePath, bool skipDuplicate, CancellationToken cancellationToken = default);

    Task<Result<bool>> DoesPackageExist(PackageIdentity identity, CancellationToken cancellationToken = default);
}