using System.Collections.Concurrent;
using System.Diagnostics;

namespace Promote.NuGet.TestInfrastructure;

public sealed class ProcessWrapper : IAsyncDisposable
{
    private readonly ConcurrentQueue<string> _stdOut = new();
    private readonly ConcurrentQueue<string> _stdError = new();
    private int _processId;
    private Task _outputReadTask = Task.CompletedTask;
    private Task _errorReadTask = Task.CompletedTask;

    public Process Process { get; }

    public int ExitCode => Process.ExitCode;

    public IReadOnlyCollection<string> StdOut => _stdOut;

    public IReadOnlyCollection<string> StdError => _stdError;

    private ProcessWrapper(Process process)
    {
        Process = process;
        _processId = -1; // Will be set after Start()
    }

    public static ProcessWrapper Create(
        string fileName,
        IReadOnlyCollection<string> arguments,
        IReadOnlyDictionary<string, string>? environmentVariables = null)
    {
        TestContext.Out.WriteLine($"Running {fileName} {string.Join(" ", arguments)}");

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
            var wrapper = new ProcessWrapper(process);
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
        if (!Process.Start())
        {
            throw new InvalidOperationException("Failed to start the process");
        }

        _processId = Process.Id;

        // Use Task.Factory.StartNew with LongRunning option to ensure dedicated threads
        // Synchronous reading eliminates race conditions with fast-exiting processes
        _outputReadTask = Task.Factory.StartNew(() =>
        {
            string? line;
            while ((line = Process.StandardOutput.ReadLine()) != null)
            {
                _stdOut.Enqueue(line);
            }
        }, TaskCreationOptions.LongRunning);

        _errorReadTask = Task.Factory.StartNew(() =>
        {
            string? line;
            while ((line = Process.StandardError.ReadLine()) != null)
            {
                _stdError.Enqueue(line);
            }
        }, TaskCreationOptions.LongRunning);
    }

    public async Task WaitForExitAsync(CancellationToken cancellationToken = default)
    {
        // Wait for both output and error stream reading tasks to complete with cancellation support
        // This ensures all data has been captured before returning
        await Task.WhenAll(_outputReadTask, _errorReadTask).WaitAsync(cancellationToken);
        
        // Then wait for process to exit (defensive check, should already be done)
        await Process.WaitForExitAsync(cancellationToken);
    }

    public async Task<ProcessRunResult> WaitForExitAndGetResult(CancellationToken cancellationToken = default)
    {
        await WaitForExitAsync(cancellationToken);

        var stdOutput = StdOut.ToList();
        var stdError = StdError.ToList();

        /* Dump results to console */
        TestContext.Out.WriteLine($"Process '{Process.StartInfo.FileName}' (PID {_processId}) exited with code {Process.ExitCode}.");

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

        Process.Dispose();
    }
}
