using FluentValidation;
using NuGet.Versioning;
using Promote.NuGet.Commands.Requests;
using Promote.NuGet.Promote.FromConfiguration;

namespace Promote.NuGet.Tests.Promote.FromConfiguration;

[TestFixture]
public class PromoteConfigurationParserTests
{
    [Test]
    public void Parse_packages_configuration()
    {
        const string input =
            """
            packages:
              - id: System.Globalization
                versions: 4.3.0
              - id: System.Runtime
                versions:
                  - '[4.1.0,4.1.2)'
                  - 4.3.1
            """;

        var configuration = PromoteConfigurationParser.Parse(input);

        configuration.Should().BeEquivalentTo(
            new PromoteConfiguration
            {
                LicenseComplianceCheck = null,
                Packages =
                [
                    new PackageConfiguration
                               {
                                   Id = "System.Globalization",
                                   Versions =
                                   [
                                       new ExactPackageVersionPolicy(new NuGetVersion(4, 3, 0))
                                   ]
                               },
                               new PackageConfiguration
                               {
                                   Id = "System.Runtime",
                                   Versions =
                                   [
                                       new VersionRangePackageVersionPolicy(new VersionRange(new NuGetVersion(4, 1, 0), true, new NuGetVersion(4, 1, 2), false)),
                                       new ExactPackageVersionPolicy(new NuGetVersion(4, 3, 1))
                                   ]
                               }
                ]
            }, x => x.RespectingRuntimeTypes());
    }

    [Test]
    public void Parse_license_compliance_configuration()
    {
        const string input =
            """
            license-compliance-check:
              enabled: true
              accept-expressions:
                - MIT
                - Apache-2.0
              accept-urls:
                - http://go.microsoft.com/fwlink/?LinkId=329770 # MICROSOFT .NET LIBRARY
                - https://github.com/dotnet/corefx/blob/master/LICENSE.TXT
              accept-files:
                - ./third-party-licenses/MIT.txt

            packages:
              - id: System.Globalization
                versions: 4.3.0
            """;

        var configuration = PromoteConfigurationParser.Parse(input);

        configuration.Should().BeEquivalentTo(
            new PromoteConfiguration
            {
                LicenseComplianceCheck = new LicenseComplianceCheckConfiguration
                                         {
                                             Enabled = true,
                                             AcceptExpressions = ["MIT", "Apache-2.0"],
                                             AcceptUrls =
                                             [
                                                 "http://go.microsoft.com/fwlink/?LinkId=329770",
                                                 "https://github.com/dotnet/corefx/blob/master/LICENSE.TXT"
                                             ],
                                             AcceptFiles = ["./third-party-licenses/MIT.txt"],
                                         },
                Packages =
                [
                    new PackageConfiguration
                    {
                        Id = "System.Globalization",
                        Versions =
                        [
                            new ExactPackageVersionPolicy(new NuGetVersion(4, 3, 0))
                        ]
                    }
                ]
            }, x => x.RespectingRuntimeTypes());
    }

    [Test]
    public void Throw_validation_error_if_no_versions()
    {
        const string input =
            """
            packages:
              - id: System.Globalization
                versions: []
              - id: System.Runtime
                versions:
                  - '[4.1.0,4.1.2)'
                  - 4.3.1
            """;

        var action = () => PromoteConfigurationParser.Parse(input);

        // TODO: better message and assert
        action.Should().Throw<ValidationException>().Which.Message.Should()
              .Be($"""
                   Validation failed:{' '}
                    -- Packages[0].Versions: 'Versions' must not be empty. Severity: Error
                   """);
    }

    [Test]
    public void Throw_validation_error_if_empty_accepted_licenses()
    {
        const string input =
            """
            license-compliance-check:
              enabled: true
              accept-expressions:
                - MIT
                - ''
              accept-urls:
                - http://go.microsoft.com/fwlink/?LinkId=329770 # MICROSOFT .NET LIBRARY
                - ''
              accept-files:
                - ./third-party-licenses/MIT.txt
                - ''

            packages:
              - id: System.Globalization
                versions: 4.3.0

            """;

        var action = () => PromoteConfigurationParser.Parse(input);

        // TODO: better message and assert
        action.Should().Throw<ValidationException>().Which.Message.Should()
              .Be($"""
                   Validation failed:{' '}
                    -- LicenseComplianceCheck.AcceptExpressions[1]: 'Accept Expressions' must not be empty. Severity: Error
                    -- LicenseComplianceCheck.AcceptUrls[1]: 'Accept Urls' must not be empty. Severity: Error
                    -- LicenseComplianceCheck.AcceptFiles[1]: 'Accept Files' must not be empty. Severity: Error
                   """);
    }
}
