namespace Promote.NuGet.Commands.Promote;

public class PromotePackageCommandOptions
{
    public bool DryRun { get; }

    public bool AlwaysResolveDeps { get; }

    public bool ForcePush { get; }

    public PromotePackageCommandOptions(bool dryRun, bool alwaysResolveDeps, bool forcePush)
    {
        DryRun = dryRun;
        AlwaysResolveDeps = alwaysResolveDeps;
        ForcePush = forcePush;
    }
}