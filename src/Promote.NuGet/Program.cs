using Promote.NuGet.Promote.FromConfiguration;
using Promote.NuGet.Promote.List;
using Promote.NuGet.Promote.SinglePackage;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Promote.NuGet;

public static class Program
{
    private static async Task<int> Main(string[] args)
    {
        SetupConsole();

        var app = new CommandApp();

        app.Configure(configurator =>
                      {
                          configurator.AddBranch(
                              "promote",
                              x =>
                              {
                                  x.SetDescription("Promote packages and their dependencies from one feed to another.");

                                  x.AddCommand<PromoteSinglePackageCommand>("package")
                                   .WithDescription("Promotes the specified package and its dependencies from one feed to another.");

                                  x.AddCommand<PromotePackageListCommand>("list")
                                   .WithAlias("from-file")
                                   .WithDescription("Promotes packages listed in the specified file, and their dependencies.");

                                  x.AddCommand<PromoteFromConfigurationCommand>("from-config")
                                   .WithDescription("Promotes packages as configured in the specified file.");
                              });

                          configurator.SetExceptionHandler(ex =>
                                                           {
                                                               if (ex is OperationCanceledException or TaskCanceledException)
                                                               {
                                                                   AnsiConsole.MarkupLineInterpolated($"[yellow]The operation was canceled[/]");
                                                               }
                                                               else
                                                               {
                                                                   AnsiConsole.WriteException(ex);
                                                               }

                                                               return -1;
                                                           });
                      });

        return await app.RunAsync(args);
    }

    private static void SetupConsole()
    {
        var noAnsiCodesEnvVar = Environment.GetEnvironmentVariable("__NO_ANSI_CONTROL_CODES");
        if (!string.IsNullOrEmpty(noAnsiCodesEnvVar))
        {
            AnsiConsole.Profile.Capabilities.Ansi = false;
        }

        var consoleWidthEnvVar = Environment.GetEnvironmentVariable("__CONSOLE_WIDTH");
        if (!string.IsNullOrEmpty(consoleWidthEnvVar) && int.TryParse(consoleWidthEnvVar, out var width) && width > 0)
        {
            AnsiConsole.Profile.Width = width;
        }
    }
}
