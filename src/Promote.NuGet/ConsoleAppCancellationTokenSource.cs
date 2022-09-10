namespace Promote.NuGet;

internal sealed class ConsoleAppCancellationTokenSource : IDisposable
{
    private readonly CancellationTokenSource _cts = new();

    public CancellationToken Token => _cts.Token;

    public ConsoleAppCancellationTokenSource()
    {
        Console.CancelKeyPress += OnCancelKeyPress;
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

        using var _ = _cts.Token.Register(
            () =>
            {
                AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
                Console.CancelKeyPress -= OnCancelKeyPress;
            }
        );
    }

    private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        e.Cancel = true;
        _cts.Cancel();
    }

    private void OnProcessExit(object? sender, EventArgs e)
    {
        if (_cts.IsCancellationRequested)
        {
            return;
        }

        _cts.Cancel();
    }

    public void Dispose()
    {
        AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
        Console.CancelKeyPress -= OnCancelKeyPress;
        _cts.Dispose();
    }
}