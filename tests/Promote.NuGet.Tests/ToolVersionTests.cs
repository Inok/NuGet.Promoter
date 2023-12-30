using System.Diagnostics;

namespace Promote.NuGet.Tests;

[TestFixture]
public class ToolVersionTests
{
    [Test, CancelAfter(10_000)]
    public async Task Returns_version_of_the_tool()
    {
        var expectedVersion = FileVersionInfo.GetVersionInfo(typeof(Program).Assembly.Location).ProductVersion ?? string.Empty;
        var expectedVersionLines = expectedVersion.Chunk(80).Select(x => new string(x)).ToList();

        // Act
        var result = await PromoteNugetProcessRunner.RunForResultAsync("--version");

        // Assert
        result.StdOutput.Should().BeEquivalentTo(expectedVersionLines);
        result.StdError.Should().BeEmpty();
        result.ExitCode.Should().Be(0);
    }
}
