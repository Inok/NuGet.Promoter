using System.IO;
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
        using var msLibLicense = await TempFile.Create(
            """
            MICROSOFT SOFTWARE LICENSE TERMS

            MICROSOFT.DATA.SQLCLIENT.SNI LIBRARY

            These license terms are an agreement between you and Microsoft Corporation (or based on where you live, one of its affiliates). They apply to the software named above. The terms also apply to any Microsoft services or updates for the software, except to the extent those have different terms.

            IF YOU COMPLY WITH THESE LICENSE TERMS, YOU HAVE THE RIGHTS BELOW.

            1.  INSTALLATION AND USE RIGHTS. You may install and use any number of copies of the software to develop and test your applications.
            2.  THIRD PARTY COMPONENTS. The software may include third party components with separate legal notices or governed by other agreements, as may be described in the ThirdPartyNotices file(s) accompanying the software.
            3.  ADDITIONAL LICENSING REQUIREMENTS AND/OR USE RIGHTS.
                a. DISTRIBUTABLE CODE.  The software is comprised of Distributable Code. "Distributable Code" is code that you are permitted to distribute in applications you develop if you comply with the terms below.
                   i. Right to Use and Distribute.
                      * You may copy and distribute the object code form of the software.
                      * Third Party Distribution. You may permit distributors of your applications to copy and distribute the Distributable Code as part of those applications.
                  ii. Distribution Requirements. For any Distributable Code you distribute, you must
                      * use the Distributable Code in your applications and not as a standalone distribution;
                      * require distributors and external end users to agree to terms that protect it at least as much as this agreement; and
                      * indemnify, defend, and hold harmless Microsoft from any claims, including attorneys' fees, related to the distribution or use of your applications, except to the extent that any claim is based solely on the unmodified Distributable Code.
                 iii. Distribution Restrictions. You may not
                      * use Microsoft's trademarks in your applications' names or in a way that suggests your applications come from or are endorsed by Microsoft; or
                      * modify or distribute the source code of any Distributable Code so that any part of it becomes subject to an Excluded License. An "Excluded License" is one that requires, as a condition of use, modification or distribution of code, that (i) it be disclosed or distributed in source code form; or (ii) others have the right to modify it.
            4.  DATA.
                a. Data Collection. Some features in the software may enable collection of data from users of your applications that access or use the software. If you use these features to enable data collection in your applications, you must comply with applicable law, including getting any required user consent, and maintain a prominent privacy policy that accurately informs users about how you use, collect, and share their data. You agree to comply with all applicable provisions of the Microsoft Privacy Statement at [https://go.microsoft.com/fwlink/?LinkId=521839].
            5.  SCOPE OF LICENSE. The software is licensed, not sold. This agreement only gives you some rights to use the software. Microsoft reserves all other rights. Unless applicable law gives you more rights despite this limitation, you may use the software only as expressly permitted in this agreement. In doing so, you must comply with any technical limitations in the software that only allow you to use it in certain ways. You may not
                * work around any technical limitations in the software;
                * reverse engineer, decompile or disassemble the software, or otherwise attempt to derive the source code for the software, except and to the extent required by third party licensing terms governing use of certain open source components that may be included in the software;
                * remove, minimize, block or modify any notices of Microsoft or its suppliers in the software;
                * use the software in any way that is against the law; or
                * share, publish, rent or lease the software, provide the software as a stand-alone offering for others to use, or transfer the software or this agreement to any third party.
            6.  EXPORT RESTRICTIONS. You must comply with all domestic and international export laws and regulations that apply to the software, which include restrictions on destinations, end users, and end use. For further information on export restrictions, visit www.microsoft.com/exporting.
            7.  SUPPORT SERVICES. Because this software is "as is," we may not provide support services for it.
            8.  ENTIRE AGREEMENT. This agreement, and the terms for supplements, updates, Internet-based services and support services that you use, are the entire agreement for the software and support services.
            9.  APPLICABLE LAW.  If you acquired the software in the United States, Washington law applies to interpretation of and claims for breach of this agreement, and the laws of the state where you live apply to all other claims. If you acquired the software in any other country, its laws apply.
            10. CONSUMER RIGHTS; REGIONAL VARIATIONS. This agreement describes certain legal rights. You may have other rights, including consumer rights, under the laws of your state or country. Separate and apart from your relationship with Microsoft, you may also have rights with respect to the party from which you acquired the software. This agreement does not change those other rights if the laws of your state or country do not permit it to do so. For example, if you acquired the software in one of the below regions, or mandatory country law applies, then the following provisions apply to you:
                a) Australia. You have statutory guarantees under the Australian Consumer Law and nothing in this agreement is intended to affect those rights.
                b) Canada. If you acquired this software in Canada, you may stop receiving updates by turning off the automatic update feature, disconnecting your device from the Internet (if and when you re-connect to the Internet, however, the software will resume checking for and installing updates), or uninstalling the software. The product documentation, if any, may also specify how to turn off updates for your specific device or software.
                c) Germany and Austria.
                   (i) Warranty. The software will perform substantially as described in any Microsoft materials that accompany it. However, Microsoft gives no contractual guarantee in relation to the software.
                   (ii) Limitation of Liability. In case of intentional conduct, gross negligence, claims based on the Product Liability Act, as well as in case of death or personal or physical injury, Microsoft is liable according to the statutory law.
                Subject to the foregoing clause (ii), Microsoft will only be liable for slight negligence if Microsoft is in breach of such material contractual obligations, the fulfillment of which facilitate the due performance of this agreement, the breach of which would endanger the purpose of this agreement and the compliance with which a party may constantly trust in (so-called "cardinal obligations"). In other cases of slight negligence, Microsoft will not be liable for slight negligence
            11. DISCLAIMER OF WARRANTY. THE SOFTWARE IS LICENSED "AS-IS." YOU BEAR THE RISK OF USING IT. MICROSOFT GIVES NO EXPRESS WARRANTIES, GUARANTEES OR CONDITIONS. TO THE EXTENT PERMITTED UNDER YOUR LOCAL LAWS, MICROSOFT EXCLUDES THE IMPLIED WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NON-INFRINGEMENT.
            12. LIMITATION ON AND EXCLUSION OF REMEDIES AND DAMAGES. YOU CAN RECOVER FROM MICROSOFT AND ITS SUPPLIERS ONLY DIRECT DAMAGES UP TO U.S. $5.00. YOU CANNOT RECOVER ANY OTHER DAMAGES, INCLUDING CONSEQUENTIAL, LOST PROFITS, SPECIAL, INDIRECT OR INCIDENTAL DAMAGES.
                This limitation applies to (a) anything related to the software, services, content (including code) on third party Internet sites, or third party applications; and (b) claims for breach of contract, breach of warranty, guarantee or condition, strict liability, negligence, or other tort to the extent permitted by applicable law.
                It also applies even if Microsoft knew or should have known about the possibility of the damages. The above limitation or exclusion may not apply to you because your state or country may not allow the exclusion or limitation of incidental, consequential or other damages.
            """);

        using var packagesFile = await TempFile.Create(
                                     $"""
                                     license-compliance-check:
                                       enabled: true
                                       accept-expressions:
                                         - MIT
                                       accept-urls:
                                         - http://go.microsoft.com/fwlink/?LinkId=329770
                                       accept-files:
                                         - '{msLibLicense.Path}'
                                       accept-no-license:
                                         - Newtonsoft.Json.3.5.8
                                     packages:
                                       - id: System.Runtime
                                         versions: 4.3.1
                                       - id: Microsoft.Data.SqlClient.SNI.runtime
                                         versions:
                                           - 5.2.0
                                       - id: System.Runtime.CompilerServices.Unsafe
                                         versions:
                                           - 6.0.0
                                       - id: Newtonsoft.Json
                                         versions:
                                           - 3.5.8
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
        actualOutput.Should().StartWith(
            $"""
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
            Resolving Newtonsoft.Json 3.5.8
            Found 1 matching package:
            └── 3.5.8
            Resolving 4 packages to promote...
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
            Processing Newtonsoft.Json 3.5.8
              Package license: <not set>
              Newtonsoft.Json 3.5.8 is not in the destination.
              Newtonsoft.Json 3.5.8 has no dependencies.
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
            ├── Newtonsoft.Json 3.5.8
            ├── System.Runtime 4.3.1
            │   ├── Microsoft.NETCore.Platforms 1.1.1
            │   └── Microsoft.NETCore.Targets 1.1.3
            └── System.Runtime.CompilerServices.Unsafe 6.0.0
            Found 6 packages to promote:
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
            ├── Newtonsoft.Json 3.5.8
            │   └── License: <not set>
            ├── System.Runtime 4.3.1
            │   └── License: MICROSOFT .NET LIBRARY
            │       (http://go.microsoft.com/fwlink/?LinkId=329770)
            └── System.Runtime.CompilerServices.Unsafe 6.0.0
                └── License: MIT (https://licenses.nuget.org/MIT)
            License summary:
            ├── 3x: MICROSOFT .NET LIBRARY
            │   (http://go.microsoft.com/fwlink/?LinkId=329770)
            ├── 1x: <not set>
            ├── 1x:
            │   https://www.nuget.org/packages/Microsoft.Data.SqlClient.
            │   SNI.runtime/5.2.0/license
            └── 1x: MIT (https://licenses.nuget.org/MIT)
            Checking license compliance...
            Checking Microsoft.Data.SqlClient.SNI.runtime 5.2.0
                License (file): LICENSE.txt
                [v] Matching accepted license file found: {Path.GetFileName(msLibLicense.Path)}.
            Checking Microsoft.NETCore.Platforms 1.1.1
                License (url):
                http://go.microsoft.com/fwlink/?LinkId=329770
                [v] The license url is in whitelist.
            Checking Microsoft.NETCore.Targets 1.1.3
                License (url):
                http://go.microsoft.com/fwlink/?LinkId=329770
                [v] The license url is in whitelist.
            Checking Newtonsoft.Json 3.5.8
                License (none): <not set>
                [v] The package is accepted to have no license.
            Checking System.Runtime 4.3.1
                License (url):
                http://go.microsoft.com/fwlink/?LinkId=329770
                [v] The license url is in whitelist.
            Checking System.Runtime.CompilerServices.Unsafe 6.0.0
                License (expression): MIT
                [v] The license expression is in whitelist.
            No license violations found.
            Promoting 6 packages...
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
                                       - id: LibGit2Sharp.NativeBinaries
                                         versions:
                                           - 2.0.322
                                       - id: Newtonsoft.Json
                                         versions:
                                           - 3.5.8
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
            Resolving LibGit2Sharp.NativeBinaries 2.0.322
            Found 1 matching package:
            └── 2.0.322
            Resolving Newtonsoft.Json 3.5.8
            Found 1 matching package:
            └── 3.5.8
            Resolving 5 packages to promote...
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
            Processing LibGit2Sharp.NativeBinaries 2.0.322
              Package license:
              https://www.nuget.org/packages/LibGit2Sharp.NativeBinaries
              /2.0.322/license
              LibGit2Sharp.NativeBinaries 2.0.322 is not in the
              destination.
              LibGit2Sharp.NativeBinaries 2.0.322 has no dependencies.
            Processing Newtonsoft.Json 3.5.8
              Package license: <not set>
              Newtonsoft.Json 3.5.8 is not in the destination.
              Newtonsoft.Json 3.5.8 has no dependencies.
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
            ├── LibGit2Sharp.NativeBinaries 2.0.322
            ├── Microsoft.Data.SqlClient.SNI.runtime 5.2.0
            ├── Newtonsoft.Json 3.5.8
            ├── System.Runtime 4.3.1
            │   ├── Microsoft.NETCore.Platforms 1.1.1
            │   └── Microsoft.NETCore.Targets 1.1.3
            └── System.Runtime.CompilerServices.Unsafe 6.0.0
            Found 7 packages to promote:
            ├── LibGit2Sharp.NativeBinaries 2.0.322
            │   └── License:
            │       https://www.nuget.org/packages/LibGit2Sharp.NativeBi
            │       naries/2.0.322/license
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
            ├── Newtonsoft.Json 3.5.8
            │   └── License: <not set>
            ├── System.Runtime 4.3.1
            │   └── License: MICROSOFT .NET LIBRARY
            │       (http://go.microsoft.com/fwlink/?LinkId=329770)
            └── System.Runtime.CompilerServices.Unsafe 6.0.0
                └── License: MIT (https://licenses.nuget.org/MIT)
            License summary:
            ├── 3x: MICROSOFT .NET LIBRARY
            │   (http://go.microsoft.com/fwlink/?LinkId=329770)
            ├── 1x: <not set>
            ├── 1x:
            │   https://www.nuget.org/packages/LibGit2Sharp.NativeBinari
            │   es/2.0.322/license
            ├── 1x:
            │   https://www.nuget.org/packages/Microsoft.Data.SqlClient.
            │   SNI.runtime/5.2.0/license
            └── 1x: MIT (https://licenses.nuget.org/MIT)
            Checking license compliance...
            Checking LibGit2Sharp.NativeBinaries 2.0.322
                License (file): libgit2\libgit2.license.txt
                [x] No matching license files found in the whitelist.
            Checking Microsoft.Data.SqlClient.SNI.runtime 5.2.0
                License (file): LICENSE.txt
                [x] No matching license files found in the whitelist.
            Checking Microsoft.NETCore.Platforms 1.1.1
                License (url):
                http://go.microsoft.com/fwlink/?LinkId=329770
                [x] The license url is not whitelisted.
            Checking Microsoft.NETCore.Targets 1.1.3
                License (url):
                http://go.microsoft.com/fwlink/?LinkId=329770
                [x] The license url is not whitelisted.
            Checking Newtonsoft.Json 3.5.8
                License (none): <not set>
                [x] License is not configured for the package.
            Checking System.Runtime 4.3.1
                License (url):
                http://go.microsoft.com/fwlink/?LinkId=329770
                [x] The license url is not whitelisted.
            Checking System.Runtime.CompilerServices.Unsafe 6.0.0
                License (expression): MIT
                [x] The license expression is not whitelisted.
            7 license violations found:
            ├── LibGit2Sharp.NativeBinaries.2.0.322
            │   ├── License (file): libgit2\libgit2.license.txt
            │   └── Reason: No matching license files found in the
            │       whitelist.
            ├── Microsoft.Data.SqlClient.SNI.runtime.5.2.0
            │   ├── License (file): LICENSE.txt
            │   └── Reason: No matching license files found in the
            │       whitelist.
            ├── Microsoft.NETCore.Platforms.1.1.1
            │   ├── License (url):
            │   │   http://go.microsoft.com/fwlink/?LinkId=329770
            │   └── Reason: The license url is not whitelisted.
            ├── Microsoft.NETCore.Targets.1.1.3
            │   ├── License (url):
            │   │   http://go.microsoft.com/fwlink/?LinkId=329770
            │   └── Reason: The license url is not whitelisted.
            ├── Newtonsoft.Json.3.5.8
            │   ├── License (none): <not set>
            │   └── Reason: License is not configured for the package.
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
        result.ExitCode.Should().BeOneOf(-1, 255);
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
