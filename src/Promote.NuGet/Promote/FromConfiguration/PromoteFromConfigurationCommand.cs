﻿using System.IO;
using CSharpFunctionalExtensions;
using JetBrains.Annotations;
using NuGet.Common;
using NuGet.Protocol.Core.Types;
using Promote.NuGet.Commands.Licensing;
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

        var sourceDescriptor = new NuGetRepositoryDescriptor(promoteSettings.Source!, null, null, promoteSettings.SourceApiKey);
        var destinationDescriptor = new NuGetRepositoryDescriptor(promoteSettings.Destination!, promoteSettings.DestinationUsername, promoteSettings.DestinationPassword, promoteSettings.DestinationApiKey);

        using var sourceRepository = new NuGetRepository(sourceDescriptor, cacheContext, nuGetLogger);
        using var destinationRepository = new NuGetRepository(destinationDescriptor, cacheContext, nuGetLogger);

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

    private static async Task<Result<PromotePackageCommandArguments>> ReadConfiguration(string file, CancellationToken cancellationToken)
    {
        file = Path.GetFullPath(file);

        var input = await File.ReadAllTextAsync(file, cancellationToken);

        var parseResult = PromoteConfigurationParser.TryParse(input);
        if (parseResult.IsFailure)
        {
            return Result.Failure<PromotePackageCommandArguments>(parseResult.Error);
        }

        var configurationDirectory = Path.GetDirectoryName(file);
        Normalize(parseResult.Value, configurationDirectory);

        var requests = parseResult.Value.Packages.Select(x => new PackageRequest(x.Id, x.Versions)).ToList();

        var complianceOptions = parseResult.Value.LicenseComplianceCheck;
        var licenseComplianceSettings = complianceOptions != null
                                            ? new LicenseComplianceSettings
                                              {
                                                  Enabled = complianceOptions.Enabled,
                                                  AcceptExpressions = complianceOptions.AcceptExpressions ?? [],
                                                  AcceptUrls = complianceOptions.AcceptUrls ?? [],
                                                  AcceptFiles = complianceOptions.AcceptFiles ?? [],
                                                  AcceptNoLicense = complianceOptions.AcceptNoLicense ?? [],
                                              }
                                            : LicenseComplianceSettings.Disabled;

        return new PromotePackageCommandArguments(requests, licenseComplianceSettings);
    }

    private static void Normalize(PromoteConfiguration configuration, string? relativePathResolutionRoot)
    {
        if (configuration.LicenseComplianceCheck?.AcceptFiles is { } files && relativePathResolutionRoot != null)
        {
            for (var i = 0; i < files.Length; i++)
            {
                files[i] = Path.GetFullPath(files[i], relativePathResolutionRoot);
            }
        }
    }
}
