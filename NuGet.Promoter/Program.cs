using System;
using NuGet.Promoter.Promote.FromFile;
using NuGet.Promoter.Promote.SinglePackage;
using Spectre.Console;
using Spectre.Console.Cli;

namespace NuGet.Promoter;

public static class Program
{
    private static async Task<int> Main(string[] args)
    {
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
                                                               AnsiConsole.WriteException(ex);
                                                               return -1;
                                                           });
                      });

        return await app.RunAsync(args);
    }
}