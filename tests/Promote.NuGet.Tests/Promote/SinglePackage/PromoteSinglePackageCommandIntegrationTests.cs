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

        // Assert
        result.StdOutput.Should().StartWith(
            [
                "Resolving package requests...",
                "Resolving System.Runtime (= 4.3.0)",
                "Found 1 matching version: 4.3.0"
            ]
        );

        result.StdOutput.Should().ContainInConsecutiveOrder(
            "Resolved package tree:",
            "└── System.Runtime 4.3.0",
            "    ├── Microsoft.NETCore.Platforms 1.1.0",
            "    └── Microsoft.NETCore.Targets 1.1.0"
        );

        result.StdOutput.Select(x => x.TrimEnd()).Should().ContainInConsecutiveOrder(
            "Found 3 packages to promote:",
            "├── Microsoft.NETCore.Platforms 1.1.0",
            "│   License: MICROSOFT .NET LIBRARY",
            "│   (http://go.microsoft.com/fwlink/?LinkId=329770)",
            "├── Microsoft.NETCore.Targets 1.1.0",
            "│   License: MICROSOFT .NET LIBRARY",
            "│   (http://go.microsoft.com/fwlink/?LinkId=329770)",
            "└── System.Runtime 4.3.0",
            "    License: MICROSOFT .NET LIBRARY",
            "    (http://go.microsoft.com/fwlink/?LinkId=329770)"
        );

        result.StdOutput.Select(x => x.TrimEnd()).Should().ContainInConsecutiveOrder(
            "License summary:",
            "└── 3x: MICROSOFT .NET LIBRARY",
            "    (http://go.microsoft.com/fwlink/?LinkId=329770)"
        );

        result.StdOutput.Should().ContainInOrder(
            "Promoting 3 packages...",
            "(1/3) Promote Microsoft.NETCore.Platforms 1.1.0",
            "(2/3) Promote Microsoft.NETCore.Targets 1.1.0",
            "(3/3) Promote System.Runtime 4.3.0",
            "3 packages promoted."
        );

        result.StdError.Should().BeEmpty();
        result.ExitCode.Should().Be(0);

        using (var cacheContext = new SourceCacheContext { NoCache = true })
        {
            using var destinationRepo = new NuGetRepository(destinationFeedDescriptor, cacheContext, TestNuGetLogger.Instance);

            await AssertContainsVersions(destinationRepo, "System.Runtime", new[] { new NuGetVersion(4, 3, 0) });
            await AssertContainsVersions(destinationRepo, "Microsoft.NETCore.Platforms", new[] { new NuGetVersion(1, 1, 0) });
            await AssertContainsVersions(destinationRepo, "Microsoft.NETCore.Targets", new[] { new NuGetVersion(1, 1, 0) });
        }
    }

    private static async Task AssertContainsVersions(INuGetRepository repo, string packageId, params NuGetVersion[] expectedVersions)
    {
        var packages = await repo.Packages.GetAllVersions(packageId);
        packages.IsSuccess.Should().BeTrue();
        packages.Value.Should().BeEquivalentTo(expectedVersions);
    }
}
