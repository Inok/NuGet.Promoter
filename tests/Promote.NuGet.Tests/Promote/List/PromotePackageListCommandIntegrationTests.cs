using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using Promote.NuGet.Feeds;
using Promote.NuGet.TestInfrastructure;

namespace Promote.NuGet.Tests.Promote.List;

[TestFixture]
public class PromotePackageListCommandIntegrationTests
{
    [Test, CancelAfter(60_000)]
    public async Task Promotes_a_set_of_packages_with_their_dependencies_to_destination_feed()
    {
        using var packagesFile = await TempFile.Create(
                                     "System.Runtime [4.1.0,4.1.2)",
                                     "System.Runtime 4.3.1",
                                     "System.Globalization 4.3.0"
                                 );

        await using var destinationFeed = await LocalNugetFeed.Create();

        // Act
        var result = await PromoteNugetProcessRunner.RunForResultAsync(
                         "promote",
                         "from-file",
                         packagesFile.Path,
                         "--destination", destinationFeed.FeedUrl,
                         "--destination-api-key", destinationFeed.ApiKey
                     );

        var destinationFeedDescriptor = new NuGetRepositoryDescriptor(destinationFeed.FeedUrl, destinationFeed.ApiKey);
        var destinationRepo = new NuGetRepository(destinationFeedDescriptor, NullSourceCacheContext.Instance, TestNuGetLogger.Instance);

        // Assert
        result.StdOutput.Should().StartWith(
            new[]
            {
                "Resolving package requests...",
                "Resolving System.Runtime (>= 4.1.0 && < 4.1.2)",
                "Found 2 matching package(s):",
                "├── 4.1.0",
                "└── 4.1.1",
                "Resolving System.Runtime 4.3.1",
                "Found 1 matching package(s):",
                "└── 4.3.1",
                "Resolving System.Globalization 4.3.0",
                "Found 1 matching package(s):",
                "└── 4.3.0",
            }
        );

        result.StdOutput.Should().ContainInConsecutiveOrder(
            "Resolved package tree:",
            "├── System.Globalization 4.3.0",
            "│   ├── Microsoft.NETCore.Platforms 1.1.0",
            "│   ├── Microsoft.NETCore.Targets 1.1.0",
            "│   └── System.Runtime 4.3.0",
            "│       ├── Microsoft.NETCore.Platforms 1.1.0",
            "│       └── Microsoft.NETCore.Targets 1.1.0",
            "├── System.Runtime 4.1.0",
            "│   ├── Microsoft.NETCore.Platforms 1.0.1",
            "│   └── Microsoft.NETCore.Targets 1.0.1",
            "├── System.Runtime 4.1.1",
            "│   ├── Microsoft.NETCore.Platforms 1.0.2",
            "│   └── Microsoft.NETCore.Targets 1.0.6",
            "└── System.Runtime 4.3.1",
            "    ├── Microsoft.NETCore.Platforms 1.1.1",
            "    └── Microsoft.NETCore.Targets 1.1.3"
        );

        result.StdOutput.Should().ContainInConsecutiveOrder(
            "Found 13 package(s) to promote:",
            "├── Microsoft.NETCore.Platforms 1.0.1",
            "├── Microsoft.NETCore.Platforms 1.0.2",
            "├── Microsoft.NETCore.Platforms 1.1.0",
            "├── Microsoft.NETCore.Platforms 1.1.1",
            "├── Microsoft.NETCore.Targets 1.0.1",
            "├── Microsoft.NETCore.Targets 1.0.6",
            "├── Microsoft.NETCore.Targets 1.1.0",
            "├── Microsoft.NETCore.Targets 1.1.3",
            "├── System.Globalization 4.3.0",
            "├── System.Runtime 4.1.0",
            "├── System.Runtime 4.1.1",
            "├── System.Runtime 4.3.0",
            "└── System.Runtime 4.3.1"
        );

        result.StdOutput.Should().ContainInOrder(
            "Promoting 13 package(s)...",
            "(1/13) Promote Microsoft.NETCore.Platforms 1.0.1",
            "(2/13) Promote Microsoft.NETCore.Platforms 1.0.2",
            "(3/13) Promote Microsoft.NETCore.Platforms 1.1.0",
            "(4/13) Promote Microsoft.NETCore.Platforms 1.1.1",
            "(5/13) Promote Microsoft.NETCore.Targets 1.0.1",
            "(6/13) Promote Microsoft.NETCore.Targets 1.0.6",
            "(7/13) Promote Microsoft.NETCore.Targets 1.1.0",
            "(8/13) Promote Microsoft.NETCore.Targets 1.1.3",
            "(9/13) Promote System.Globalization 4.3.0",
            "(10/13) Promote System.Runtime 4.1.0",
            "(11/13) Promote System.Runtime 4.1.1",
            "(12/13) Promote System.Runtime 4.3.0",
            "(13/13) Promote System.Runtime 4.3.1",
            "13 package(s) promoted."
        );

        result.StdError.Should().BeEmpty();
        result.ExitCode.Should().Be(0);

        await AssertContainsVersions(
            destinationRepo,
            "System.Runtime",
            new NuGetVersion(4, 1, 0), new NuGetVersion(4, 1, 1), new NuGetVersion(4, 3, 0), new NuGetVersion(4, 3, 1)
        );
        await AssertContainsVersions(
            destinationRepo,
            "System.Globalization",
            new NuGetVersion(4, 3, 0)
        );
        await AssertContainsVersions(
            destinationRepo,
            "Microsoft.NETCore.Platforms",
            new NuGetVersion(1, 0, 1), new NuGetVersion(1, 0, 2), new NuGetVersion(1, 1, 0), new NuGetVersion(1, 1, 1));
        await AssertContainsVersions(
            destinationRepo,
            "Microsoft.NETCore.Targets",
            new NuGetVersion(1, 0, 1), new NuGetVersion(1, 0, 6), new NuGetVersion(1, 1, 0), new NuGetVersion(1, 1, 3));
    }

    private static async Task AssertContainsVersions(INuGetRepository repo, string packageId, params NuGetVersion[] expectedVersions)
    {
        var packages = await repo.Packages.GetAllVersions(packageId);
        packages.IsSuccess.Should().BeTrue();
        packages.Value.Should().BeEquivalentTo(expectedVersions);
    }
}
