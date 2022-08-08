using NuGet.Packaging.Core;
using NuGet.Promoter.Commands.Promote;
using Spectre.Console;

namespace NuGet.Promoter.Promote;

public class PromotePackageLogger : IPromotePackageLogger
{
    public void LogResolvingMatchingPackages(IReadOnlyCollection<PackageDependency> dependencies)
    {
        var tree = new Tree("[bold green]Resolving matching packages for:[/]");
        foreach (var dep in dependencies)
        {
            tree.AddNode(Markup.FromInterpolated($"{dep.Id} {dep.VersionRange.PrettyPrint()}"));
        }

        AnsiConsole.Write(tree);
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