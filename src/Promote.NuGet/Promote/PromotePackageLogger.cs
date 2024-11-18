using NuGet.Packaging.Core;
using Promote.NuGet.Commands.Licensing;
using Promote.NuGet.Commands.Promote;
using Promote.NuGet.Commands.Promote.Resolution;
using Promote.NuGet.Commands.Requests;
using Spectre.Console;

namespace Promote.NuGet.Promote;

public class PromotePackageLogger : IPromotePackageLogger
{
    private const int SingleLeftPaddingSize = 2;

    public void LogResolvingPackageRequests()
    {
        AnsiConsole.MarkupLineInterpolated($"[bold green]Resolving package requests...[/]");
    }

    public void LogResolvingPackageRequest(PackageRequest request)
    {
        AnsiConsole.MarkupLineInterpolated($"Resolving {request}");
    }

    public void LogPackageRequestResolution(PackageRequest request, IReadOnlyCollection<PackageIdentity> matchingPackages)
    {
        if (matchingPackages.Count == 0)
        {
            AnsiConsole.MarkupLineInterpolated($"Found 0 matching packages.");
            return;
        }

        var tree = new Tree(Markup.FromInterpolated($"Found {matchingPackages.Count} matching {Decl(matchingPackages.Count, "package", "packages")}:"));
        foreach (var identity in matchingPackages.OrderBy(x => x.Id).ThenBy(x => x.Version))
        {
            tree.AddNode(Markup.FromInterpolated($"{identity.Version}"));
        }

        AnsiConsole.Write(tree);
    }

    public void LogProcessingPackage(PackageIdentity identity)
    {
        AnsiConsole.MarkupLineInterpolated($"[bold]Processing {identity.Id} {identity.Version}[/]");
    }

    public void LogPackageLicense(PackageIdentity identity, PackageLicenseInfo license)
    {
        var text = Markup.FromInterpolated($"[gray]Package license: {license.PrettyPrint()}[/]");
        var padder = new Padder(text).Padding(left: SingleLeftPaddingSize, top: 0, right: 0, bottom: 0);
        AnsiConsole.Write(padder);
    }

    public void LogPackagePresentInDestination(PackageIdentity identity)
    {
        var text = Markup.FromInterpolated($"[gray]{identity.Id} {identity.Version} is already in the destination.[/]");
        var padder = new Padder(text).Padding(left: SingleLeftPaddingSize, top: 0, right: 0, bottom: 0);
        AnsiConsole.Write(padder);
    }

    public void LogPackageNotInDestination(PackageIdentity identity)
    {
        var text = Markup.FromInterpolated($"[gray]{identity.Id} {identity.Version} is not in the destination.[/]");
        var padder = new Padder(text).Padding(left: SingleLeftPaddingSize, top: 0, right: 0, bottom: 0);
        AnsiConsole.Write(padder);
    }

    public void LogNoDependencies(PackageIdentity identity)
    {
        var text = Markup.FromInterpolated($"[gray]{identity.Id} {identity.Version} has no dependencies.[/]");
        var padder = new Padder(text).Padding(left: SingleLeftPaddingSize, top: 0, right: 0, bottom: 0);
        AnsiConsole.Write(padder);
    }

    public void LogPackageDependenciesSkipped(PackageIdentity identity)
    {
        var text = Markup.FromInterpolated($"[gray]Skipping dependencies of {identity.Id} {identity.Version}.[/]");
        var padder = new Padder(text).Padding(left: SingleLeftPaddingSize, top: 0, right: 0, bottom: 0);
        AnsiConsole.Write(padder);
    }

    public void LogNoPackagesToPromote()
    {
        AnsiConsole.MarkupLineInterpolated($"[bold green]There are no packages to promote.[/]");
    }

    public void LogResolvingPackagesToPromote(IReadOnlyCollection<PackageIdentity> identities)
    {
        AnsiConsole.MarkupLineInterpolated($"[bold green]Resolving {identities.Count} {Decl(identities.Count, "package", "packages")} to promote...[/]");
    }

