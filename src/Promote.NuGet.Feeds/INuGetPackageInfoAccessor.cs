using System.IO;
using CSharpFunctionalExtensions;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Promote.NuGet.Feeds;

public interface INuGetPackageInfoAccessor
{
    Task<Result<IReadOnlyCollection<NuGetVersion>, string>> GetAllVersions(string packageId, CancellationToken cancellationToken = default);

    Task<Result<IPackageSearchMetadata, string>> GetPackageMetadata(PackageIdentity identity, CancellationToken cancellationToken = default);

    Task<UnitResult<string>> CopyNupkgToStream(PackageIdentity identity, Stream stream, CancellationToken cancellationToken = default);

    Task<UnitResult<string>> PushPackage(string filePath, bool skipDuplicate, CancellationToken cancellationToken = default);
}