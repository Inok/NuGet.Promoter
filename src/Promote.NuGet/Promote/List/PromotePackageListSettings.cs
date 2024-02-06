using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Promote.NuGet.Promote.List;

internal sealed class PromotePackageListSettings : PromoteSettings
{
    [Description(
        "Path of a file with a list of packages. Each line contains package id and its version or version range. Allowed formats:"
      + "\n- Space-separated: <id> <version/version-range>"
      + "\n- Package Manager: Install-Package <id> -Version <version/version-range>"
      + "\n- PackageReference: <PackageReference Include=\"<version>\" Version=\"<version/version-range>\" />"
    )]
    [CommandArgument(0, "<file>")]
    public string? File { get; init; }

    public override ValidationResult Validate()
    {
        if (!System.IO.File.Exists(File))
        {
            return ValidationResult.Error("The specified file does not exist.");
        }

        return base.Validate();
    }
}
