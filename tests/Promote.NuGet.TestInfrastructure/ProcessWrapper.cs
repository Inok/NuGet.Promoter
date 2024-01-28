﻿using System.Diagnostics;

namespace Promote.NuGet.TestInfrastructure;

public sealed class ProcessWrapper : IAsyncDisposable
{
    private readonly List<string> _stdOut = new();
    private readonly List<string> _stdError = new();

    public Process Process { get; }

    public int ExitCode => Process.ExitCode;

    public IReadOnlyList<string> StdOut => _stdOut;

    public IReadOnlyList<string> StdError => _stdError;

    public ProcessWrapper(Process process)
    {
        Process = process;
        SubscribeToOutputs();
    }

    public Task WaitForExitAsync(CancellationToken cancellationToken = default)
    {
        return Process.WaitForExitAsync(cancellationToken);
    }

    public async Task<ProcessRunResult> WaitForExitAndGetResult(CancellationToken cancellationToken = default)
    {
        await WaitForExitAsync(cancellationToken);

        var stdOutput = StdOut.ToList();
        var stdError = StdError.ToList();

        /* Dump results to console */
        TestContext.WriteLine($"Process {Process} exited with code {Process.ExitCode}.");

        if (stdOutput.Count > 0)
        {
            TestContext.WriteLine("StdOut:");
            foreach (var line in stdOutput)
            {
                TestContext.WriteLine("> " + line);
            }
        }

        if (stdError.Count > 0)
        {
            TestContext.WriteLine("StdErr:");
            foreach (var line in stdError)
            {
                TestContext.WriteLine("> " + line);
            }
        }

        return new ProcessRunResult(ExitCode, stdOutput, stdError);
    }

    private void SubscribeToOutputs()
    {
        Process.OutputDataReceived += (_, args) =>
                                      {
                                          if (args.Data != null) _stdOut.Add(args.Data);
                                      };
        Process.ErrorDataReceived += (_, args) =>
                                     {
                                         if (args.Data != null) _stdError.Add(args.Data);
                                     };

        Process.BeginOutputReadLine();
        Process.BeginErrorReadLine();
    }

    public async ValueTask DisposeAsync()
    {
        if (Process.HasExited == false)
        {
            TestContext.WriteLine("Killing...");
            Process.Kill(entireProcessTree: true);

            await Process.WaitForExitAsync();

            TestContext.WriteLine("The process is still running. Dumping its output and killing the process.");
            TestContext.WriteLine("Error output:");
            TestContext.WriteLine(await Process.StandardError.ReadToEndAsync());
            TestContext.WriteLine("Standard output:");
            TestContext.WriteLine(await Process.StandardOutput.ReadToEndAsync());

            TestContext.WriteLine("The process is stopped.");
        }

        Process.Dispose();
    }
}
