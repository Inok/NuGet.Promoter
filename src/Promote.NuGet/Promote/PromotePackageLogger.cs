using NuGet.Packaging.Core;
using NuGet.Versioning;
using Promote.NuGet.Commands.Promote;
using Promote.NuGet.Commands.Requests;
using Spectre.Console;

namespace Promote.NuGet.Promote;

public class PromotePackageLogger : IPromotePackageLogger
{
    public void LogResolvingMatchingPackages(IReadOnlyCollection<IPackageRequest> requests)
    {
        var tree = new Tree("[bold green]Resolving matching packages for:[/]");
        foreach (var request in requests.OrderBy(x => x.Id))
        {
            tree.AddNode(Markup.Escape(request.ToString()));
        }

        AnsiConsole.Write(tree);
    }

    public void LogPackageRequestResolution(IPackageRequest request, IReadOnlyCollection<PackageIdentity> matchingPackages)
    {
        var versionsString = string.Join(", ", matchingPackages.OrderBy(x => x.Id).ThenBy(x => x.Version).Select(r => r.Version));
        AnsiConsole.MarkupLineInterpolated($"[gray]Matching packages for {request}: {versionsString}[/]");
    }

    public void LogPackagePresentInDestination(PackageIdentity identity)
    {
        AnsiConsole.MarkupLineInterpolated($"[gray]Package {identity.Id} {identity.Version} is already present in the destination repository.[/]");
    }

    public void LogNoPackagesToPromote()
    {
        AnsiConsole.MarkupLineInterpolated($"[bold green]There are no packages to promote.[/]");
    }

    public void LogResolvingPackagesToPromote(IReadOnlyCollection<PackageIdentity> identities)
    {
        var tree = new Tree("[bold green]Resolving packages to promote:[/]");
        foreach (var identity in identities.OrderBy(x => x.Id).ThenBy(x => x.Version))
        {
            tree.AddNode(Markup.FromInterpolated($"{identity.Id} {identity.Version}"));
        }

        AnsiConsole.Write(tree);
    }

    public void LogProcessingPackage(PackageIdentity identity)
    {
        AnsiConsole.MarkupLine($"[gray]Processing package {identity.Id} {identity.Version}[/]");
    }

    public void LogProcessingDependency(string packageId, VersionRange versionRange)
    {
        AnsiConsole.MarkupLine($"[gray]Processing dependency {packageId} {versionRange.PrettyPrint()}[/]");
    }

    public void LogNewDependencyToProcess(string packageId, VersionRange versionRange)
    {
        AnsiConsole.MarkupLine($"[gray]New dependency to process: {packageId} {versionRange.PrettyPrint()}[/]");
    }

    public void LogNewDependencyFound(PackageIdentity identity)
    {
        AnsiConsole.MarkupLine($"[gray]New dependency found: {identity.Id} {identity.Version}[/]");
    }

    public void LogPackagesToPromote(IReadOnlyCollection<PackageIdentity> identities)
    {
        var tree = new Tree(Markup.FromInterpolated($"[bold green]Found {identities.Count} package(s) to promote:[/]"));
        foreach (var identity in identities.OrderBy(x => x.Id).ThenBy(x => x.Version))
        {
            tree.AddNode(Markup.FromInterpolated($"{identity.Id} {identity.Version}"));
        }

        AnsiConsole.Write(tree);
    }

    public void LogPromotePackage(PackageIdentity identity, int current, int total)
    {
        AnsiConsole.MarkupLine($"[bold green]({current}/{total}) Promote {identity.Id} {identity.Version}[/]");
    }

    public void LogPromotedPackagesCount(int count)
    {
        AnsiConsole.MarkupLine($"[bold green]{count} package(s) promoted.[/]");
    }

    public void LogDryRun()
    {
        AnsiConsole.MarkupLine("[bold green]Packages won't be promoted in dry run mode.[/]");
    }
}
