using System.Diagnostics;
using System.IO;

namespace Promote.NuGet.TestInfrastructure;

public sealed class ProcessWrapper : IAsyncDisposable
{
    public Process Process { get; }

    public int ExitCode => Process.ExitCode;

    public StreamReader StandardError => Process.StandardError;

    public StreamReader StandardOutput => Process.StandardOutput;

    public ProcessWrapper(Process process)
    {
        Process = process;
    }

    public Task WaitForExitAsync(CancellationToken cancellationToken = default)
    {
        return Process.WaitForExitAsync(cancellationToken);
    }

    public async Task<ProcessRunResult> WaitForExitAndGetResult(CancellationToken cancellationToken = default)
    {
        await WaitForExitAsync(cancellationToken);

        var stdOutput = new List<string>();
        while (await StandardOutput.ReadLineAsync() is { } line)
        {
            stdOutput.Add(line);
        }

        var stdError = new List<string>();
        while (await StandardError.ReadLineAsync() is { } line)
        {
            stdError.Add(line);
        }

        return new ProcessRunResult(ExitCode, stdOutput, stdError);
    }

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
