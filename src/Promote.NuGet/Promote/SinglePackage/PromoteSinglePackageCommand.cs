using JetBrains.Annotations;
using NuGet.Common;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using Promote.NuGet.Commands.Promote;
using Promote.NuGet.Commands.Requests;
using Promote.NuGet.Feeds;
using Promote.NuGet.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Promote.NuGet.Promote.SinglePackage;

[PublicAPI]
internal sealed class PromoteSinglePackageCommand : CancellableAsyncCommand<PromoteSinglePackageSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, PromoteSinglePackageSettings promoteSettings, CancellationToken cancellationToken)
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

        var packageRequest = CreatePackageRequest(promoteSettings);

        var promoter = new PromotePackageCommand(sourceRepository, destinationRepository, new PromotePackageLogger());

        var options = new PromotePackageCommandOptions(promoteSettings.DryRun, promoteSettings.AlwaysResolveDeps, promoteSettings.ForcePush);

        var promotionResult = await promoter.Promote(new[] { packageRequest }, options, cancellationToken);
        if (promotionResult.IsFailure)
        {
            AnsiConsole.WriteLine(promotionResult.Error);
            return -1;
        }

        return 0;
    }

    private static PackageRequest CreatePackageRequest(PromoteSinglePackageSettings promoteSettings)
    {
        IPackageVersionPolicy versionPolicy = promoteSettings.IsLatestVersion
                                                    ? new LatestPackageVersionPolicy()
                                                    : new ExactPackageVersionPolicy(NuGetVersion.Parse(promoteSettings.Version));

        return new PackageRequest(promoteSettings.Id!, new[] { versionPolicy });
    }
}
