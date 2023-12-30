using Promote.NuGet.Promote.FromFile;
using Promote.NuGet.Promote.SinglePackage;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Promote.NuGet;

public static class Program
{
    private static async Task<int> Main(string[] args)
    {
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("__NO_ANSI_CONTROL_CODES")))
        {
            AnsiConsole.Profile.Capabilities.Ansi = false;
        }

        var app = new CommandApp();

        app.Configure(configurator =>
                      {
                          configurator.AddBranch(
                              "promote",
                              x =>
                              {
                                  x.SetDescription("Promote packages and its dependencies from one feed to another.");

                                  x.AddCommand<PromoteSinglePackage>("package")
                                   .WithDescription("Promotes the specified package and its dependencies from one feed to another.");

                                  x.AddCommand<PromotePackagesFromFile>("from-file")
                                   .WithDescription("Promotes packages listed in the specified file.");
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
}
