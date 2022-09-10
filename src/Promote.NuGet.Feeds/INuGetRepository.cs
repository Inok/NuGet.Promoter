namespace Promote.NuGet.Feeds;

public interface INuGetRepository
{
    INuGetPackageInfoAccessor Packages { get; }
}