using NuGet.Common;
using Spectre.Console;

namespace Promote.NuGet.Infrastructure;

public class NuGetLogger : LoggerBase
{
    public NuGetLogger()
    {
    }

    public NuGetLogger(LogLevel verbosityLevel)
        : base(verbosityLevel)
    {
    }

    public override void Log(ILogMessage message)
    {
        switch (message.Level)
        {
            case LogLevel.Debug:
                AnsiConsole.MarkupLineInterpolated($"[gray]Debug: {FormatMessage(message)}[/]");
                break;
            case LogLevel.Verbose:
                AnsiConsole.MarkupLineInterpolated($"[gray]Verbose: {FormatMessage(message)}[/]");
                break;
            case LogLevel.Information:
                AnsiConsole.MarkupLineInterpolated($"Info: {FormatMessage(message)}");
                break;
            case LogLevel.Minimal:
                AnsiConsole.MarkupLineInterpolated($"[yellow]Minimal: {FormatMessage(message)}[/]");
                break;
            case LogLevel.Warning:
                AnsiConsole.MarkupLineInterpolated($"[yellow]Warning: {FormatMessage(message)}[/]");
                break;
            case LogLevel.Error:
                AnsiConsole.MarkupLineInterpolated($"[red]Error: {FormatMessage(message)}[/]");
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public override Task LogAsync(ILogMessage message)
    {
        Log(message);
        return Task.CompletedTask;
    }

    private static string FormatMessage(ILogMessage message)
    {
        return message.FormatWithCode();
    }
}