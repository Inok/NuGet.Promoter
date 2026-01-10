using System.Collections.Concurrent;
using System.Diagnostics;

namespace Promote.NuGet.TestInfrastructure;

public sealed class ProcessWrapper : IAsyncDisposable
{
    private readonly ConcurrentQueue<string> _stdOut = new();
    private readonly ConcurrentQueue<string> _stdError = new();
    private readonly string _processName;
    private int _processId;
    private readonly TaskCompletionSource _outputComplete = new();
    private readonly TaskCompletionSource _errorComplete = new();

    public Process Process { get; }

    public int ExitCode => Process.ExitCode;

    public IReadOnlyCollection<string> StdOut => _stdOut;

    public IReadOnlyCollection<string> StdError => _stdError;

    private ProcessWrapper(Process process, string processName)
    {
        Process = process;
        _processName = processName;
        _processId = -1; // Will be set after Start()
    }

    public static ProcessWrapper Create(
        string fileName,
        IEnumerable<string> arguments,
        IReadOnlyDictionary<string, string>? environmentVariables = null)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        foreach (var argument in arguments)
        {
            processStartInfo.ArgumentList.Add(argument);
        }

        if (environmentVariables != null)
        {
            foreach (var (key, value) in environmentVariables)
            {
                processStartInfo.Environment[key] = value;
            }
        }

        var process = new Process { StartInfo = processStartInfo };

        try
        {
            var wrapper = new ProcessWrapper(process, fileName);
            wrapper.Start();
            return wrapper;
        }
        catch
        {
            process.Dispose();
            throw;
        }
    }

    private void Start()
    {
        // Attach event handlers BEFORE starting the process
        // This ensures handlers are ready to capture output from the moment the process starts
        AttachEventHandlers();

        if (!Process.Start())
        {
            throw new InvalidOperationException("Failed to start the process");
        }

        _processId = Process.Id;

        // Start asynchronous reading IMMEDIATELY after process starts
        // to minimize the window where output could be lost
        Process.BeginOutputReadLine();
        Process.BeginErrorReadLine();
    }

    private void AttachEventHandlers()
    {
        Process.OutputDataReceived += (_, args) =>
                                      {
                                          if (args.Data != null)
                                          {
                                              _stdOut.Enqueue(args.Data);
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
                                             _stdError.Enqueue(args.Data);
                                         }
                                         else
                                         {
                                             // null indicates end of stream
                                             _errorComplete.TrySetResult();
                                         }
                                     };
    }

    public async Task WaitForExitAsync(CancellationToken cancellationToken = default)
    {
        await Process.WaitForExitAsync(cancellationToken);
        
        // Wait for both output and error streams to signal completion
        // This ensures all data has been received from the async event handlers
        await Task.WhenAll(_outputComplete.Task, _errorComplete.Task);
    }

    public async Task<ProcessRunResult> WaitForExitAndGetResult(CancellationToken cancellationToken = default)
    {
        await WaitForExitAsync(cancellationToken);

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
