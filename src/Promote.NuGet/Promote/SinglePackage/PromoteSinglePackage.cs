using System;
using CSharpFunctionalExtensions;
using JetBrains.Annotations;
using NuGet.Common;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using Promote.NuGet.Commands.Core;
using Promote.NuGet.Commands.Promote;
using Promote.NuGet.Feeds;
using Promote.NuGet.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Promote.NuGet.Promote.SinglePackage;

[PublicAPI]
internal sealed class PromoteSinglePackage : CancellableAsyncCommand<PromoteSinglePackageSettings>
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

        var identityResult = await CreatePackageIdentity(sourceRepository, promoteSettings, cancellationToken);
        if (identityResult.IsFailure)
        {
            AnsiConsole.WriteLine(identityResult.Error);
            return -1;
        }

        var promoter = new PromotePackageCommand(sourceRepository, destinationRepository, new PromotePackageLogger());

        var options = new PromotePackageCommandOptions(promoteSettings.DryRun, promoteSettings.AlwaysResolveDeps, promoteSettings.ForcePush);

        var promotionResult = await promoter.Promote(identityResult.Value, options, cancellationToken);
        if (promotionResult.IsFailure)
        {
            AnsiConsole.WriteLine(promotionResult.Error);
            return -1;
        }

        return 0;
    }

    private async Task<Result<PackageIdentity>> CreatePackageIdentity(INuGetRepository repository,
                                                                              PromoteSinglePackageSettings promoteSettings,
                                                                              CancellationToken cancellationToken)
    {
        if (!promoteSettings.IsLatestVersion)
        {
            return new PackageIdentity(promoteSettings.Id, NuGetVersion.Parse(promoteSettings.Version));
        }

        var packageVersionFinder = new PackageVersionFinder(repository);
        return await packageVersionFinder.FindLatestVersion(promoteSettings.Id!, cancellationToken);
    }
}