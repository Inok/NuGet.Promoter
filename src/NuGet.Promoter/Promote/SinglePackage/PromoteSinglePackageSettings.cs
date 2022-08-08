using System.ComponentModel;
using NuGet.Versioning;
using Spectre.Console;
using Spectre.Console.Cli;

namespace NuGet.Promoter.Promote.SinglePackage;

internal sealed class PromoteSinglePackageSettings : PromoteSettings
{
    [Description("Id of the package to promote.")]
    [CommandArgument(0, "<id>")]
    public string? Id { get; init; }

    [Description("Version of the package. If not specified, the most recent version will be promoted.")]
    [CommandOption("-v|--version")]
    public string? Version { get; init; }

    public override ValidationResult Validate()
    {
        if (!string.IsNullOrEmpty(Version) && !NuGetVersion.TryParse(Version, out _))
        {
            return ValidationResult.Error("Cannot parse version.");
        }

        return base.Validate();
    }
}