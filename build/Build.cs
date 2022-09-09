using System;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build : NukeBuild
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
    AbsolutePath TestResultsDirectory => RootDirectory / "test-results";

    public static int Main() => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    /************************/
    /* PREPARE              */
    /************************/

    protected override void OnBuildInitialized()
    {
        InitGitVersion();

        Serilog.Log.Information("Host: {0}, {1} ({2})", Host, Environment.MachineName, Environment.OSVersion);
        Serilog.Log.Information("Building MySport.Core in '{0}' configuration.", Configuration);
        Serilog.Log.Information("Branch name: {0}", GitVersion.BranchName);
        Serilog.Log.Information("Semantic version: {0}", GitVersion.FullSemVer);
        Serilog.Log.Information("Informational version: {0}", GitVersion.InformationalVersion);
    }

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
                                         var informationalVersion = GitVersion.InformationalVersion;
                                         var assemblyVersion = GitVersion.AssemblySemVer;
                                         var fileVersion = GitVersion.AssemblySemFileVer;

                                         Serilog.Log.Information("Build {0} in {1} configuration", Solution, Configuration);
                                         Serilog.Log.Debug("Informational version: {0}", informationalVersion);
                                         Serilog.Log.Debug("Assembly version: {0}", assemblyVersion);
                                         Serilog.Log.Debug("File version: {0}", fileVersion);

                                         DotNetBuild(s => s.SetProjectFile(Solution)
                                                           .SetConfiguration(Configuration)
                                                           .EnableNoRestore()
                                                           .SetAssemblyVersion(assemblyVersion)
                                                           .SetFileVersion(fileVersion)
                                                           .SetInformationalVersion(informationalVersion));
                                     });

    /************************/
    /* TEST                 */
    /************************/

    Target ReadyForTesting => _ => _.DependsOn(Compile)
                                    .Unlisted();

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
                                                          .SetResultsDirectory(TestResultsDirectory)
                                                          .CombineWith(projects,
                                                                       (cs, v) =>
                                                                       {
                                                                           var logFilePath = $"test-result-{v.NameWithoutExtension}-{Guid.NewGuid():N}.xml";
                                                                           return cs.SetProjectFile(v)
                                                                                    .SetLoggers($"trx;logfilename={logFilePath}");
                                                                       }),
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
                                                                   .SetVersion(GitVersion.FullSemVer)
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