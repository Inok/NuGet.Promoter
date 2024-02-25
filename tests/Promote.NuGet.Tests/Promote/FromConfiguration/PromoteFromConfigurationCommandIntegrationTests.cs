using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using Promote.NuGet.Feeds;
using Promote.NuGet.TestInfrastructure;

namespace Promote.NuGet.Tests.Promote.FromConfiguration;

[TestFixture]
public class PromoteFromConfigurationCommandIntegrationTests
{
    [Test, CancelAfter(60_000)]
    public async Task Promotes_a_set_of_packages_with_their_dependencies_to_destination_feed()
    {
        using var packagesFile = await TempFile.Create(
                                     "packages:",
                                     "  - id: System.Collections",
                                     "    versions: 4.3.0",
                                     "  - id: System.Globalization",
                                     "    versions: 4.3.0",
                                     "  - id: System.Runtime",
                                     "    versions:",
                                     "      - '[4.1.0,4.1.2)'",
                                     "      - 4.3.1"
                                 );

        await using var destinationFeed = await LocalNugetFeed.Create();

        // Act
        var result = await PromoteNugetProcessRunner.RunForResultAsync(
                         "promote",
                         "from-config",
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
                "Resolving matching packages for:",
                "├── System.Collections 4.3.0",
                "├── System.Globalization 4.3.0",
                "└── System.Runtime (>= 4.1.0 && < 4.1.2), 4.3.1",
                "Matching packages for System.Collections 4.3.0: 4.3.0",
                "Matching packages for System.Globalization 4.3.0: 4.3.0",
                "Matching packages for System.Runtime (>= 4.1.0 && < 4.1.2), ",
                "4.3.1: 4.1.0, 4.1.1, 4.3.1",
                "Resolving packages to promote:",
                "├── System.Collections 4.3.0",
                "├── System.Globalization 4.3.0",
                "├── System.Runtime 4.1.0",
                "├── System.Runtime 4.1.1",
                "└── System.Runtime 4.3.1"
            }
        );

        result.StdOutput.Should().ContainInConsecutiveOrder(
            "Resolved package tree:",
            "├── System.Collections 4.3.0",
            "│   ├── Microsoft.NETCore.Platforms 1.1.0",
            "│   ├── Microsoft.NETCore.Targets 1.1.0",
            "│   └── System.Runtime 4.3.0",
            "│       ├── Microsoft.NETCore.Platforms 1.1.0",
            "│       └── Microsoft.NETCore.Targets 1.1.0",
            "├── System.Globalization 4.3.0",
            "│   ├── Microsoft.NETCore.Platforms 1.1.0",
            "│   ├── Microsoft.NETCore.Targets 1.1.0",
            "│   └── System.Runtime 4.3.0",
            "│       └── + 2 direct dependencies (expanded above)",
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
            "Found 14 package(s) to promote:",
            "├── Microsoft.NETCore.Platforms 1.0.1",
            "├── Microsoft.NETCore.Platforms 1.0.2",
            "├── Microsoft.NETCore.Platforms 1.1.0",
            "├── Microsoft.NETCore.Platforms 1.1.1",
            "├── Microsoft.NETCore.Targets 1.0.1",
            "├── Microsoft.NETCore.Targets 1.0.6",
            "├── Microsoft.NETCore.Targets 1.1.0",
            "├── Microsoft.NETCore.Targets 1.1.3",
            "├── System.Collections 4.3.0",
            "├── System.Globalization 4.3.0",
            "├── System.Runtime 4.1.0",
            "├── System.Runtime 4.1.1",
            "├── System.Runtime 4.3.0",
            "└── System.Runtime 4.3.1"
        );

        result.StdOutput.Should().ContainInOrder(
            "Promoting 14 package(s)...",
            "(1/14) Promote Microsoft.NETCore.Platforms 1.0.1",
            "(2/14) Promote Microsoft.NETCore.Platforms 1.0.2",
            "(3/14) Promote Microsoft.NETCore.Platforms 1.1.0",
            "(4/14) Promote Microsoft.NETCore.Platforms 1.1.1",
            "(5/14) Promote Microsoft.NETCore.Targets 1.0.1",
            "(6/14) Promote Microsoft.NETCore.Targets 1.0.6",
            "(7/14) Promote Microsoft.NETCore.Targets 1.1.0",
            "(8/14) Promote Microsoft.NETCore.Targets 1.1.3",
            "(9/14) Promote System.Collections 4.3.0",
            "(10/14) Promote System.Globalization 4.3.0",
            "(11/14) Promote System.Runtime 4.1.0",
            "(12/14) Promote System.Runtime 4.1.1",
            "(13/14) Promote System.Runtime 4.3.0",
            "(14/14) Promote System.Runtime 4.3.1",
            "14 package(s) promoted."
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
            "System.Collections",
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
