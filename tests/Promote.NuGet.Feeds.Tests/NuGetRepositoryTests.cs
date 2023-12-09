using System.IO;
using System.Net;
using DotNet.Testcontainers.Builders;
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
        var apiKey = TestContext.CurrentContext.Random.GetString(20);

        await using var container = new ContainerBuilder()
                                    .WithImage("bagetter/bagetter:1.0.0")
                                    .WithEnvironment("ApiKey", apiKey)
                                    .WithPortBinding(8080, true)
                                    .WithWaitStrategy(Wait.ForUnixContainer()
                                                          .UntilHttpRequestIsSucceeded(r => r.ForPort(8080).ForPath("/").ForStatusCode(HttpStatusCode.OK)))
                                    .Build();

        await container.StartAsync();

        var nugetOrgDescriptor = new NuGetRepositoryDescriptor("https://api.nuget.org/v3/index.json", apiKey: null);

        var destinationUri = new UriBuilder("http", container.Hostname, container.GetMappedPublicPort(8080), "/v3/index.json").Uri.ToString();
        var destinationFeedDescriptor = new NuGetRepositoryDescriptor(destinationUri, apiKey);

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
