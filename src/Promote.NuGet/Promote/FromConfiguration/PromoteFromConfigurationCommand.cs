using System.IO;
using CSharpFunctionalExtensions;
using JetBrains.Annotations;
using NuGet.Common;
using NuGet.Protocol.Core.Types;
using Promote.NuGet.Commands.Promote;
using Promote.NuGet.Commands.Requests;
using Promote.NuGet.Feeds;
using Promote.NuGet.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Promote.NuGet.Promote.FromConfiguration;

[PublicAPI]
internal sealed class PromoteFromConfigurationCommand : CancellableAsyncCommand<PromoteFromConfigurationCommandSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, PromoteFromConfigurationCommandSettings promoteSettings, CancellationToken cancellationToken)
    {
        using var cacheContext = new SourceCacheContext
                                 {
                                     NoCache = promoteSettings.NoCache,
                                 };

        var nuGetLogger = new NuGetLogger(promoteSettings.Verbose ? LogLevel.Information : LogLevel.Minimal);

        var sourceDescriptor = new NuGetRepositoryDescriptor(promoteSettings.Source!, promoteSettings.SourceApiKey);
        var destinationDescriptor = new NuGetRepositoryDescriptor(promoteSettings.Destination!, promoteSettings.DestinationApiKey);

        var sourceRepository = new NuGetRepository(sourceDescriptor, cacheContext, nuGetLogger);
        var destinationRepository = new NuGetRepository(destinationDescriptor, cacheContext, nuGetLogger);

        var packageRequestsResult = await ReadConfiguration(promoteSettings.File!, cancellationToken);
        if (packageRequestsResult.IsFailure)
        {
            AnsiConsole.WriteLine(packageRequestsResult.Error);
            return -1;
        }

        var promoter = new PromotePackageCommand(sourceRepository, destinationRepository, new PromotePackageLogger());

        var options = new PromotePackageCommandOptions(promoteSettings.DryRun, promoteSettings.AlwaysResolveDeps, promoteSettings.ForcePush);

        var promotionResult = await promoter.Promote(packageRequestsResult.Value, options, cancellationToken);
        if (promotionResult.IsFailure)
        {
            AnsiConsole.WriteLine(promotionResult.Error);
            return -1;
        }

        return 0;
    }

    private static async Task<Result<IReadOnlyCollection<IPackageRequest>>> ReadConfiguration(string file, CancellationToken cancellationToken)
    {
        var input = await File.ReadAllTextAsync(file, cancellationToken);

        var parseResult = PackagesConfigurationParser.TryParse(input);
        if (parseResult.IsFailure)
        {
            return Result.Failure<IReadOnlyCollection<IPackageRequest>>(parseResult.Error);
        }

        return parseResult.Value.Packages.Select(x => new VersionRangePackageRequest(x.Id, x.Versions)).ToList();
    }

}
