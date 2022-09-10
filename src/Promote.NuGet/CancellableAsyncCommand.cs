using Spectre.Console.Cli;

namespace Promote.NuGet;

public abstract class CancellableAsyncCommand<TSettings> : AsyncCommand<TSettings>
    where TSettings : CommandSettings
{
    public abstract Task<int> ExecuteAsync(CommandContext context, TSettings settings, CancellationToken cancellationToken);

    public sealed override async Task<int> ExecuteAsync(CommandContext context, TSettings settings)
    {
        using var cts = new ConsoleAppCancellationTokenSource();
        return await ExecuteAsync(context, settings, cts.Token);
    }
}