using System;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode
    [Solution] readonly Solution Solution;

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath NuGetPromoterToolProject => SourceDirectory / "NuGet.Promoter";

    AbsolutePath TestsDirectory => RootDirectory / "tests";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    AbsolutePath TestResultsDirectory => RootDirectory / "test_results";

    public static int Main() => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    /************************/
    /* BUILD                */
    /************************/

    Target CleanOutputDirectories => _ => _
                                          .Before(Restore)
                                          .Executes(() =>
                                                    {
                                                        EnsureCleanDirectory(ArtifactsDirectory);
                                                        EnsureCleanDirectory(TestResultsDirectory);
                                                    });

    Target CleanSolution => _ => _
                                 .DependsOn(Restore)
                                 .Executes(() =>
                                           {
                                               DotNetClean(s => s.SetProject(Solution)
                                                                 .SetConfiguration(Configuration)
                                                                 .SetVerbosity(DotNetVerbosity.Minimal));
                                           });

    Target Restore => _ => _
                          .Executes(() =>
                                    {
                                        DotNetRestore(s => s.SetProjectFile(Solution));
                                    });

    Target Compile => _ => _
                           .DependsOn(CleanOutputDirectories, CleanSolution, Restore)
                           .Executes(() =>
                                     {
                                         DotNetBuild(s => s.SetProjectFile(Solution)
                                                           .SetConfiguration(Configuration)
                                                           .EnableNoRestore());
                                     });

    /************************/
    /* TEST                 */
    /************************/

    Target ReadyForTesting => _ => _.DependsOn(Compile)
                                    .Unlisted();

    void RunCoreTest(AbsolutePath root, string[] patterns, bool executeInParallel)
    {
        var projects = root.GlobFiles(patterns);
        Assert.NotEmpty(projects);

        DotNetTest(s => s
                        .SetConfiguration(Configuration)
                        .EnableNoBuild()
                        .EnableNoRestore()
                        .SetTestAdapterPath(".")
                        .SetResultsDirectory(TestResultsDirectory)
                        .CombineWith(
                            projects, (cs, v) =>
                                      {
                                          var logFilePath = TestResultsDirectory / "test-result-{assembly}-{framework}.xml";
                                          return cs.SetProjectFile(v)
                                                   .SetLoggers($"nunit;LogFilePath={logFilePath}");
                                      }),
                   degreeOfParallelism: executeInParallel ? Environment.ProcessorCount : 1,
                   completeOnFailure: true
        );
    }

    Target RunTests => _ => _
                            .DependsOn(ReadyForTesting)
                            .Executes(() =>
                                      {
                                          var projects = TestsDirectory.GlobFiles(new[] { "**/*.Tests.csproj" });
                                          Assert.NotEmpty(projects);

                                          DotNetTest(s => s
                                                          .SetConfiguration(Configuration)
                                                          .EnableNoBuild()
                                                          .EnableNoRestore()
                                                          .SetTestAdapterPath(".")
                                                          .SetResultsDirectory(TestResultsDirectory)
                                                          .CombineWith(projects, (cs, v) => cs.SetProjectFile(v)),
                                                     degreeOfParallelism: Environment.ProcessorCount,
                                                     completeOnFailure: true
                                          );
                                      });

    /************************/
    /* PACKAGING            */
    /************************/

    Target ReadyForPackaging => _ => _.DependsOn(Compile)
                                      .After(RunTests)
                                      .Unlisted();

    Target PackNugetPackages => _ => _
                                     .DependsOn(ReadyForPackaging)
                                     .Executes(() =>
                                               {
                                                   DotNetPack(s => s
                                                                   .SetConfiguration(Configuration)
                                                                   .EnableNoRestore()
                                                                   .EnableNoBuild()
                                                                   .SetProject(NuGetPromoterToolProject)
                                                                   .SetOutputDirectory(ArtifactsDirectory)
                                                   );
                                               });

    Target PackAll => _ => _
                          .DependsOn(PackNugetPackages);

    /************************/
    /* COMPOSITES           */
    /************************/

    Target All => _ => _.DependsOn(RunTests, PackAll);
}