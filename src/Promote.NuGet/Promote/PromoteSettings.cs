using System.ComponentModel;
using Promote.NuGet.Feeds;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Promote.NuGet.Promote;

internal class PromoteSettings : CommandSettings
{
    [Description("Source repository.")]
    [CommandOption("-s|--source")]
    [DefaultValue(NuGetFeedRegistry.NUGET_ORG_V3_URL)]
    public string? Source { get; init; }

    [Description("Source repository's API key.")]
    [CommandOption("--source-api-key")]
    public string? SourceApiKey { get; init; }

    [Description("Destination repository.")]
    [CommandOption("-d|--destination")]
    public string? Destination { get; init; }

    [Description("Destination repository's API key.")]
    [CommandOption("--destination-api-key")]
    public string? DestinationApiKey { get; init; }

    [Description("Do not use local cache.")]
    [CommandOption("--no-cache")]
    public bool NoCache { get; init; }

    [Description("Evaluate packages to promote, but don't actually promote them.")]
    [CommandOption("--dry-run")]
    public bool DryRun { get; init; }

    [Description("Enable verbose logs.")]
    [CommandOption("--verbose")]
    public bool Verbose { get; init; }

    [Description("Always resolve dependencies of a package, even if the package itself exists in the destination repository. "
               + "This option allows to restore the integrity of the destination repository by promoting missing dependencies.")]
    [CommandOption("--always-resolve-deps")]
    public bool AlwaysResolveDeps { get; init; }

    [Description("Push packages and their dependencies even if they already exist in the destination repository. "
               + "Use that option to restore the integrity of the destination repository (i.e. when some packages in the feed are broken).")]
    [CommandOption("--force-push")]
    public bool ForcePush { get; init; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrEmpty(Source))
        {
            return ValidationResult.Error("Source repository must be specified.");
        }

        if (string.IsNullOrEmpty(Destination))
        {
            return ValidationResult.Error("Destination repository must be specified.");
        }

        return ValidationResult.Success();
    }
}