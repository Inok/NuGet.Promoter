using NuGet.Versioning;
using Promote.NuGet.Commands.Requests;
using Promote.NuGet.Promote.List;

namespace Promote.NuGet.Tests.Promote.List;

[TestFixture]
public class PackageDescriptorParserTests
{
    [TestCaseSource(nameof(PackagesTestCases))]
    public void Parse_package_descriptors(string input, PackageRequest expectedVersionRequest)
    {
        var result = PackageDescriptorParser.ParseLine(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedVersionRequest, x => x.RespectingRuntimeTypes());
    }

    private static IEnumerable<object[]> PackagesTestCases()
    {
        yield return new object[]
                     {
                         "Install-Package PackageName -Version 1.2.3",
                         new PackageRequest("PackageName", new ExactPackageVersionPolicy(new NuGetVersion(1, 2, 3)))
                     };

        yield return new object[]
                     {
                         "PackageName 1.2.3",
                         new PackageRequest("PackageName", new ExactPackageVersionPolicy(new NuGetVersion(1, 2, 3)))
                     };

        yield return new object[]
                     {
                         "PackageName [1.0.5,3)",
                         new PackageRequest(
                             "PackageName",
                             new VersionRangePackageVersionPolicy(new VersionRange(new NuGetVersion(1, 0, 5), true, new NuGetVersion(3, 0, 0), false))
                         )
                     };
    }
}
