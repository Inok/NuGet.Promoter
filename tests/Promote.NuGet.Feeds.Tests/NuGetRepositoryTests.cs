using System.IO;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Promote.NuGet.Feeds.Tests;

[TestFixture]
public class NuGetRepositoryTests
{
    [Test]
    public async Task Downloads_a_package_from_source_feed_and_pushes_it_to_destination_feed()
    {
        await using var feed = await LocalNugetFeed.Create();

        var nugetOrgDescriptor = new NuGetRepositoryDescriptor("https://api.nuget.org/v3/index.json", apiKey: null);
        var destinationFeedDescriptor = new NuGetRepositoryDescriptor(feed.FeedUrl, feed.ApiKey);

        var sourceRepo = new NuGetRepository(nugetOrgDescriptor, new NullSourceCacheContext(), TestConsoleLogger.Instance);
        var destinationRepo = new NuGetRepository(destinationFeedDescriptor, new NullSourceCacheContext(), TestConsoleLogger.Instance);

        var packageIdentity = new PackageIdentity("System.Text.Json", new NuGetVersion(8, 0, 0));

        var path = Path.GetTempFileName();
        try
        {
            await using (var stream = File.OpenWrite(path))
            {
                var copyResult = await sourceRepo.Packages.CopyNupkgToStream(packageIdentity, stream);
                copyResult.IsSuccess.Should().BeTrue();
                await stream.FlushAsync();
            }

            var pushResult = await destinationRepo.Packages.PushPackage(path, false);
            pushResult.IsSuccess.Should().BeTrue();
        }
        finally
        {
            File.Delete(path);
        }

        var packageMetadata = await destinationRepo.Packages.GetPackageMetadata(packageIdentity);

        packageMetadata.IsSuccess.Should().BeTrue();
        packageMetadata.Value.Identity.Should().Be(packageIdentity);
        packageMetadata.Value.Title.Should().Be("System.Text.Json");
        packageMetadata.Value.Authors.Should().Be("Microsoft");
    }
}
