using NuGet.Versioning;
using Promote.NuGet.Promote.FromConfiguration;

namespace Promote.NuGet.Tests.Promote.FromConfiguration;

[TestFixture]
public class PackagesConfigurationParserTests
{
    [Test]
    public void Parse_configuration()
    {
        var input =
            "packages:\n"
          + "  - id: System.Globalization\n"
          + "    versions: 4.3.0\n"
          + "  - id: System.Runtime\n"
          + "    versions:\n"
          + "      - '[4.1.0,4.1.2)'\n"
          + "      - 4.3.1\n";

        var configuration = PackagesConfigurationParser.Parse(input);

        configuration.Should().BeEquivalentTo(
            new PackagesConfiguration
            {
                Packages = new[]
                           {
                               new PackageConfiguration
                               {
                                   Id = "System.Globalization",
                                   Versions = new[]
                                              {
                                                  new VersionRange(new NuGetVersion(4, 3, 0), true, new NuGetVersion(4, 3, 0), true)
                                              }
                               },
                               new PackageConfiguration
                               {
                                   Id = "System.Runtime",
                                   Versions = new[]
                                              {
                                                  new VersionRange(new NuGetVersion(4, 1, 0), true, new NuGetVersion(4, 1, 2), false),
                                                  new VersionRange(new NuGetVersion(4, 3, 1), true, new NuGetVersion(4, 3, 1), true),
                                              }
                               }
                           }
            });
    }
}
