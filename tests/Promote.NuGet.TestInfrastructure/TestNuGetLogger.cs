using NuGet.Common;

namespace Promote.NuGet.TestInfrastructure;

public sealed class TestNuGetLogger : LoggerBase
{
    public static TestNuGetLogger Instance { get; } = new();

    private TestNuGetLogger()
    {
    }

    public override void Log(ILogMessage message)
    {
        TestContext.Out.WriteLine($"[{TimeOnly.FromDateTime(DateTime.UtcNow):O} nuget] {message.FormatWithCode()}");
    }

    public override Task LogAsync(ILogMessage message)
    {
        Log(message);
        return Task.CompletedTask;
    }
}
