using System.IO;
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

namespace Promote.NuGet.Promote.List;

[PublicAPI]
internal sealed class PromotePackageListCommand : CancellableAsyncCommand<PromotePackageListSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, PromotePackageListSettings promoteSettings, CancellationToken cancellationToken)
    {
        using var cacheContext = new SourceCacheContext
                                 {
                                     NoCache = promoteSettings.NoCache,
                                 };

        var nuGetLogger = new NuGetLogger(promoteSettings.Verbose ? LogLevel.Information : LogLevel.Minimal);

        var sourceDescriptor = new NuGetRepositoryDescriptor(promoteSettings.Source!, null, null, promoteSettings.SourceApiKey);
        var destinationDescriptor = new NuGetRepositoryDescriptor(promoteSettings.Destination!, promoteSettings.DestinationUsername, promoteSettings.DestinationPassword, promoteSettings.DestinationApiKey);

        var sourceRepository = new NuGetRepository(sourceDescriptor, cacheContext, nuGetLogger);
        var destinationRepository = new NuGetRepository(destinationDescriptor, cacheContext, nuGetLogger);

        var identitiesResult = await ParsePackages(promoteSettings.File!, cancellationToken);
        if (identitiesResult.IsFailure)
        {
            AnsiConsole.WriteLine(identitiesResult.Error);
            return -1;
        }

        var promoter = new PromotePackageCommand(sourceRepository, destinationRepository, new PromotePackageLogger());

        var arguments = new PromotePackageCommandArguments(identitiesResult.Value, LicenseComplianceSettings.Disabled);
        var options = new PromotePackageCommandOptions(promoteSettings.DryRun, promoteSettings.AlwaysResolveDeps, promoteSettings.ForcePush);

        var promotionResult = await promoter.Promote(arguments, options, cancellationToken);
        if (promotionResult.IsFailure)
        {
            AnsiConsole.WriteLine(promotionResult.Error);
            return -1;
        }

        return 0;
    }

    private static async Task<Result<IReadOnlyCollection<PackageRequest>>> ParsePackages(string file, CancellationToken cancellationToken)
    {
        var packages = new List<PackageRequest>();

        var lines = await File.ReadAllLinesAsync(file, cancellationToken);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parseIdentityResult = PackageDescriptorParser.ParseLine(line);
            if (parseIdentityResult.IsFailure)
            {
                return Result.Failure<IReadOnlyCollection<PackageRequest>>(parseIdentityResult.Error);
            }

            packages.Add(parseIdentityResult.Value);
        }

        return packages;
    }
}