    public void LogResolvingDependency(PackageIdentity source, DependencyDescriptor dependency)
    {
        var text = Markup.FromInterpolated($"[gray]Resolving dependency {dependency.Identity.Id} {dependency.VersionRange.PrettyPrint()}[/]");
        var padder = new Padder(text).Padding(left: SingleLeftPaddingSize, top: 0, right: 0, bottom: 0);
        AnsiConsole.Write(padder);
    }

    public void LogResolvedDependency(PackageIdentity identity)
    {
        var text = Markup.FromInterpolated($"[gray]Resolved as {identity.Id} {identity.Version}[/]");
        var padder = new Padder(text).Padding(left: SingleLeftPaddingSize * 2, top: 0, right: 0, bottom: 0);
        AnsiConsole.Write(padder);
    }

    public void LogNewPackageQueuedForProcessing(PackageIdentity identity)
    {
        var text = Markup.FromInterpolated($"[gray]{identity.Id} {identity.Version} is queued for processing.[/]");
        var padder = new Padder(text).Padding(left: SingleLeftPaddingSize * 2, top: 0, right: 0, bottom: 0);
        AnsiConsole.Write(padder);
    }

    public void LogPackageIsAlreadyProcessedOrQueued(PackageIdentity identity)
    {
        var text = Markup.FromInterpolated($"[gray]{identity.Id} {identity.Version} is already processed or queued.[/]");
        var padder = new Padder(text).Padding(left: SingleLeftPaddingSize * 2, top: 0, right: 0, bottom: 0);
        AnsiConsole.Write(padder);
    }

    public void LogResolvedPackageTree(PackageResolutionTree packageTree)
    {
        var expanded = new HashSet<PackageIdentity>();

        var tree = new Tree("[bold green]Resolved package tree:[/]");
        foreach (var rootPackage in packageTree.Roots.OrderBy(x => x))
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
                node.AddNode(Markup.FromInterpolated($"[gray]+ {dependencies.Count} direct dependencies (expanded below)[/]"));
            }

