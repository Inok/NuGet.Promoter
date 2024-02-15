using NuGet.Versioning;
using Promote.NuGet.Commands.Requests;
using Promote.NuGet.Promote.List;

namespace Promote.NuGet.Tests.Promote.List;

[TestFixture]
public class PackageDescriptorParserTests
{
    [TestCase("PackageName 1.2.3", "PackageName", "[1.2.3]")]
    public void Parse_space_separated_package_descriptor(string input, string id, string versionRange)
    {
        var expected = new VersionRangePackageRequest(id, VersionRange.Parse(versionRange));

        var result = PackageDescriptorParser.ParseLine(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expected);
    }
}
