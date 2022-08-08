using System;
using System.IO;
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
using Spectre.Console;
using Spectre.Console.Cli;

namespace NuGet.Promoter.Promote.FromFile;

[PublicAPI]
internal sealed class PromotePackagesFromFile : ICommand<PromotePackagesFromFileSettings>
{
    public async Task<int> Execute(CommandContext context, PromotePackagesFromFileSettings promoteSettings)
    {
        using var cacheContext = new SourceCacheContext
                                 {
                                     NoCache = promoteSettings.NoCache,
                                 };

        var nuGetLogger = new NuGetLogger(promoteSettings.Verbose ? LogLevel.Information : LogLevel.Minimal);

        var sourceRepository = CreateRepository(promoteSettings.Source!, promoteSettings.SourceApiKey);
        var destinationRepository = CreateRepository(promoteSettings.Destination!, promoteSettings.DestinationApiKey);

        var identitiesResult = await ParsePackages(promoteSettings.File!);
        if (identitiesResult.IsFailure)
        {
            AnsiConsole.WriteLine(identitiesResult.Error);
            return -1;
        }

        var promoter = new PromotePackageCommand(sourceRepository, destinationRepository, cacheContext, nuGetLogger, new PromotePackageLogger());

        var promotionResult = await promoter.Promote(identitiesResult.Value, promoteSettings.DryRun);
        if (promotionResult.IsFailure)
        {
            AnsiConsole.WriteLine(promotionResult.Error);
            return -1;
        }

        return 0;
    }

    private async Task<Result<IReadOnlySet<PackageDependency>, string>> ParsePackages(string file)
    {
        var packages = new HashSet<PackageDependency>();

        var lines = await File.ReadAllLinesAsync(file);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parseIdentityResult = PackageDescriptorParser.ParseLine(line);
            if (parseIdentityResult.IsFailure)
            {
                return parseIdentityResult.Error;
            }

            packages.Add(parseIdentityResult.Value);
        }

        return packages;
    }

    private static NuGetRepository CreateRepository(string source, string? apiKey)
    {
        var sourceRepository = Repository.Factory.GetCoreV3(source);
        return new NuGetRepository(sourceRepository, apiKey);
    }

    public ValidationResult Validate(CommandContext context, CommandSettings settings)
    {
        return ValidationResult.Success();
    }

    public Task<int> Execute(CommandContext context, CommandSettings settings)
    {
        return Execute(context, (PromotePackagesFromFileSettings)settings);
    }
}