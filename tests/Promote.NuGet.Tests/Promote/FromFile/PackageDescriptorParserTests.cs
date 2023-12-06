using NuGet.Packaging.Core;
using NuGet.Versioning;
using Promote.NuGet.Promote.FromFile;

namespace Promote.NuGet.Tests.Promote.FromFile;

[TestFixture]
public class PackageDescriptorParserTests
{
    [TestCase("PackageName 1.2.3", "PackageName", "[1.2.3]")]
    public void Parse_space_separated_package_descriptor(string input, string id, string versionRange)
    {
        var expected = new PackageDependency(id, VersionRange.Parse(versionRange));

        var result = PackageDescriptorParser.ParseLine(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expected);
    }
}
