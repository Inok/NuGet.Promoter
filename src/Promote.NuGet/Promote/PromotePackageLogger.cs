using NuGet.Packaging.Core;
using NuGet.Versioning;
using Promote.NuGet.Commands.Promote;
using Spectre.Console;

namespace Promote.NuGet.Promote;

public class PromotePackageLogger : IPromotePackageLogger
{
    public void LogResolvingMatchingPackages(IReadOnlyCollection<PackageDependency> dependencies)
    {
        var tree = new Tree("[bold green]Resolving matching packages for:[/]");
        foreach (var dep in dependencies.OrderBy(x => x.Id))
        {
            tree.AddNode(Markup.FromInterpolated($"{dep.Id} {dep.VersionRange.PrettyPrint()}"));
        }

        AnsiConsole.Write(tree);
    }

    public void LogMatchingPackagesResolved(string packageId,
                                            IReadOnlyCollection<VersionRange> versionRanges,
                                            IReadOnlyCollection<PackageIdentity> matchingPackages)
    {
        var rangesString = string.Join(", ", versionRanges.Select(r => r.PrettyPrint()));
        var versionsString = string.Join(", ", matchingPackages.Select(r => r.Version));
        AnsiConsole.MarkupLineInterpolated($"[gray]Matching packages for {packageId} {rangesString}: {versionsString}[/]");
    }

    public void LogFilteringPresentPackages(IReadOnlySet<PackageIdentity> identities)
    {
        AnsiConsole.MarkupLineInterpolated($"[bold green]Filtering packages that are already present in the destination repository...[/]");
    }

    public void LogPackagePresentInDestination(PackageIdentity identity)
    {
        AnsiConsole.MarkupLineInterpolated($"[gray]Package {identity.Id} {identity.Version} is already present in the destination repository.[/]");
    }

    public void LogNoPackagesToPromote()
    {
        AnsiConsole.MarkupLineInterpolated($"[bold green]There are no packages to promote.[/]");
    }

    public void LogResolvingDependencies(IReadOnlyCollection<PackageIdentity> identities)
    {
        var tree = new Tree("[bold green]Resolving dependencies for:[/]");
        foreach (var identity in identities)
        {
            tree.AddNode(Markup.FromInterpolated($"{identity.Id} {identity.Version}"));
        }

        AnsiConsole.Write(tree);
    }

    public void LogProcessingDependenciesOfPackage(PackageIdentity identity)
    {
        AnsiConsole.MarkupLine($"[gray]Processing deps: {identity.Id} {identity.Version}[/]");
    }

    public void LogNewDependencyFound(PackageIdentity identity)
    {
        AnsiConsole.MarkupLine($"[gray]New dependency found: {identity.Id} {identity.Version}[/]");
    }

    public void LogPackagesToPromote(IReadOnlyCollection<PackageIdentity> identities)
    {
        var tree = new Tree(Markup.FromInterpolated($"[bold green]Found {identities.Count} package(s) to promote:[/]"));
        foreach (var identity in identities)
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
}