using NuGet.Packaging.Core;
using NuGet.Versioning;
using Promote.NuGet.Commands.Promote;
using Promote.NuGet.Commands.Promote.Resolution;
using Promote.NuGet.Commands.Requests;
using Spectre.Console;

namespace Promote.NuGet.Promote;

public class PromotePackageLogger : IPromotePackageLogger
{
    public void LogResolvingMatchingPackages(IReadOnlyCollection<PackageRequest> requests)
    {
        var tree = new Tree("[bold green]Resolving package requests:[/]");
        foreach (var request in requests.OrderBy(x => x.Id))
        {
            tree.AddNode(Markup.Escape(request.ToString()));
        }

        AnsiConsole.Write(tree);
    }

    public void LogPackageRequestResolution(PackageRequest request, IReadOnlyCollection<PackageIdentity> matchingPackages)
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

    public void LogNewDependencyToProcess(PackageIdentity source, string dependencyPackageId, VersionRange dependencyVersionRange)
    {
        AnsiConsole.MarkupLine($"[gray]New dependency of {source} to process: {dependencyPackageId} {dependencyVersionRange.PrettyPrint()}[/]");
    }

    public void LogProcessingDependency(PackageIdentity source, string dependencyPackageId, VersionRange dependencyVersionRange)
    {
        AnsiConsole.MarkupLine($"[gray]Processing dependency of {source.Id} {source.Version}: {dependencyPackageId} {dependencyVersionRange.PrettyPrint()}[/]");
    }

    public void LogResolvedDependency(PackageIdentity source, PackageIdentity resolvedDependency)
    {
        AnsiConsole.MarkupLine($"[gray]Dependency of {source.Id} {source.Version} resolved: {resolvedDependency.Id} {resolvedDependency.Version}[/]");
    }

    public void LogNewDependencyFound(PackageIdentity identity)
    {
        AnsiConsole.MarkupLine($"[gray]New dependency found: {identity.Id} {identity.Version}[/]");
    }

    public void LogResolvedPackageTree(PackageResolutionTree packageTree)
    {
        var expanded = new HashSet<PackageIdentity>();

        var tree = new Tree("[bold green]Resolved package tree:[/]");
        foreach (var rootPackage in packageTree.Roots.OrderBy(x => x.Id))
        {
            AddNodeAndChildren(tree, packageTree, rootPackage, expanded, rootLevel: true);
        }

        AnsiConsole.Write(tree);
    }

    private static void AddNodeAndChildren(IHasTreeNodes parentNode,
                                           PackageResolutionTree packageTree,
                                           PackageIdentity package,
                                           HashSet<PackageIdentity> expanded,
                                           bool rootLevel)
    {
        var isInTargetFeed = packageTree.IsInTargetFeed(package);
        var isRootPackage = packageTree.Roots.Contains(package);

        var labels = new List<string>();

        if (isInTargetFeed)
        {
            labels.Add("exists");
        }

        if (!rootLevel && isRootPackage)
        {
            labels.Add("root");
        }

        var labelsStr = labels.Count > 0 ? string.Join(", ", labels) : null;

        var node = parentNode.AddNode(Markup.FromInterpolated($"{package.Id} {package.Version}{(labelsStr != null ? $" [{labelsStr}]" : "")}"));

        var dependencies = packageTree.GetDependencies(package);

        if (!rootLevel && isRootPackage)
        {
            if (dependencies.Count > 0)
            {
                node.AddNode(Markup.FromInterpolated($"+ {dependencies.Count} direct dependencies (expanded below)"));
            }

            return;
        }

        var expandedBefore = !expanded.Add(package);
        if (expandedBefore)
        {
            if (dependencies.Count > 0)
            {
                node.AddNode(Markup.FromInterpolated($"+ {dependencies.Count} direct dependencies (expanded above)"));
            }

            return;
        }

        foreach (var child in dependencies.OrderBy(x => x))
        {
            AddNodeAndChildren(node, packageTree, child, expanded, rootLevel: false);
        }
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

    public void LogDryRun()
    {
        AnsiConsole.MarkupLine("[bold green]Packages won't be promoted in dry run mode.[/]");
    }

    public void LogStartMirroringPackagesCount(int count)
    {
        AnsiConsole.MarkupLine($"[bold green]Promoting {count} package(s)...[/]");
    }

    public void LogMirrorPackage(PackageIdentity identity, int current, int total)
    {
        AnsiConsole.MarkupLine($"[bold green]({current}/{total}) Promote {identity.Id} {identity.Version}[/]");
    }

    public void LogMirroredPackagesCount(int count)
    {
        AnsiConsole.MarkupLine($"[bold green]{count} package(s) promoted.[/]");
    }
}
