using System.Collections.Concurrent;
using System.Diagnostics;

namespace Promote.NuGet.TestInfrastructure;

public sealed class ProcessWrapper : IAsyncDisposable
{
    private readonly ConcurrentBag<string> _stdOut = new();
    private readonly ConcurrentBag<string> _stdError = new();
    private readonly string _processName;
    private readonly int _processId;
    private readonly TaskCompletionSource _outputComplete = new();
    private readonly TaskCompletionSource _errorComplete = new();

    public Process Process { get; }

    public int ExitCode => Process.ExitCode;

    public IReadOnlyList<string> StdOut => _stdOut.ToList();

    public IReadOnlyList<string> StdError => _stdError.ToList();

    public ProcessWrapper(ProcessStartInfo processStartInfo)
    {
        Process = new Process { StartInfo = processStartInfo };
        
        // Attach event handlers BEFORE starting the process
        // This ensures handlers are ready to capture output from the moment the process starts
        AttachEventHandlers();
        
        if (!Process.Start())
        {
            throw new InvalidOperationException("Failed to start the process");
        }
        
        // Start asynchronous reading IMMEDIATELY after process starts
        // to minimize the window where output could be lost
        Process.BeginOutputReadLine();
        Process.BeginErrorReadLine();
        
        _processName = Process.ProcessName;
        _processId = Process.Id;
    }

    private void AttachEventHandlers()
    {
        Process.OutputDataReceived += (_, args) =>
                                      {
                                          if (args.Data != null)
                                          {
                                              _stdOut.Add(args.Data);
                                          }
                                          else
                                          {
                                              // null indicates end of stream
                                              _outputComplete.TrySetResult();
                                          }
                                      };
        Process.ErrorDataReceived += (_, args) =>
                                     {
                                         if (args.Data != null)
                                         {
                                             _stdError.Add(args.Data);
                                         }
                                         else
                                         {
                                             // null indicates end of stream
                                             _errorComplete.TrySetResult();
                                         }
                                     };
    }

    public Task WaitForExitAsync(CancellationToken cancellationToken = default)
    {
        return Process.WaitForExitAsync(cancellationToken);
    }

    public async Task<ProcessRunResult> WaitForExitAndGetResult(CancellationToken cancellationToken = default)
    {
        await WaitForExitAsync(cancellationToken);

        // Wait for both output and error streams to signal completion
        // This ensures all data has been received from the async event handlers
        await Task.WhenAll(_outputComplete.Task, _errorComplete.Task);

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

    public async ValueTask DisposeAsync()
    {
        if (Process.HasExited == false)
        {
            TestContext.Out.WriteLine("Killing...");
            Process.Kill(entireProcessTree: true);

            await Process.WaitForExitAsync();

            TestContext.Out.WriteLine("The process was still running and has been killed.");
        }

        // Wait for stream completion signals before disposing
        try
        {
            await Task.WhenAll(_outputComplete.Task, _errorComplete.Task).ConfigureAwait(false);
        }
        catch
        {
            // Ignore exceptions during cleanup
        }

        Process.Dispose();
    }
}
