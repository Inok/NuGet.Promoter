using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using Promote.NuGet.Feeds;
using Promote.NuGet.TestInfrastructure;

namespace Promote.NuGet.Tests.Promote.FromConfiguration;

[TestFixture]
public class PromoteFromConfigurationCommandIntegrationTests
{
    private static readonly NuGetRepositoryDescriptor _nugetOrgRepositoryDescriptor = new("https://api.nuget.org/v3/index.json", apiKey: null);

    [Test, CancelAfter(60_000)]
    public async Task Promotes_a_set_of_packages_with_their_dependencies_to_destination_feed()
    {
        using var packagesFile = await TempFile.Create(
                                     "packages:",
                                     "  - id: System.Collections",
                                     "    versions: 4.3.0",
                                     "  - id: System.Globalization",
                                     "    versions:",
                                     "      - 4.0.11",
                                     "      - 4.3.0",
                                     "  - id: System.Runtime",
                                     "    versions:",
                                     "      - '[4.1.0,4.1.2)'",
                                     "      - 4.3.1"
                                 );

        await using var destinationFeed = await LocalNugetFeed.Create();

        var destinationFeedDescriptor = new NuGetRepositoryDescriptor(destinationFeed.FeedUrl, destinationFeed.ApiKey);
        var destinationRepo = new NuGetRepository(destinationFeedDescriptor, new SourceCacheContext { NoCache = true }, TestNuGetLogger.Instance);

        await PromotePackageToFeed(destinationRepo, new PackageIdentity("Microsoft.NETCore.Platforms", new NuGetVersion(1, 1, 1)));
        await PromotePackageToFeed(destinationRepo, new PackageIdentity("Microsoft.NETCore.Targets", new NuGetVersion(1, 1, 3)));
        await PromotePackageToFeed(destinationRepo, new PackageIdentity("System.Runtime", new NuGetVersion(4, 3, 1)));

        // Act
        var result = await PromoteNugetProcessRunner.RunForResultAsync(
                         "promote",
                         "from-config",
                         packagesFile.Path,
                         "--destination", destinationFeed.FeedUrl,
                         "--destination-api-key", destinationFeed.ApiKey
                     );

        // Assert
        result.StdOutput.Should().StartWith(
            new[]
            {
                "Resolving package requests...",
                "Resolving System.Collections 4.3.0",
                "Found 1 matching package(s):",
                "└── 4.3.0",
                "Resolving System.Globalization 4.0.11, 4.3.0",
                "Found 2 matching package(s):",
                "├── 4.0.11",
                "└── 4.3.0",
                "Resolving System.Runtime (>= 4.1.0 && < 4.1.2), 4.3.1",
                "Found 3 matching package(s):",
                "├── 4.1.0",
                "├── 4.1.1",
                "└── 4.3.1",
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
            "├── System.Globalization 4.0.11",
            "│   ├── Microsoft.NETCore.Platforms 1.0.1",
            "│   ├── Microsoft.NETCore.Targets 1.0.1",
            "│   └── System.Runtime 4.1.0 [root]",
            "│       └── + 2 direct dependencies (expanded below)",
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
            "└── System.Runtime 4.3.1 [exists]"
        );

        result.StdOutput.Should().ContainInConsecutiveOrder(
            "Found 12 package(s) to promote:",
            "├── Microsoft.NETCore.Platforms 1.0.1",
            "├── Microsoft.NETCore.Platforms 1.0.2",
            "├── Microsoft.NETCore.Platforms 1.1.0",
            "├── Microsoft.NETCore.Targets 1.0.1",
            "├── Microsoft.NETCore.Targets 1.0.6",
            "├── Microsoft.NETCore.Targets 1.1.0",
            "├── System.Collections 4.3.0",
            "├── System.Globalization 4.0.11",
            "├── System.Globalization 4.3.0",
            "├── System.Runtime 4.1.0",
            "├── System.Runtime 4.1.1",
            "└── System.Runtime 4.3.0"
        );

        result.StdOutput.Should().ContainInOrder(
            "Promoting 12 package(s)...",
            "(1/12) Promote Microsoft.NETCore.Platforms 1.0.1",
            "(2/12) Promote Microsoft.NETCore.Platforms 1.0.2",
            "(3/12) Promote Microsoft.NETCore.Platforms 1.1.0",
            "(4/12) Promote Microsoft.NETCore.Targets 1.0.1",
            "(5/12) Promote Microsoft.NETCore.Targets 1.0.6",
            "(6/12) Promote Microsoft.NETCore.Targets 1.1.0",
            "(7/12) Promote System.Collections 4.3.0",
            "(8/12) Promote System.Globalization 4.0.11",
            "(9/12) Promote System.Globalization 4.3.0",
            "(10/12) Promote System.Runtime 4.1.0",
            "(11/12) Promote System.Runtime 4.1.1",
            "(12/12) Promote System.Runtime 4.3.0",
            "12 package(s) promoted."
        );

        result.StdError.Should().BeEmpty();
        result.ExitCode.Should().Be(0);

        // Recreate destination repo to reset cache
        destinationRepo = new NuGetRepository(destinationFeedDescriptor, new SourceCacheContext { NoCache = true }, TestNuGetLogger.Instance);

        await AssertContainsVersions(
            destinationRepo,
            "System.Runtime",
            new NuGetVersion(4, 1, 0), new NuGetVersion(4, 1, 1), new NuGetVersion(4, 3, 0), new NuGetVersion(4, 3, 1)
        );
        await AssertContainsVersions(
            destinationRepo,
            "System.Globalization",
            new NuGetVersion(4, 0, 11), new NuGetVersion(4, 3, 0)
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

    private static async Task PromotePackageToFeed(INuGetRepository destinationRepo, PackageIdentity packageId)
    {
        var sourceRepo = new NuGetRepository(_nugetOrgRepositoryDescriptor, NullSourceCacheContext.Instance, TestNuGetLogger.Instance);

        var tempNupkg = TempFile.Create();
        await using (var stream = tempNupkg.OpenStream())
        {
            await sourceRepo.Packages.CopyNupkgToStream(packageId, stream);
        }

        await destinationRepo.Packages.PushPackage(tempNupkg.Path, false);
    }

    private static async Task AssertContainsVersions(INuGetRepository repo, string packageId, params NuGetVersion[] expectedVersions)
    {
        var packages = await repo.Packages.GetAllVersions(packageId);
        packages.IsSuccess.Should().BeTrue();
        packages.Value.Should().BeEquivalentTo(expectedVersions);
    }
}
