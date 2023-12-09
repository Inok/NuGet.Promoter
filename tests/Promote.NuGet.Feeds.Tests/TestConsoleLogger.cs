using NuGet.Common;

namespace Promote.NuGet.Feeds.Tests;

public sealed class TestConsoleLogger : LoggerBase
{
    public static TestConsoleLogger Instance { get; } = new();

    private TestConsoleLogger()
    {
    }

    public override void Log(ILogMessage message)
    {
        Console.WriteLine($"{message.Level:G}: {message.FormatWithCode()}");
    }

    public override Task LogAsync(ILogMessage message)
    {
        Log(message);
        return Task.CompletedTask;
    }
}
