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
                                     """
                                     packages:
                                       - id: System.Collections
                                         versions: 4.3.0
                                       - id: System.Globalization
                                         versions:
                                           - 4.0.11
                                           - 4.3.0
                                       - id: System.Runtime
                                         versions:
                                           - '[4.1.0,4.1.2)'
                                           - 4.3.1
                                       - id: System.Runtime.CompilerServices.Unsafe
                                         versions:
                                           - 6.0.0
                                     """
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
                "Resolving System.Runtime.CompilerServices.Unsafe 6.0.0",
                "Found 1 matching package(s):",
                "└── 6.0.0",
            }
        );

        result.StdOutput.Select(x => x.TrimEnd()).Should().ContainInConsecutiveOrder(
            "Resolving 7 package(s) to promote...",
            "Processing System.Collections 4.3.0",
            "  Package license:",
            "  http://go.microsoft.com/fwlink/?LinkId=329770",
            "  System.Collections 4.3.0 is not in the destination.",
            "  Resolving dependency Microsoft.NETCore.Platforms (>=",
            "  1.1.0)",
            "    Resolved as Microsoft.NETCore.Platforms 1.1.0",
            "    Microsoft.NETCore.Platforms 1.1.0 is queued for",
            "    processing.",
            "  Resolving dependency Microsoft.NETCore.Targets (>= 1.1.0)",
            "    Resolved as Microsoft.NETCore.Targets 1.1.0",
            "    Microsoft.NETCore.Targets 1.1.0 is queued for",
            "    processing.",
            "  Resolving dependency System.Runtime (>= 4.3.0)",
            "    Resolved as System.Runtime 4.3.0",
            "    System.Runtime 4.3.0 is queued for processing.",
            "Processing System.Globalization 4.0.11",
            "  Package license:",
            "  http://go.microsoft.com/fwlink/?LinkId=329770",
            "  System.Globalization 4.0.11 is not in the destination.",
            "  Resolving dependency Microsoft.NETCore.Platforms (>=",
            "  1.0.1)",
            "    Resolved as Microsoft.NETCore.Platforms 1.0.1",
            "    Microsoft.NETCore.Platforms 1.0.1 is queued for",
            "    processing.",
            "  Resolving dependency Microsoft.NETCore.Targets (>= 1.0.1)",
            "    Resolved as Microsoft.NETCore.Targets 1.0.1",
            "    Microsoft.NETCore.Targets 1.0.1 is queued for",
            "    processing.",
            "  Resolving dependency System.Runtime (>= 4.1.0)",
            "    Resolved as System.Runtime 4.1.0",
            "    System.Runtime 4.1.0 is already processed or queued.",
            "Processing System.Globalization 4.3.0",
            "  Package license:",
            "  http://go.microsoft.com/fwlink/?LinkId=329770",
            "  System.Globalization 4.3.0 is not in the destination.",
            "  Resolving dependency Microsoft.NETCore.Platforms (>=",
            "  1.1.0)",
            "    Resolved as Microsoft.NETCore.Platforms 1.1.0",
            "    Microsoft.NETCore.Platforms 1.1.0 is already processed",
            "    or queued.",
            "  Resolving dependency Microsoft.NETCore.Targets (>= 1.1.0)",
            "    Resolved as Microsoft.NETCore.Targets 1.1.0",
            "    Microsoft.NETCore.Targets 1.1.0 is already processed or",
            "    queued.",
            "  Resolving dependency System.Runtime (>= 4.3.0)",
            "    Resolved as System.Runtime 4.3.0",
            "    System.Runtime 4.3.0 is already processed or queued.",
            "Processing System.Runtime 4.1.0",
            "  Package license:",
            "  http://go.microsoft.com/fwlink/?LinkId=329770",
            "  System.Runtime 4.1.0 is not in the destination.",
            "  Resolving dependency Microsoft.NETCore.Platforms (>=",
            "  1.0.1)",
            "    Resolved as Microsoft.NETCore.Platforms 1.0.1",
            "    Microsoft.NETCore.Platforms 1.0.1 is already processed",
            "    or queued.",
            "  Resolving dependency Microsoft.NETCore.Targets (>= 1.0.1)",
            "    Resolved as Microsoft.NETCore.Targets 1.0.1",
            "    Microsoft.NETCore.Targets 1.0.1 is already processed or",
            "    queued.",
            "Processing System.Runtime 4.1.1",
            "  Package license:",
            "  http://go.microsoft.com/fwlink/?LinkId=329770",
            "  System.Runtime 4.1.1 is not in the destination.",
            "  Resolving dependency Microsoft.NETCore.Platforms (>=",
            "  1.0.2)",
            "    Resolved as Microsoft.NETCore.Platforms 1.0.2",
            "    Microsoft.NETCore.Platforms 1.0.2 is queued for",
            "    processing.",
            "  Resolving dependency Microsoft.NETCore.Targets (>= 1.0.6)",
            "    Resolved as Microsoft.NETCore.Targets 1.0.6",
            "    Microsoft.NETCore.Targets 1.0.6 is queued for",
            "    processing.",
            "Processing System.Runtime 4.3.1",
            "  Package license:",
            "  http://go.microsoft.com/fwlink/?LinkId=329770",
            "  System.Runtime 4.3.1 is already in the destination.",
            "  Skipping dependencies of System.Runtime 4.3.1.",
            "Processing System.Runtime.CompilerServices.Unsafe 6.0.0",
            "  Package license: MIT",
            "  System.Runtime.CompilerServices.Unsafe 6.0.0 is not in the",
            "  destination.",
            "  System.Runtime.CompilerServices.Unsafe 6.0.0 has no",
            "  dependencies.",
            "Processing Microsoft.NETCore.Platforms 1.1.0",
            "  Package license:",
            "  http://go.microsoft.com/fwlink/?LinkId=329770",
            "  Microsoft.NETCore.Platforms 1.1.0 is not in the",
            "  destination.",
            "  Microsoft.NETCore.Platforms 1.1.0 has no dependencies.",
            "Processing Microsoft.NETCore.Targets 1.1.0",
            "  Package license:",
            "  http://go.microsoft.com/fwlink/?LinkId=329770",
            "  Microsoft.NETCore.Targets 1.1.0 is not in the destination.",
            "  Microsoft.NETCore.Targets 1.1.0 has no dependencies.",
            "Processing System.Runtime 4.3.0",
            "  Package license:",
            "  http://go.microsoft.com/fwlink/?LinkId=329770",
            "  System.Runtime 4.3.0 is not in the destination.",
            "  Resolving dependency Microsoft.NETCore.Platforms (>=",
            "  1.1.0)",
            "    Resolved as Microsoft.NETCore.Platforms 1.1.0",
            "    Microsoft.NETCore.Platforms 1.1.0 is already processed",
            "    or queued.",
            "  Resolving dependency Microsoft.NETCore.Targets (>= 1.1.0)",
            "    Resolved as Microsoft.NETCore.Targets 1.1.0",
            "    Microsoft.NETCore.Targets 1.1.0 is already processed or",
            "    queued.",
            "Processing Microsoft.NETCore.Platforms 1.0.1",
            "  Package license:",
            "  http://go.microsoft.com/fwlink/?LinkId=329770",
            "  Microsoft.NETCore.Platforms 1.0.1 is not in the",
            "  destination.",
            "  Microsoft.NETCore.Platforms 1.0.1 has no dependencies.",
            "Processing Microsoft.NETCore.Targets 1.0.1",
            "  Package license:",
            "  http://go.microsoft.com/fwlink/?LinkId=329770",
            "  Microsoft.NETCore.Targets 1.0.1 is not in the destination.",
            "  Microsoft.NETCore.Targets 1.0.1 has no dependencies.",
            "Processing Microsoft.NETCore.Platforms 1.0.2",
            "  Package license:",
            "  http://go.microsoft.com/fwlink/?LinkId=329770",
            "  Microsoft.NETCore.Platforms 1.0.2 is not in the",
            "  destination.",
            "  Microsoft.NETCore.Platforms 1.0.2 has no dependencies.",
            "Processing Microsoft.NETCore.Targets 1.0.6",
            "  Package license:",
            "  http://go.microsoft.com/fwlink/?LinkId=329770",
            "  Microsoft.NETCore.Targets 1.0.6 is not in the destination.",
            "  Microsoft.NETCore.Targets 1.0.6 has no dependencies."
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
            "├── System.Runtime 4.3.1 [exists]",
            "└── System.Runtime.CompilerServices.Unsafe 6.0.0"
        );

        result.StdOutput.Should().ContainInConsecutiveOrder(
            "Found 13 package(s) to promote:",
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
            "├── System.Runtime 4.3.0",
            "└── System.Runtime.CompilerServices.Unsafe 6.0.0"
        );

        result.StdOutput.Should().ContainInOrder(
            "Promoting 13 package(s)...",
            "(1/13) Promote Microsoft.NETCore.Platforms 1.0.1",
            "(2/13) Promote Microsoft.NETCore.Platforms 1.0.2",
            "(3/13) Promote Microsoft.NETCore.Platforms 1.1.0",
            "(4/13) Promote Microsoft.NETCore.Targets 1.0.1",
            "(5/13) Promote Microsoft.NETCore.Targets 1.0.6",
            "(6/13) Promote Microsoft.NETCore.Targets 1.1.0",
            "(7/13) Promote System.Collections 4.3.0",
            "(8/13) Promote System.Globalization 4.0.11",
            "(9/13) Promote System.Globalization 4.3.0",
            "(10/13) Promote System.Runtime 4.1.0",
            "(11/13) Promote System.Runtime 4.1.1",
            "(12/13) Promote System.Runtime 4.3.0",
            "(13/13) Promote System.Runtime.CompilerServices.Unsafe 6.0.0",
            "13 package(s) promoted."
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
        await AssertContainsVersions(
            destinationRepo,
            "System.Runtime.CompilerServices.Unsafe",
            new NuGetVersion(6, 0, 0));
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