            return;
        }

        var expandedBefore = !expanded.Add(package);
        if (expandedBefore)
        {
            if (dependencies.Count > 0)
            {
                node.AddNode(Markup.FromInterpolated($"[gray]+ {dependencies.Count} direct dependencies (expanded above)[/]"));
            }

            return;
        }

        foreach (var child in dependencies.OrderBy(x => x))
        {
            AddNodeAndChildren(node, packageTree, child, expanded, rootLevel: false);
        }
    }

    public void LogPackagesToPromote(IReadOnlyCollection<PackageInfo> packages)
    {
        var tree = new Tree(Markup.FromInterpolated($"[bold green]Found {packages.Count} {Decl(packages.Count, "package", "packages")} to promote:[/]"));
        foreach (var identity in packages.OrderBy(x => x.Id))
        {
            var node = tree.AddNode(Markup.FromInterpolated($"{identity.Id.Id} {identity.Id.Version}"));
            node.AddNode(Markup.FromInterpolated($"License: {identity.License.PrettyPrint()}"));
        }

        AnsiConsole.Write(tree);
    }

    public void LogLicenseSummary(IReadOnlyCollection<PackageInfo> packages)
    {
        var licenseItems = packages.GroupBy(x => x.License)
                                   .Select(x => (License: x.Key, Count: x.Count()))
                                   .OrderByDescending(x => x.Count)
                                   .ThenBy(x => x.License.License);

        var tree = new Tree(Markup.FromInterpolated($"[green]License summary:[/]"));
        foreach (var item in licenseItems)
        {
            tree.AddNode(Markup.FromInterpolated($"{item.Count}x: {item.License.PrettyPrint()}"));
        }

        AnsiConsole.Write(tree);
    }

    public void LogDryRun()
    {
        AnsiConsole.MarkupLineInterpolated($"[bold green]Packages won't be promoted in dry run mode.[/]");
    }

    public void LogStartMirroringPackagesCount(int count)
    {
        AnsiConsole.MarkupLineInterpolated($"[bold green]Promoting {count} {Decl(count, "package", "packages")}...[/]");
    }

    public void LogMirrorPackage(PackageIdentity identity, int current, int total)
    {
        AnsiConsole.MarkupLineInterpolated($"[bold green]({current}/{total}) Promote {identity.Id} {identity.Version}[/]");
    }

    public void LogMirroredPackagesCount(int count)
    {
        AnsiConsole.MarkupLineInterpolated($"[bold green]{count} {Decl(count, "package", "packages")} promoted.[/]");
    }

    public void LogCheckingLicenseCompliance()
    {
        AnsiConsole.MarkupLineInterpolated($"[bold green]Checking license compliance...[/]");
    }

    public void LogComplianceChecksDisabled()
    {
        AnsiConsole.MarkupLineInterpolated($"[yellow]License compliance checks are disabled.[/]");
    }

    public void LogLicenseViolationsSummary(IReadOnlyCollection<LicenseComplianceViolation> violations)
    {
        var tree = new Tree(Markup.FromInterpolated($"[red]{violations.Count} license {Decl(violations.Count, "violation", "violations")} found:[/]"));

        foreach (var violation in violations.OrderBy(x => x.PackageId))
        {
            var node = tree.AddNode(Markup.FromInterpolated($"[red]{violation.PackageId}[/]"));
            node.AddNode(Markup.FromInterpolated($"[red]License ({violation.LicenseType.ToString().ToLowerInvariant()}): {violation.License}[/]"));
            node.AddNode(Markup.FromInterpolated($"[red]Reason: {violation.Explanation}[/]"));
        }

        AnsiConsole.Write(tree);
    }

    public void LogNoLicenseViolations()
    {
        AnsiConsole.MarkupLineInterpolated($"[green]No license violations found.[/]");
    }

    public void LogCheckingLicenseComplianceForPackage(PackageIdentity identity)
    {
        AnsiConsole.MarkupLineInterpolated($"Checking {identity.Id} {identity.Version}");
    }

    public void LogFailedToDownloadPackage(PackageIdentity identity)
    {
        var text = Markup.FromInterpolated($"[red]Failed to download package {identity.Id} {identity.Version}[/]");
        var padder = new Padder(text).Padding(left: SingleLeftPaddingSize * 2, top: 0, right: 0, bottom: 0);
        AnsiConsole.Write(padder);
    }

    public void LogPackageLicense(PackageLicenseType licenseType, string license, IReadOnlyList<string>? warningsAndErrors)
    {
        var text = Markup.FromInterpolated($"[gray]License ({licenseType.ToString().ToLowerInvariant()}): {license}[/]");
        var padder = new Padder(text).Padding(left: SingleLeftPaddingSize * 2, top: 0, right: 0, bottom: 0);
        AnsiConsole.Write(padder);

        if (warningsAndErrors?.Count > 0)
        {
            var tree = new Tree(Markup.FromInterpolated($"[yellow]License warnings and errors:[/]"));
            foreach (var item in warningsAndErrors)
            {
                tree.AddNode(Markup.FromInterpolated($"{item}"));
            }

            var treePadder = new Padder(tree).Padding(left: SingleLeftPaddingSize * 2, top: 0, right: 0, bottom: 0);
            AnsiConsole.Write(treePadder);
        }
    }

    public void LogLicenseCompliance(string reason)
    {
        var text = Markup.FromInterpolated($"[gray][[v]] {reason}[/]");
        var padder = new Padder(text).Padding(left: SingleLeftPaddingSize * 2, top: 0, right: 0, bottom: 0);
        AnsiConsole.Write(padder);
    }

    public void LogLicenseViolation(LicenseComplianceViolation violation)
    {
        var text = Markup.FromInterpolated($"[red][[x]] {violation.Explanation}[/]");
        var padder = new Padder(text).Padding(left: SingleLeftPaddingSize * 2, top: 0, right: 0, bottom: 0);
        AnsiConsole.Write(padder);
    }

    public void LogAcceptedLicenseFileReadFailure(string acceptedFilePath, Exception exception)
    {
        var text = Markup.FromInterpolated($"[red]Failed to read accepted file {acceptedFilePath}: {exception.Message}[/]");
        var padder = new Padder(text).Padding(left: SingleLeftPaddingSize * 2, top: 0, right: 0, bottom: 0);
        AnsiConsole.Write(padder);

    }

    private static string Decl(int count, string one, string many)
    {
        return count == 1 ? one : many;
    }
}
