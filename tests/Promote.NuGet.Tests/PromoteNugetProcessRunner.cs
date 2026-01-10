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
        var args = new List<string> { "Promote.NuGet.dll" };
        args.AddRange(arguments);

        var environmentVariables = new Dictionary<string, string>
        {
            ["__NO_ANSI_CONTROL_CODES"] = "1",
            ["__CONSOLE_WIDTH"] = ConsoleWidth.ToString(),
        };

        TestContext.Out.WriteLine($"Running dotnet {string.Join(" ", args)}");

        return ProcessWrapper.Create("dotnet", args, environmentVariables);
    }
}
