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
                                       - id: Microsoft.Data.SqlClient.SNI.runtime
                                         versions:
                                           - 5.2.0
                                     """
                                 );

        await using var destinationFeed = await LocalNugetFeed.Create();

        var destinationFeedDescriptor = new NuGetRepositoryDescriptor(destinationFeed.FeedUrl, destinationFeed.ApiKey);
        using (var cacheContext = new SourceCacheContext { NoCache = true })
        {
            using var destinationRepo = new NuGetRepository(destinationFeedDescriptor, cacheContext, TestNuGetLogger.Instance);
            await PromotePackageToFeed(destinationRepo, new PackageIdentity("Microsoft.NETCore.Platforms", new NuGetVersion(1, 1, 1)));
            await PromotePackageToFeed(destinationRepo, new PackageIdentity("Microsoft.NETCore.Targets", new NuGetVersion(1, 1, 3)));
            await PromotePackageToFeed(destinationRepo, new PackageIdentity("System.Runtime", new NuGetVersion(4, 3, 1)));
        }

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
                "Found 1 matching package:",
                "└── 4.3.0",
                "Resolving System.Globalization 4.0.11, 4.3.0",
                "Found 2 matching packages:",
                "├── 4.0.11",
                "└── 4.3.0",
                "Resolving System.Runtime (>= 4.1.0 && < 4.1.2), 4.3.1",
                "Found 3 matching packages:",
                "├── 4.1.0",
                "├── 4.1.1",
                "└── 4.3.1",
                "Resolving System.Runtime.CompilerServices.Unsafe 6.0.0",
                "Found 1 matching package:",
                "└── 6.0.0",
                "Resolving Microsoft.Data.SqlClient.SNI.runtime 5.2.0",
                "Found 1 matching package:",
                "└── 5.2.0",
            }
        );

        result.StdOutput.Select(x => x.TrimEnd()).Should().ContainInConsecutiveOrder(
            "Resolving 8 packages to promote...",
            "Processing System.Collections 4.3.0",
            "  Package license: MICROSOFT .NET LIBRARY",
            "  (http://go.microsoft.com/fwlink/?LinkId=329770)",
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
            "  Package license: MICROSOFT .NET LIBRARY",
            "  (http://go.microsoft.com/fwlink/?LinkId=329770)",
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
            "  Package license: MICROSOFT .NET LIBRARY",
            "  (http://go.microsoft.com/fwlink/?LinkId=329770)",
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
            "  Package license: MICROSOFT .NET LIBRARY",
            "  (http://go.microsoft.com/fwlink/?LinkId=329770)",
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
            "  Package license: MICROSOFT .NET LIBRARY",
            "  (http://go.microsoft.com/fwlink/?LinkId=329770)",
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
            "  Package license: MICROSOFT .NET LIBRARY",
            "  (http://go.microsoft.com/fwlink/?LinkId=329770)",
            "  System.Runtime 4.3.1 is already in the destination.",
            "  Skipping dependencies of System.Runtime 4.3.1.",
            "Processing System.Runtime.CompilerServices.Unsafe 6.0.0",
            "  Package license: MIT (https://licenses.nuget.org/MIT)",
            "  System.Runtime.CompilerServices.Unsafe 6.0.0 is not in the",
            "  destination.",
            "  System.Runtime.CompilerServices.Unsafe 6.0.0 has no",
            "  dependencies.",
            "Processing Microsoft.Data.SqlClient.SNI.runtime 5.2.0",
            "  Package license:",
            "  https://www.nuget.org/packages/Microsoft.Data.SqlClient.SN",
            "  I.runtime/5.2.0/license",
            "  Microsoft.Data.SqlClient.SNI.runtime 5.2.0 is not in the",
            "  destination.",
            "  Microsoft.Data.SqlClient.SNI.runtime 5.2.0 has no",
            "  dependencies.",
            "Processing Microsoft.NETCore.Platforms 1.1.0",
            "  Package license: MICROSOFT .NET LIBRARY",
            "  (http://go.microsoft.com/fwlink/?LinkId=329770)",
            "  Microsoft.NETCore.Platforms 1.1.0 is not in the",
            "  destination.",
            "  Microsoft.NETCore.Platforms 1.1.0 has no dependencies.",
            "Processing Microsoft.NETCore.Targets 1.1.0",
            "  Package license: MICROSOFT .NET LIBRARY",
            "  (http://go.microsoft.com/fwlink/?LinkId=329770)",
            "  Microsoft.NETCore.Targets 1.1.0 is not in the destination.",
            "  Microsoft.NETCore.Targets 1.1.0 has no dependencies.",
            "Processing System.Runtime 4.3.0",
            "  Package license: MICROSOFT .NET LIBRARY",
            "  (http://go.microsoft.com/fwlink/?LinkId=329770)",
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
            "  Package license: MICROSOFT .NET LIBRARY",
            "  (http://go.microsoft.com/fwlink/?LinkId=329770)",
            "  Microsoft.NETCore.Platforms 1.0.1 is not in the",
            "  destination.",
            "  Microsoft.NETCore.Platforms 1.0.1 has no dependencies.",
            "Processing Microsoft.NETCore.Targets 1.0.1",
            "  Package license: MICROSOFT .NET LIBRARY",
            "  (http://go.microsoft.com/fwlink/?LinkId=329770)",
            "  Microsoft.NETCore.Targets 1.0.1 is not in the destination.",
            "  Microsoft.NETCore.Targets 1.0.1 has no dependencies.",
            "Processing Microsoft.NETCore.Platforms 1.0.2",
            "  Package license: MICROSOFT .NET LIBRARY",
            "  (http://go.microsoft.com/fwlink/?LinkId=329770)",
            "  Microsoft.NETCore.Platforms 1.0.2 is not in the",
            "  destination.",
            "  Microsoft.NETCore.Platforms 1.0.2 has no dependencies.",
            "Processing Microsoft.NETCore.Targets 1.0.6",
            "  Package license: MICROSOFT .NET LIBRARY",
            "  (http://go.microsoft.com/fwlink/?LinkId=329770)",
            "  Microsoft.NETCore.Targets 1.0.6 is not in the destination.",
            "  Microsoft.NETCore.Targets 1.0.6 has no dependencies."
        );

        result.StdOutput.Should().ContainInConsecutiveOrder(
            "Resolved package tree:",
            "├── Microsoft.Data.SqlClient.SNI.runtime 5.2.0",
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

        result.StdOutput.Select(x => x.TrimEnd()).Should().ContainInConsecutiveOrder(
            "Found 14 packages to promote:",
            "├── Microsoft.Data.SqlClient.SNI.runtime 5.2.0",
            "│   └── License:",
            "│       https://www.nuget.org/packages/Microsoft.Data.SqlCli",
            "│       ent.SNI.runtime/5.2.0/license",
            "├── Microsoft.NETCore.Platforms 1.0.1",
            "│   └── License: MICROSOFT .NET LIBRARY",
            "│       (http://go.microsoft.com/fwlink/?LinkId=329770)",
            "├── Microsoft.NETCore.Platforms 1.0.2",
            "│   └── License: MICROSOFT .NET LIBRARY",
            "│       (http://go.microsoft.com/fwlink/?LinkId=329770)",
            "├── Microsoft.NETCore.Platforms 1.1.0",
            "│   └── License: MICROSOFT .NET LIBRARY",
            "│       (http://go.microsoft.com/fwlink/?LinkId=329770)",
            "├── Microsoft.NETCore.Targets 1.0.1",
            "│   └── License: MICROSOFT .NET LIBRARY",
            "│       (http://go.microsoft.com/fwlink/?LinkId=329770)",
            "├── Microsoft.NETCore.Targets 1.0.6",
            "│   └── License: MICROSOFT .NET LIBRARY",
            "│       (http://go.microsoft.com/fwlink/?LinkId=329770)",
            "├── Microsoft.NETCore.Targets 1.1.0",
            "│   └── License: MICROSOFT .NET LIBRARY",
            "│       (http://go.microsoft.com/fwlink/?LinkId=329770)",
            "├── System.Collections 4.3.0",
            "│   └── License: MICROSOFT .NET LIBRARY",
            "│       (http://go.microsoft.com/fwlink/?LinkId=329770)",
            "├── System.Globalization 4.0.11",
            "│   └── License: MICROSOFT .NET LIBRARY",
            "│       (http://go.microsoft.com/fwlink/?LinkId=329770)",
            "├── System.Globalization 4.3.0",
            "│   └── License: MICROSOFT .NET LIBRARY",
            "│       (http://go.microsoft.com/fwlink/?LinkId=329770)",
            "├── System.Runtime 4.1.0",
            "│   └── License: MICROSOFT .NET LIBRARY",
            "│       (http://go.microsoft.com/fwlink/?LinkId=329770)",
            "├── System.Runtime 4.1.1",
            "│   └── License: MICROSOFT .NET LIBRARY",
            "│       (http://go.microsoft.com/fwlink/?LinkId=329770)",
            "├── System.Runtime 4.3.0",
            "│   └── License: MICROSOFT .NET LIBRARY",
            "│       (http://go.microsoft.com/fwlink/?LinkId=329770)",
            "└── System.Runtime.CompilerServices.Unsafe 6.0.0",
            "    └── License: MIT (https://licenses.nuget.org/MIT)"
        );

        result.StdOutput.Select(x => x.TrimEnd()).Should().ContainInConsecutiveOrder(
            "License summary:",
            "├── 12x: MICROSOFT .NET LIBRARY",
            "│   (http://go.microsoft.com/fwlink/?LinkId=329770)",
            "├── 1x:",
            "│   https://www.nuget.org/packages/Microsoft.Data.SqlClient.",
            "│   SNI.runtime/5.2.0/license",
            "└── 1x: MIT (https://licenses.nuget.org/MIT)"
        );

        result.StdOutput.Should().ContainInOrder(
            "Promoting 14 packages...",
            "(1/14) Promote Microsoft.Data.SqlClient.SNI.runtime 5.2.0",
            "(2/14) Promote Microsoft.NETCore.Platforms 1.0.1",
            "(3/14) Promote Microsoft.NETCore.Platforms 1.0.2",
            "(4/14) Promote Microsoft.NETCore.Platforms 1.1.0",
            "(5/14) Promote Microsoft.NETCore.Targets 1.0.1",
            "(6/14) Promote Microsoft.NETCore.Targets 1.0.6",
            "(7/14) Promote Microsoft.NETCore.Targets 1.1.0",
            "(8/14) Promote System.Collections 4.3.0",
            "(9/14) Promote System.Globalization 4.0.11",
            "(10/14) Promote System.Globalization 4.3.0",
            "(11/14) Promote System.Runtime 4.1.0",
            "(12/14) Promote System.Runtime 4.1.1",
            "(13/14) Promote System.Runtime 4.3.0",
            "(14/14) Promote System.Runtime.CompilerServices.Unsafe 6.0.0",
            "14 packages promoted."
        );

        result.StdError.Should().BeEmpty();
        result.ExitCode.Should().Be(0);

        // Recreate destination repo to reset cache
        using (var cacheContext = new SourceCacheContext { NoCache = true })
        {
            using var destinationRepo = new NuGetRepository(destinationFeedDescriptor, cacheContext, TestNuGetLogger.Instance);

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
            await AssertContainsVersions(
                destinationRepo,
                "Microsoft.Data.SqlClient.SNI.runtime",
                new NuGetVersion(5, 2, 0));
        }
    }

    [Test, CancelAfter(60_000)]
    public async Task Checks_license_compliance_with_whitelisted_licenses()
    {
        using var packagesFile = await TempFile.Create(
                                     """
                                     license-compliance-check:
                                       enabled: true
                                       accept-expressions:
                                         - MIT
                                       accept-urls:
                                         - http://go.microsoft.com/fwlink/?LinkId=329770
                                     packages:
                                       - id: System.Runtime
                                         versions: 4.3.1
                                       - id: Microsoft.Data.SqlClient.SNI.runtime
                                         versions:
                                           - 5.2.0
                                       - id: System.Runtime.CompilerServices.Unsafe
                                         versions:
                                           - 6.0.0
                                     """
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

        // Assert
        var actualOutput = string.Join(Environment.NewLine, result.StdOutput.Select(x => x.TrimEnd()));
        actualOutput.Should().Be(
            """
            Resolving package requests...
            Resolving System.Runtime 4.3.1
            Found 1 matching package:
            └── 4.3.1
            Resolving Microsoft.Data.SqlClient.SNI.runtime 5.2.0
            Found 1 matching package:
            └── 5.2.0
            Resolving System.Runtime.CompilerServices.Unsafe 6.0.0
            Found 1 matching package:
            └── 6.0.0
            Resolving 3 packages to promote...
            Processing System.Runtime 4.3.1
              Package license: MICROSOFT .NET LIBRARY
              (http://go.microsoft.com/fwlink/?LinkId=329770)
              System.Runtime 4.3.1 is not in the destination.
              Resolving dependency Microsoft.NETCore.Platforms (>=
              1.1.1)
                Resolved as Microsoft.NETCore.Platforms 1.1.1
                Microsoft.NETCore.Platforms 1.1.1 is queued for
                processing.
              Resolving dependency Microsoft.NETCore.Targets (>= 1.1.3)
                Resolved as Microsoft.NETCore.Targets 1.1.3
                Microsoft.NETCore.Targets 1.1.3 is queued for
                processing.
            Processing Microsoft.Data.SqlClient.SNI.runtime 5.2.0
              Package license:
              https://www.nuget.org/packages/Microsoft.Data.SqlClient.SN
              I.runtime/5.2.0/license
              Microsoft.Data.SqlClient.SNI.runtime 5.2.0 is not in the
              destination.
              Microsoft.Data.SqlClient.SNI.runtime 5.2.0 has no
              dependencies.
            Processing System.Runtime.CompilerServices.Unsafe 6.0.0
              Package license: MIT (https://licenses.nuget.org/MIT)
              System.Runtime.CompilerServices.Unsafe 6.0.0 is not in the
              destination.
              System.Runtime.CompilerServices.Unsafe 6.0.0 has no
              dependencies.
            Processing Microsoft.NETCore.Platforms 1.1.1
              Package license: MICROSOFT .NET LIBRARY
              (http://go.microsoft.com/fwlink/?LinkId=329770)
              Microsoft.NETCore.Platforms 1.1.1 is not in the
              destination.
              Microsoft.NETCore.Platforms 1.1.1 has no dependencies.
            Processing Microsoft.NETCore.Targets 1.1.3
              Package license: MICROSOFT .NET LIBRARY
              (http://go.microsoft.com/fwlink/?LinkId=329770)
              Microsoft.NETCore.Targets 1.1.3 is not in the destination.
              Microsoft.NETCore.Targets 1.1.3 has no dependencies.
            Resolved package tree:
            ├── Microsoft.Data.SqlClient.SNI.runtime 5.2.0
            ├── System.Runtime 4.3.1
            │   ├── Microsoft.NETCore.Platforms 1.1.1
            │   └── Microsoft.NETCore.Targets 1.1.3
            └── System.Runtime.CompilerServices.Unsafe 6.0.0
            Found 5 packages to promote:
            ├── Microsoft.Data.SqlClient.SNI.runtime 5.2.0
            │   └── License:
            │       https://www.nuget.org/packages/Microsoft.Data.SqlCli
            │       ent.SNI.runtime/5.2.0/license
            ├── Microsoft.NETCore.Platforms 1.1.1
            │   └── License: MICROSOFT .NET LIBRARY
            │       (http://go.microsoft.com/fwlink/?LinkId=329770)
            ├── Microsoft.NETCore.Targets 1.1.3
            │   └── License: MICROSOFT .NET LIBRARY
            │       (http://go.microsoft.com/fwlink/?LinkId=329770)
            ├── System.Runtime 4.3.1
            │   └── License: MICROSOFT .NET LIBRARY
            │       (http://go.microsoft.com/fwlink/?LinkId=329770)
            └── System.Runtime.CompilerServices.Unsafe 6.0.0
                └── License: MIT (https://licenses.nuget.org/MIT)
            License summary:
            ├── 3x: MICROSOFT .NET LIBRARY
            │   (http://go.microsoft.com/fwlink/?LinkId=329770)
            ├── 1x:
            │   https://www.nuget.org/packages/Microsoft.Data.SqlClient.
            │   SNI.runtime/5.2.0/license
            └── 1x: MIT (https://licenses.nuget.org/MIT)
            Checking license compliance...
            Checking Microsoft.Data.SqlClient.SNI.runtime 5.2.0
                License (file): LICENSE.txt
                [x] NOT IMPLEMENTED
            Checking Microsoft.NETCore.Platforms 1.1.1
                License (url):
                http://go.microsoft.com/fwlink/?LinkId=329770
                [v] The license url is in whitelist.
            Checking Microsoft.NETCore.Targets 1.1.3
                License (url):
                http://go.microsoft.com/fwlink/?LinkId=329770
                [v] The license url is in whitelist.
            Checking System.Runtime 4.3.1
                License (url):
                http://go.microsoft.com/fwlink/?LinkId=329770
                [v] The license url is in whitelist.
            Checking System.Runtime.CompilerServices.Unsafe 6.0.0
                License (expression): MIT
                [v] The license expression is in whitelist.
            1 license violation found:
            └── Microsoft.Data.SqlClient.SNI.runtime.5.2.0
                ├── License (file): LICENSE.txt
                └── Reason: NOT IMPLEMENTED
            License violations found.
            """
        );

        result.StdError.Should().BeEmpty();
        result.ExitCode.Should().Be(0);
    }

    [Test, CancelAfter(60_000)]
    public async Task Checks_license_compliance_when_no_licenses_are_accepted()
    {
        using var packagesFile = await TempFile.Create(
                                     """
                                     license-compliance-check:
                                       enabled: true
                                     packages:
                                       - id: System.Runtime
                                         versions: 4.3.1
                                       - id: Microsoft.Data.SqlClient.SNI.runtime
                                         versions:
                                           - 5.2.0
                                       - id: System.Runtime.CompilerServices.Unsafe
                                         versions:
                                           - 6.0.0
                                     """
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

        // Assert
        var actualOutput = string.Join(Environment.NewLine, result.StdOutput.Select(x => x.TrimEnd()));
        actualOutput.Should().Be(
            """
            Resolving package requests...
            Resolving System.Runtime 4.3.1
            Found 1 matching package:
            └── 4.3.1
            Resolving Microsoft.Data.SqlClient.SNI.runtime 5.2.0
            Found 1 matching package:
            └── 5.2.0
            Resolving System.Runtime.CompilerServices.Unsafe 6.0.0
            Found 1 matching package:
            └── 6.0.0
            Resolving 3 packages to promote...
            Processing System.Runtime 4.3.1
              Package license: MICROSOFT .NET LIBRARY
              (http://go.microsoft.com/fwlink/?LinkId=329770)
              System.Runtime 4.3.1 is not in the destination.
              Resolving dependency Microsoft.NETCore.Platforms (>=
              1.1.1)
                Resolved as Microsoft.NETCore.Platforms 1.1.1
                Microsoft.NETCore.Platforms 1.1.1 is queued for
                processing.
              Resolving dependency Microsoft.NETCore.Targets (>= 1.1.3)
                Resolved as Microsoft.NETCore.Targets 1.1.3
                Microsoft.NETCore.Targets 1.1.3 is queued for
                processing.
            Processing Microsoft.Data.SqlClient.SNI.runtime 5.2.0
              Package license:
              https://www.nuget.org/packages/Microsoft.Data.SqlClient.SN
              I.runtime/5.2.0/license
              Microsoft.Data.SqlClient.SNI.runtime 5.2.0 is not in the
              destination.
              Microsoft.Data.SqlClient.SNI.runtime 5.2.0 has no
              dependencies.
            Processing System.Runtime.CompilerServices.Unsafe 6.0.0
              Package license: MIT (https://licenses.nuget.org/MIT)
              System.Runtime.CompilerServices.Unsafe 6.0.0 is not in the
              destination.
              System.Runtime.CompilerServices.Unsafe 6.0.0 has no
              dependencies.
            Processing Microsoft.NETCore.Platforms 1.1.1
              Package license: MICROSOFT .NET LIBRARY
              (http://go.microsoft.com/fwlink/?LinkId=329770)
              Microsoft.NETCore.Platforms 1.1.1 is not in the
              destination.
              Microsoft.NETCore.Platforms 1.1.1 has no dependencies.
            Processing Microsoft.NETCore.Targets 1.1.3
              Package license: MICROSOFT .NET LIBRARY
              (http://go.microsoft.com/fwlink/?LinkId=329770)
              Microsoft.NETCore.Targets 1.1.3 is not in the destination.
              Microsoft.NETCore.Targets 1.1.3 has no dependencies.
            Resolved package tree:
            ├── Microsoft.Data.SqlClient.SNI.runtime 5.2.0
            ├── System.Runtime 4.3.1
            │   ├── Microsoft.NETCore.Platforms 1.1.1
            │   └── Microsoft.NETCore.Targets 1.1.3
            └── System.Runtime.CompilerServices.Unsafe 6.0.0
            Found 5 packages to promote:
            ├── Microsoft.Data.SqlClient.SNI.runtime 5.2.0
            │   └── License:
            │       https://www.nuget.org/packages/Microsoft.Data.SqlCli
            │       ent.SNI.runtime/5.2.0/license
            ├── Microsoft.NETCore.Platforms 1.1.1
            │   └── License: MICROSOFT .NET LIBRARY
            │       (http://go.microsoft.com/fwlink/?LinkId=329770)
            ├── Microsoft.NETCore.Targets 1.1.3
            │   └── License: MICROSOFT .NET LIBRARY
            │       (http://go.microsoft.com/fwlink/?LinkId=329770)
            ├── System.Runtime 4.3.1
            │   └── License: MICROSOFT .NET LIBRARY
            │       (http://go.microsoft.com/fwlink/?LinkId=329770)
            └── System.Runtime.CompilerServices.Unsafe 6.0.0
                └── License: MIT (https://licenses.nuget.org/MIT)
            License summary:
            ├── 3x: MICROSOFT .NET LIBRARY
            │   (http://go.microsoft.com/fwlink/?LinkId=329770)
            ├── 1x:
            │   https://www.nuget.org/packages/Microsoft.Data.SqlClient.
            │   SNI.runtime/5.2.0/license
            └── 1x: MIT (https://licenses.nuget.org/MIT)
            Checking license compliance...
            Checking Microsoft.Data.SqlClient.SNI.runtime 5.2.0
                License (file): LICENSE.txt
                [x] NOT IMPLEMENTED
            Checking Microsoft.NETCore.Platforms 1.1.1
                License (url):
                http://go.microsoft.com/fwlink/?LinkId=329770
                [x] The license url is not whitelisted.
            Checking Microsoft.NETCore.Targets 1.1.3
                License (url):
                http://go.microsoft.com/fwlink/?LinkId=329770
                [x] The license url is not whitelisted.
            Checking System.Runtime 4.3.1
                License (url):
                http://go.microsoft.com/fwlink/?LinkId=329770
                [x] The license url is not whitelisted.
            Checking System.Runtime.CompilerServices.Unsafe 6.0.0
                License (expression): MIT
                [x] The license expression is not whitelisted.
            5 license violations found:
            ├── Microsoft.Data.SqlClient.SNI.runtime.5.2.0
            │   ├── License (file): LICENSE.txt
            │   └── Reason: NOT IMPLEMENTED
            ├── Microsoft.NETCore.Platforms.1.1.1
            │   ├── License (url):
            │   │   http://go.microsoft.com/fwlink/?LinkId=329770
            │   └── Reason: The license url is not whitelisted.
            ├── Microsoft.NETCore.Targets.1.1.3
            │   ├── License (url):
            │   │   http://go.microsoft.com/fwlink/?LinkId=329770
            │   └── Reason: The license url is not whitelisted.
            ├── System.Runtime.4.3.1
            │   ├── License (url):
            │   │   http://go.microsoft.com/fwlink/?LinkId=329770
            │   └── Reason: The license url is not whitelisted.
            └── System.Runtime.CompilerServices.Unsafe.6.0.0
                ├── License (expression): MIT
                └── Reason: The license expression is not whitelisted.
            License violations found.
            """
        );

        result.StdError.Should().BeEmpty();
        result.ExitCode.Should().Be(0);
    }

    private static async Task PromotePackageToFeed(INuGetRepository destinationRepo, PackageIdentity packageId)
    {
        using var sourceRepo = new NuGetRepository(_nugetOrgRepositoryDescriptor, NullSourceCacheContext.Instance, TestNuGetLogger.Instance);

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
