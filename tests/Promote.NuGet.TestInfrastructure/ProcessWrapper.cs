using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;

namespace Promote.NuGet.TestInfrastructure;

public sealed class ProcessWrapper : IAsyncDisposable
{
    private readonly ConcurrentQueue<string> _stdOut = new();
    private readonly ConcurrentQueue<string> _stdError = new();
    private readonly string _processName;
    private readonly int _processId;
    private readonly Task _stdOutReadTask;
    private readonly Task _stdErrorReadTask;

    public Process Process { get; }

    public int ExitCode => Process.ExitCode;

    public IReadOnlyList<string> StdOut => _stdOut.ToList();

    public IReadOnlyList<string> StdError => _stdError.ToList();

    public ProcessWrapper(ProcessStartInfo processStartInfo)
    {
        Process = new Process { StartInfo = processStartInfo };
        
        if (!Process.Start())
        {
            throw new InvalidOperationException("Failed to start the process");
        }
        
        _processName = Process.ProcessName;
        _processId = Process.Id;
        
        // Start reading streams immediately in background tasks
        // This approach avoids the race condition with BeginOutputReadLine()
        TestContext.Out.WriteLine($"DEBUG: Starting stream reading tasks for PID {_processId}");
        _stdOutReadTask = ReadStreamAsync(Process.StandardOutput, _stdOut, "stdout");
        _stdErrorReadTask = ReadStreamAsync(Process.StandardError, _stdError, "stderr");
        TestContext.Out.WriteLine($"DEBUG: Stream reading tasks started");
    }

    private static async Task ReadStreamAsync(StreamReader reader, ConcurrentQueue<string> queue, string streamName)
    {
        try
        {
            TestContext.Out.WriteLine($"DEBUG: Starting to read {streamName}");
            string? line;
            int lineCount = 0;
            while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
            {
                lineCount++;
                TestContext.Out.WriteLine($"DEBUG: Read line {lineCount} from {streamName}: '{line}'");
                queue.Enqueue(line);
            }
            TestContext.Out.WriteLine($"DEBUG: Finished reading {streamName}, total lines: {lineCount}");
        }
        catch (Exception ex)
        {
            TestContext.Out.WriteLine($"DEBUG: Exception reading {streamName}: {ex.Message}");
            // Stream might be closed if process exits, ignore
        }
    }

    public Task WaitForExitAsync(CancellationToken cancellationToken = default)
    {
        return Process.WaitForExitAsync(cancellationToken);
    }

    public async Task<ProcessRunResult> WaitForExitAndGetResult(CancellationToken cancellationToken = default)
    {
        await WaitForExitAsync(cancellationToken);

        // Wait for both stream reading tasks to complete
        // This ensures all data has been read from the streams
        await Task.WhenAll(_stdOutReadTask, _stdErrorReadTask);

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

        // Wait for stream reading tasks to complete before disposing
        try
        {
            await Task.WhenAll(_stdOutReadTask, _stdErrorReadTask).ConfigureAwait(false);
        }
        catch
        {
            // Ignore exceptions during cleanup
        }

        Process.Dispose();
    }
}
