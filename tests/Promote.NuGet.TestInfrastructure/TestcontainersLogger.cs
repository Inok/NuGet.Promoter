using Microsoft.Extensions.Logging;

namespace Promote.NuGet.TestInfrastructure;

public class TestcontainersLogger : ILogger, IDisposable
{
    public static ILogger Instance { get; } = new TestcontainersLogger();

    private TestcontainersLogger()
    {
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        TestContext.WriteLine($"[{TimeOnly.FromDateTime(DateTime.UtcNow):O} testcontainers] {formatter.Invoke(state, exception)}");
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= LogLevel.Debug;
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        return this;
    }

    public void Dispose()
    {
    }
}
