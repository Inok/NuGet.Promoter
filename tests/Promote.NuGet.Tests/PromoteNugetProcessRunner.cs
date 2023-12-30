using System.Diagnostics;
using System.IO;

namespace Promote.NuGet.Tests;

public static class PromoteNugetProcessRunner
{
    public sealed class ProcessWrapper : IAsyncDisposable
    {
        public Process Process { get; }

        public ProcessWrapper(Process process)
        {
            Process = process;
        }

        public void WaitForExit()
        {
            Process.WaitForExit();
        }

        public bool WaitForExit(int milliseconds)
        {
            return Process.WaitForExit(milliseconds);
        }

        public Task WaitForExitAsync(CancellationToken cancellationToken = default)
        {
            return Process.WaitForExitAsync(cancellationToken);
        }

        public int ExitCode => Process.ExitCode;

        public StreamReader StandardError => Process.StandardError;

        public StreamReader StandardOutput => Process.StandardOutput;

        public async ValueTask DisposeAsync()
        {
            if (Process.HasExited == false)
            {
                TestContext.WriteLine("The process is still running. Dumping its output and killing the process.");
                TestContext.WriteLine("Error output:");
                TestContext.WriteLine(await Process.StandardError.ReadToEndAsync());
                TestContext.WriteLine("Standard output:");
                TestContext.WriteLine(await Process.StandardOutput.ReadToEndAsync());

                TestContext.WriteLine("Killing...");
                Process.Kill();
                await Process.WaitForExitAsync();

                TestContext.WriteLine("The process is stopped.");
            }

            Process.Dispose();
        }
    }

    public record ProcessRunResult(int ExitCode, IReadOnlyCollection<string> StdOutput, IReadOnlyCollection<string> StdError);

    public static async Task<ProcessRunResult> RunForResultAsync(params string[] arguments)
    {
        await using var process = await RunToExitAsync(arguments);

        var stdOutput = new List<string>();
        while (await process.StandardOutput.ReadLineAsync() is { } line)
        {
            stdOutput.Add(line);
        }

        var stdError = new List<string>();
        while (await process.StandardError.ReadLineAsync() is { } line)
        {
            stdError.Add(line);
        }

        return new ProcessRunResult(process.ExitCode, stdOutput, stdError);
    }

    public static async Task<ProcessWrapper> RunToExitAsync(params string[] arguments)
    {
        var cancellationToken = TestContext.CurrentContext.CancellationToken;

        var process = Run(arguments);

        await process.WaitForExitAsync(cancellationToken);

        return process;
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
                                       ["__NO_ANSI_CONTROL_CODES"] = "1"
                                   }
                               };

        foreach (var argument in arguments)
        {
            processStartInfo.ArgumentList.Add(argument);
        }

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
