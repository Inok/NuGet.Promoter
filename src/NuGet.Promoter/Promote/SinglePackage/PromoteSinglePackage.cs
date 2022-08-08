using System;
using CSharpFunctionalExtensions;
using JetBrains.Annotations;
using NuGet.Common;
using NuGet.Packaging.Core;
using NuGet.Promoter.Commands;
using NuGet.Promoter.Commands.Core;
using NuGet.Promoter.Commands.Promote;
using NuGet.Promoter.Infrastructure;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using Spectre.Console;
using Spectre.Console.Cli;

namespace NuGet.Promoter.Promote.SinglePackage;

[PublicAPI]
internal sealed class PromoteSinglePackage : ICommand<PromoteSinglePackageSettings>
{
    public async Task<int> Execute(CommandContext context, PromoteSinglePackageSettings promoteSettings)
    {
        using var cacheContext = new SourceCacheContext
                                 {
                                     NoCache = promoteSettings.NoCache,
                                 };

        var nuGetLogger = new NuGetLogger(promoteSettings.Verbose ? LogLevel.Information : LogLevel.Minimal);

        var sourceRepository = CreateRepository(promoteSettings.Source!, promoteSettings.SourceApiKey);
        var destinationRepository = CreateRepository(promoteSettings.Destination!, promoteSettings.DestinationApiKey);

        var identityResult = await CreatePackageIdentity(sourceRepository, promoteSettings, cacheContext, nuGetLogger);
        if (identityResult.IsFailure)
        {
            AnsiConsole.WriteLine(identityResult.Error);
            return -1;
        }

        var promoter = new PromotePackageCommand(sourceRepository, destinationRepository, cacheContext, nuGetLogger, new PromotePackageLogger());

        var promotionResult = await promoter.Promote(identityResult.Value, promoteSettings.DryRun);
        if (promotionResult.IsFailure)
        {
            AnsiConsole.WriteLine(promotionResult.Error);
            return -1;
        }

        return 0;
    }

    private static NuGetRepository CreateRepository(string source, string? apiKey)
    {
        var sourceRepository = Repository.Factory.GetCoreV3(source);
        return new NuGetRepository(sourceRepository, apiKey);
    }

    private async Task<Result<PackageIdentity, string>> CreatePackageIdentity(NuGetRepository repository,
                                                                              PromoteSinglePackageSettings promoteSettings,
                                                                              SourceCacheContext cacheContext,
                                                                              NuGetLogger nuGetLogger)
    {
        if (!string.IsNullOrEmpty(promoteSettings.Version))
        {
            return new PackageIdentity(promoteSettings.Id, NuGetVersion.Parse(promoteSettings.Version));
        }

        var packageVersionFinder = new PackageVersionFinder(repository, cacheContext, nuGetLogger);
        return await packageVersionFinder.FindLatestVersion(promoteSettings.Id!);
    }

    public ValidationResult Validate(CommandContext context, CommandSettings settings)
    {
        return ValidationResult.Success();
    }

    public Task<int> Execute(CommandContext context, CommandSettings settings)
    {
        return Execute(context, (PromoteSinglePackageSettings)settings);
    }
}