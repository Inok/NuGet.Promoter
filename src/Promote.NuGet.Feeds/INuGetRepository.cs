namespace Promote.NuGet.Feeds;

public interface INuGetRepository : IDisposable
{
    INuGetPackageInfoAccessor Packages { get; }
}
