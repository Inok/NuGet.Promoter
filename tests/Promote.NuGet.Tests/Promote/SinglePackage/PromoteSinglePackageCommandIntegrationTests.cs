using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using Promote.NuGet.Feeds;
using Promote.NuGet.TestInfrastructure;

namespace Promote.NuGet.Tests.Promote.SinglePackage;

[TestFixture]
public class PromoteSinglePackageCommandIntegrationTests
{
    [Test, CancelAfter(60_000)]
    public async Task Promotes_a_package_with_its_dependencies_to_destination_feed()
    {
        await using var destinationFeed = await LocalNugetFeed.Create();

        // Act
        var result = await PromoteNugetProcessRunner.RunForResultAsync(
                         "promote",
                         "package",
                         "System.Runtime",
                         "--version", "4.3.0",
                         "--destination", destinationFeed.FeedUrl,
                         "--destination-api-key", destinationFeed.ApiKey
                     );

        var destinationFeedDescriptor = new NuGetRepositoryDescriptor(destinationFeed.FeedUrl, destinationFeed.ApiKey);
        var destinationRepo = new NuGetRepository(destinationFeedDescriptor, new SourceCacheContext { NoCache = true }, TestNuGetLogger.Instance);

        // Assert
        result.StdOutput.Should().StartWith(
            new[]
            {
                "Resolving package requests...",
                "Resolving System.Runtime 4.3.0",
                "Found 1 matching package(s):",
                "└── 4.3.0",
            }
        );

        result.StdOutput.Should().ContainInConsecutiveOrder(
            "Resolved package tree:",
            "└── System.Runtime 4.3.0",
            "    ├── Microsoft.NETCore.Platforms 1.1.0",
            "    └── Microsoft.NETCore.Targets 1.1.0"
        );

        result.StdOutput.Should().ContainInConsecutiveOrder(
            "Found 3 package(s) to promote:",
            "├── Microsoft.NETCore.Platforms 1.1.0",
            "├── Microsoft.NETCore.Targets 1.1.0",
            "└── System.Runtime 4.3.0"
        );

        result.StdOutput.Should().ContainInOrder(
            "Promoting 3 package(s)...",
            "(1/3) Promote Microsoft.NETCore.Platforms 1.1.0",
            "(2/3) Promote Microsoft.NETCore.Targets 1.1.0",
            "(3/3) Promote System.Runtime 4.3.0",
            "3 package(s) promoted."
        );

        result.StdError.Should().BeEmpty();
        result.ExitCode.Should().Be(0);

        await AssertContainsVersions(destinationRepo, "System.Runtime", new[] { new NuGetVersion(4, 3, 0) });
        await AssertContainsVersions(destinationRepo, "Microsoft.NETCore.Platforms", new[] { new NuGetVersion(1, 1, 0) });
        await AssertContainsVersions(destinationRepo, "Microsoft.NETCore.Targets", new[] { new NuGetVersion(1, 1, 0) });
    }

    private static async Task AssertContainsVersions(INuGetRepository repo, string packageId, params NuGetVersion[] expectedVersions)
    {
        var packages = await repo.Packages.GetAllVersions(packageId);
        packages.IsSuccess.Should().BeTrue();
        packages.Value.Should().BeEquivalentTo(expectedVersions);
    }
}
