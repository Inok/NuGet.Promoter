using System.Diagnostics;

namespace Promote.NuGet.TestInfrastructure;

public sealed class ProcessWrapper : IAsyncDisposable
{
    private readonly List<string> _stdOut = new();
    private readonly List<string> _stdError = new();
    private readonly string _processName;
    private readonly int _processId;

    public Process Process { get; }

    public int ExitCode => Process.ExitCode;

    public IReadOnlyList<string> StdOut => _stdOut;

    public IReadOnlyList<string> StdError => _stdError;

    public ProcessWrapper(Process process)
    {
        Process = process;
        _processName = process.ProcessName;
        _processId = process.Id;
        SubscribeToOutputs();
    }

    public Task WaitForExitAsync(CancellationToken cancellationToken = default)
    {
        return Process.WaitForExitAsync(cancellationToken);
    }

    public async Task<ProcessRunResult> WaitForExitAndGetResult(CancellationToken cancellationToken = default)
    {
        await WaitForExitAsync(cancellationToken);

        // Ensure asynchronous event handling has been completed.
        // WaitForExitAsync (like WaitForExit with timeout) may return before all output is processed.
        // The parameterless WaitForExit ensures OutputDataReceived and ErrorDataReceived events have finished.
        // See: https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.waitforexit
        Process.WaitForExit();

        var stdOutput = StdOut.ToList();
        var stdError = StdError.ToList();

        /* Dump results to console */
        TestContext.Out.WriteLine($"Process '{_processName}' (PID {_processId}) exited with code {Process.ExitCode}.");

        if (stdOutput.Count > 0)
        {
            TestContext.Out.WriteLine("StdOut:");
            foreach (var line in stdOutput)
            {
                TestContext.Out.WriteLine("> " + line);
            }
        }

        if (stdError.Count > 0)
        {
            TestContext.Out.WriteLine("StdErr:");
            foreach (var line in stdError)
            {
                TestContext.Out.WriteLine("> " + line);
            }
        }

        return new ProcessRunResult(ExitCode, stdOutput, stdError);
    }

    private void SubscribeToOutputs()
    {
        Process.OutputDataReceived += (_, args) =>
                                      {
                                          if (args.Data != null)
                                          {
                                              _stdOut.Add(args.Data);
                                          }
                                      };
        Process.ErrorDataReceived += (_, args) =>
                                     {
                                         if (args.Data != null)
                                         {
                                             _stdError.Add(args.Data);
                                         }
                                     };

        Process.BeginOutputReadLine();
        Process.BeginErrorReadLine();
    }

    public async ValueTask DisposeAsync()
    {
        if (Process.HasExited == false)
        {
            TestContext.Out.WriteLine("Killing...");
            Process.Kill(entireProcessTree: true);

            await Process.WaitForExitAsync();

            TestContext.Out.WriteLine("The process is still running. Dumping its output and killing the process.");
            TestContext.Out.WriteLine("Error output:");
            TestContext.Out.WriteLine(await Process.StandardError.ReadToEndAsync());
            TestContext.Out.WriteLine("Standard output:");
            TestContext.Out.WriteLine(await Process.StandardOutput.ReadToEndAsync());

            TestContext.Out.WriteLine("The process is stopped.");
        }

        Process.Dispose();
    }
}
