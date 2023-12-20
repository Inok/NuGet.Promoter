using System;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.GitVersion;

public partial class Build
{
    GitVersion GitVersion;

    void InitGitVersion()
    {
        Serilog.Log.Debug("Init GitVersion");

        var gitVersionSettings = new GitVersionSettings()
                                 .SetProcessWorkingDirectory(RootDirectory)
                                 .SetFramework("net6.0")
                                 .EnableNoFetch()
                                 .EnableNoCache();

        GitVersion = GitVersionTasks.GitVersion(gitVersionSettings).Result;
    }
}
