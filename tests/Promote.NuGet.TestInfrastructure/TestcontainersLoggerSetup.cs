using System.Runtime.CompilerServices;
using DotNet.Testcontainers.Configurations;
using Microsoft.Extensions.Logging;

namespace Promote.NuGet.TestInfrastructure;

public static class TestcontainersLoggerSetup
{
    [ModuleInitializer]
    public static void SetupTestcontainersLogger()
    {
        TestcontainersSettings.Logger = new NunitTestContextLogger();
    }

    private class NunitTestContextLogger : ILogger, IDisposable
    {
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
}
