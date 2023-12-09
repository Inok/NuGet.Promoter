using System.IO;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Promote.NuGet.Feeds.Tests;

[TestFixture]
public class NuGetRepositoryTests
{
    private static readonly NuGetRepositoryDescriptor _nugetOrgRepositoryDescriptor = new("https://api.nuget.org/v3/index.json", apiKey: null);

    [Test]
    public async Task GetPackageMetadata_returns_package_metadata()
    {
        await using var feed = await LocalNugetFeed.Create();

        var sourceRepo = new NuGetRepository(_nugetOrgRepositoryDescriptor, new NullSourceCacheContext(), TestConsoleLogger.Instance);

        var packageIdentity = new PackageIdentity("Newtonsoft.Json", new NuGetVersion(13, 0, 3));

        var packageMetadataResult = await sourceRepo.Packages.GetPackageMetadata(packageIdentity);

        packageMetadataResult.IsSuccess.Should().BeTrue();

        var metadata = packageMetadataResult.Value;
        metadata.Identity.Should().Be(packageIdentity);
        metadata.Title.Should().Be("Json.NET");
        metadata.Authors.Should().Be("James Newton-King");

        metadata.DependencySets.Should()
                .BeEquivalentTo(new[]
                                {
                                    new PackageDependencyGroup(FrameworkConstants.CommonFrameworks.Net2, Array.Empty<PackageDependency>()),
                                    new PackageDependencyGroup(FrameworkConstants.CommonFrameworks.Net35, Array.Empty<PackageDependency>()),
                                    new PackageDependencyGroup(FrameworkConstants.CommonFrameworks.Net4, Array.Empty<PackageDependency>()),
                                    new PackageDependencyGroup(FrameworkConstants.CommonFrameworks.Net45, Array.Empty<PackageDependency>()),
                                    new PackageDependencyGroup(
                                        FrameworkConstants.CommonFrameworks.NetStandard10,
                                        new[]
                                        {
                                            new PackageDependency("Microsoft.CSharp", VersionRange.Parse("[4.3.0,)")),
                                            new PackageDependency("NETStandard.Library", VersionRange.Parse("[1.6.1,)")),
                                            new PackageDependency("System.ComponentModel.TypeConverter", VersionRange.Parse("[4.3.0,)")),
                                            new PackageDependency("System.Runtime.Serialization.Primitives", VersionRange.Parse("[4.3.0,)")),
                                        }
                                    ),
                                    new PackageDependencyGroup(
                                        FrameworkConstants.CommonFrameworks.NetStandard13,
                                        new[]
                                        {
                                            new PackageDependency("Microsoft.CSharp", VersionRange.Parse("[4.3.0,)")),
                                            new PackageDependency("NETStandard.Library", VersionRange.Parse("[1.6.1,)")),
                                            new PackageDependency("System.ComponentModel.TypeConverter", VersionRange.Parse("[4.3.0,)")),
                                            new PackageDependency("System.Runtime.Serialization.Formatters", VersionRange.Parse("[4.3.0,)")),
                                            new PackageDependency("System.Runtime.Serialization.Primitives", VersionRange.Parse("[4.3.0,)")),
                                            new PackageDependency("System.Xml.XmlDocument", VersionRange.Parse("[4.3.0,)")),
                                        }
                                    ),
                                    new PackageDependencyGroup(FrameworkConstants.CommonFrameworks.NetStandard20, Array.Empty<PackageDependency>()),
                                    new PackageDependencyGroup(FrameworkConstants.CommonFrameworks.Net60, Array.Empty<PackageDependency>()),
                                });
    }

    [Test]
    public async Task GetPackageMetadata_returns_failure_if_package_does_not_exist()
    {
        await using var feed = await LocalNugetFeed.Create();

        var sourceRepo = new NuGetRepository(_nugetOrgRepositoryDescriptor, new NullSourceCacheContext(), TestConsoleLogger.Instance);

        var packageIdentity = new PackageIdentity("System.Not.Existing.Package.Name", new NuGetVersion(1, 2, 3));

        var packageMetadataResult = await sourceRepo.Packages.GetPackageMetadata(packageIdentity);

        packageMetadataResult.IsFailure.Should().BeTrue();
    }

    [Test]
    [TestCase("System.Text.Json", "8.0.0", true)]
    [TestCase("System.Text.Json", "7.9.9", false)]
    [TestCase("System.Not.Existing.Package.Name", "1.0.0", false)]
    public async Task DoesPackageExist_returns_expected_package_status(string packageId, string packageVersion, bool expectedResult)
    {
        await using var feed = await LocalNugetFeed.Create();

        var sourceRepo = new NuGetRepository(_nugetOrgRepositoryDescriptor, new NullSourceCacheContext(), TestConsoleLogger.Instance);

        var packageIdentity = new PackageIdentity(packageId, NuGetVersion.Parse(packageVersion));

        var doesPackageExistResult = await sourceRepo.Packages.DoesPackageExist(packageIdentity);

        doesPackageExistResult.IsSuccess.Should().BeTrue();
        doesPackageExistResult.Value.Should().Be(expectedResult);
    }

    [Test]
    public async Task Download_a_package_from_source_feed_and_push_it_to_destination_feed()
    {
        await using var feed = await LocalNugetFeed.Create();

        var destinationFeedDescriptor = new NuGetRepositoryDescriptor(feed.FeedUrl, feed.ApiKey);

        var sourceRepo = new NuGetRepository(_nugetOrgRepositoryDescriptor, new NullSourceCacheContext(), TestConsoleLogger.Instance);
        var destinationRepo = new NuGetRepository(destinationFeedDescriptor, new NullSourceCacheContext(), TestConsoleLogger.Instance);

        var packageIdentity = new PackageIdentity("System.Text.Json", new NuGetVersion(8, 0, 0));

        var path = Path.GetTempFileName();
        try
        {
            await using (var stream = File.OpenWrite(path))
            {
                var copyResult = await sourceRepo.Packages.CopyNupkgToStream(packageIdentity, stream);
                copyResult.IsSuccess.Should().BeTrue();
            }

            var pushResult = await destinationRepo.Packages.PushPackage(path, false);
            pushResult.IsSuccess.Should().BeTrue();
        }
        finally
        {
            File.Delete(path);
        }

        var packageMetadataResult = await destinationRepo.Packages.GetPackageMetadata(packageIdentity);

        packageMetadataResult.IsSuccess.Should().BeTrue();

        var metadata = packageMetadataResult.Value;
        metadata.Identity.Should().Be(packageIdentity);
        metadata.Title.Should().Be("System.Text.Json");
        metadata.Authors.Should().Be("Microsoft");
    }
}
