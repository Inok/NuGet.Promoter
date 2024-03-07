using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Promote.NuGet.Promote.FromConfiguration;

internal sealed class PromoteFromConfigurationCommandSettings : PromoteSettings
{
    [Description(
        """
        Path to the configuraton file (in YAML format). Example:
        packages:
          - id: System.Runtime
            versions: 4.3.1
          - id: System.Text.Json
            versions:
              - '[[6, 7)'
              - '[[8, 9)'
        """
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
