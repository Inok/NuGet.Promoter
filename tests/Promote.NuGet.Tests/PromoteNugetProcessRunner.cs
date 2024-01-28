using System.Diagnostics;
using Promote.NuGet.TestInfrastructure;

namespace Promote.NuGet.Tests;

public static class PromoteNugetProcessRunner
{
    public static async Task<ProcessRunResult> RunForResultAsync(params string[] arguments)
    {
        var cancellationToken = TestContext.CurrentContext.CancellationToken;

        await using var process = Run(arguments);

        var result = await process.WaitForExitAndGetResult(cancellationToken);

        return result;
    }

    public static ProcessWrapper Run(params string[] arguments)
    {
        var processStartInfo = new ProcessStartInfo
                               {
                                   FileName = "dotnet",
                                   ArgumentList = { "Promote.NuGet.dll" },
                                   RedirectStandardOutput = true,
                                   RedirectStandardError = true,
                                   Environment =
                                   {
                                       ["__NO_ANSI_CONTROL_CODES"] = "1",
                                       ["__CONSOLE_WIDTH"] = "60",
                                   }
                               };

        foreach (var argument in arguments)
        {
            processStartInfo.ArgumentList.Add(argument);
        }

        TestContext.WriteLine($"Running {processStartInfo.FileName} {string.Join(" ", processStartInfo.ArgumentList)}");

        Process? process = null;
        try
        {
            process = Process.Start(processStartInfo);

            if (process == null)
            {
                Assert.Fail("Failed to run the process");
                Environment.FailFast("UNREACHABLE");
            }

            return new ProcessWrapper(process);
        }
        catch
        {
            process?.Dispose();
            throw;
        }
    }
}
