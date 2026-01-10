using System.Diagnostics;
using Promote.NuGet.TestInfrastructure;

namespace Promote.NuGet.Tests;

public static class PromoteNugetProcessRunner
{
    public const int ConsoleWidth = 60;

    public static async Task<ProcessRunResult> RunForResultAsync(params string[] arguments)
    {
        var cancellationToken = TestContext.CurrentContext.CancellationToken;

        await using var process = Run(arguments);

        var result = await process.WaitForExitAndGetResult(cancellationToken);

        return result;
    }

    public static ProcessWrapper Run(params string[] arguments)
    {
        return ProcessWrapper.Create(
            "dotnet",
            ["Promote.NuGet.dll", ..arguments],
            new Dictionary<string, string>
            {
                ["__NO_ANSI_CONTROL_CODES"] = "1",
                ["__CONSOLE_WIDTH"] = ConsoleWidth.ToString(),
            }
        );
    }
}
