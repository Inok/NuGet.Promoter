using System.IO;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using Promote.NuGet.TestInfrastructure;

namespace Promote.NuGet.Feeds.Tests;

[TestFixture, Parallelizable(ParallelScope.All)]
public class NuGetRepositoryTests
{
    private static readonly NuGetRepositoryDescriptor _nugetOrgRepositoryDescriptor = new("https://api.nuget.org/v3/index.json", apiKey: null);

    [Test]
    public async Task GetAllVersions_returns_all_versions_of_a_package()
    {
        await using var feed = await LocalNugetFeed.Create();

        using var sourceRepo = new NuGetRepository(_nugetOrgRepositoryDescriptor, NullSourceCacheContext.Instance, TestNuGetLogger.Instance);

        var packageMetadataResult = await sourceRepo.Packages.GetAllVersions("System.Text.Json");

        packageMetadataResult.IsSuccess.Should().BeTrue();

        var actualVersions = packageMetadataResult.Value;

        // New packages appear in the feed, so we can't check equivalency here.
        actualVersions.Should().HaveCountGreaterThanOrEqualTo(113);
        actualVersions.Should().Contain(new NuGetVersion[]
                                        {
                                            new(8, 0, 0),
                                            new(8, 0, 0, "rc.2.23479.6"),
                                            new(8, 0, 0, "rc.1.23419.4"),
                                            new(8, 0, 0, "preview.7.23375.6"),
                                            new(7, 0, 4),
                                            new(7, 0, 3),
                                            new(7, 0, 2),
                                            new(7, 0, 1),
                                            new(7, 0, 0),
                                            new(5, 0, 0), // Marked as deprecated
                                            new(2, 0, 0, 11), // Unlisted, with revision
                                            new(1, 0, 0), // Unlisted
                                        });
    }

    [Test]
    public async Task GetPackageMetadata_returns_package_metadata()
    {
        await using var feed = await LocalNugetFeed.Create();

        using var sourceRepo = new NuGetRepository(_nugetOrgRepositoryDescriptor, NullSourceCacheContext.Instance, TestNuGetLogger.Instance);

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
    public async Task GetPackageMetadata_returns_package_metadata_with_proper_IsListed_value_for_unlisted_packages()
    {
        await using var feed = await LocalNugetFeed.Create();

        using var sourceRepo = new NuGetRepository(_nugetOrgRepositoryDescriptor, NullSourceCacheContext.Instance, TestNuGetLogger.Instance);

        var packageIdentity = new PackageIdentity("System.Text.Json", new NuGetVersion(1, 0, 0));

        var packageMetadataResult = await sourceRepo.Packages.GetPackageMetadata(packageIdentity);

        packageMetadataResult.IsSuccess.Should().BeTrue();

        var metadata = packageMetadataResult.Value;
        metadata.Identity.Should().Be(packageIdentity);
        metadata.IsListed.Should().BeFalse();
    }

    [Test]
    public async Task GetPackageMetadata_returns_failure_if_package_does_not_exist()
    {
        await using var feed = await LocalNugetFeed.Create();

        using var sourceRepo = new NuGetRepository(_nugetOrgRepositoryDescriptor, NullSourceCacheContext.Instance, TestNuGetLogger.Instance);

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

        using var sourceRepo = new NuGetRepository(_nugetOrgRepositoryDescriptor, NullSourceCacheContext.Instance, TestNuGetLogger.Instance);

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

        using var sourceRepo = new NuGetRepository(_nugetOrgRepositoryDescriptor, NullSourceCacheContext.Instance, TestNuGetLogger.Instance);
        using var destinationRepo = new NuGetRepository(destinationFeedDescriptor, NullSourceCacheContext.Instance, TestNuGetLogger.Instance);

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
