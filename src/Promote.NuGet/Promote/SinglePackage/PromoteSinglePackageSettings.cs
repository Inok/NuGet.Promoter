using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using NuGet.Versioning;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Promote.NuGet.Promote.SinglePackage;

internal sealed class PromoteSinglePackageSettings : PromoteSettings
{
    private const string EXPLICIT_LATEST_VERSION_STRING = "latest";

    [Description("Id of the package to promote.")]
    [CommandArgument(0, "<id>")]
    public string? Id { get; init; }

    [Description($"Version of the package. If not specified or set to '{EXPLICIT_LATEST_VERSION_STRING}', the most recent version will be promoted.")]
    [CommandOption("-v|--version")]
    public string? Version { get; init; }

    [MemberNotNullWhen(false, nameof(Version))]
    public bool IsLatestVersion => string.IsNullOrEmpty(Version) || string.Equals(Version, EXPLICIT_LATEST_VERSION_STRING, StringComparison.OrdinalIgnoreCase);

    public override ValidationResult Validate()
    {
        if (!IsLatestVersion && !NuGetVersion.TryParse(Version, out _))
        {
            return ValidationResult.Error("Cannot parse version.");
        }

        return base.Validate();
    }
}