using NuGet.Packaging.Core;
using Promote.NuGet.Commands.Licensing;
using Promote.NuGet.Commands.Mirroring;
using Promote.NuGet.Commands.Promote;
using Promote.NuGet.Commands.Promote.Resolution;
using Promote.NuGet.Commands.Requests;
using Promote.NuGet.Commands.Requests.Resolution;
using Spectre.Console;

namespace Promote.NuGet.Promote;

public sealed class PromotePackageLogger(bool verbose) : IPromotePackageLogger
{
    private const int SingleLeftPaddingSize = 2;

    void IPackageRequestResolverLogger.LogResolvingPackageRequests()
    {
        AnsiConsole.MarkupLineInterpolated($"[bold green]Resolving package requests...[/]");
    }

    void IPackageRequestResolverLogger.LogResolvingPackageRequest(PackageRequest request)
    {
        AnsiConsole.MarkupLineInterpolated($"[bold]Resolving {request}[/]");
    }

    void IPackageRequestResolverLogger.LogPackageRequestResolution(PackageRequest request, IReadOnlyCollection<PackageIdentity> matchingPackages)
    {
        if (!verbose)
        {
            if (matchingPackages.Count == 0)
            {
                AnsiConsole.MarkupLineInterpolated($"[gray]Found 0 matching versions.[/]");
                return;
            }

            var versions = matchingPackages.Select(x => x.Version).Order();
            var versionsText = string.Join(", ", versions);
            AnsiConsole.MarkupLineInterpolated($"[gray]Found {matchingPackages.Count} matching {Decl(matchingPackages.Count, "version", "versions")}: {versionsText}[/]");
        }
        else
        {
            if (matchingPackages.Count == 0)
            {
                AnsiConsole.MarkupLineInterpolated($"Found 0 matching versions.");
                return;
            }

            var tree = new Tree(Markup.FromInterpolated($"Found {matchingPackages.Count} matching {Decl(matchingPackages.Count, "version", "versions")}:"));
            foreach (var identity in matchingPackages.OrderBy(x => x.Version))
            {
                tree.AddNode(Markup.FromInterpolated($"{identity.Version}"));
            }
            AnsiConsole.Write(tree);
        }
    }

    void IPackagesToPromoteResolverLogger.LogResolvingPackagesToPromote(IReadOnlyCollection<PackageIdentity> identities)
    {
        AnsiConsole.MarkupLineInterpolated($"[bold green]Resolving {identities.Count} {Decl(identities.Count, "package", "packages")} to promote...[/]");
    }

    void IPackagesToPromoteResolverLogger.LogProcessingPackage(PackageIdentity identity)
    {
        AnsiConsole.MarkupLineInterpolated($"[bold]Processing {identity.Id} {identity.Version}[/]");
    }

    void IPackagesToPromoteResolverLogger.LogPackageLicense(PackageIdentity identity, PackageLicenseInfo license)
    {
        if (!verbose)
        {
            return;
        }

        var text = Markup.FromInterpolated($"[gray]Package license: {license.PrettyPrint()}[/]");
        var padder = new Padder(text).Padding(left: SingleLeftPaddingSize, top: 0, right: 0, bottom: 0);
        AnsiConsole.Write(padder);
    }

    void IPackagesToPromoteResolverLogger.LogPackagePresentInDestination(PackageIdentity identity)
    {
        if (!verbose)
        {
            return;
        }

        var text = Markup.FromInterpolated($"[gray]The package is already in the destination.[/]");
        var padder = new Padder(text).Padding(left: SingleLeftPaddingSize, top: 0, right: 0, bottom: 0);
        AnsiConsole.Write(padder);
    }

    void IPackagesToPromoteResolverLogger.LogPackageNotInDestination(PackageIdentity identity)
    {
        if (!verbose)
        {
            return;
        }

        var text = Markup.FromInterpolated($"[gray]The package is not in the destination.[/]");
        var padder = new Padder(text).Padding(left: SingleLeftPaddingSize, top: 0, right: 0, bottom: 0);
        AnsiConsole.Write(padder);
    }

    void IPackagesToPromoteResolverLogger.LogNoDependencies(PackageIdentity identity)
    {
        if (!verbose)
        {
            return;
        }

        var text = Markup.FromInterpolated($"[gray]The package has no dependencies.[/]");
        var padder = new Padder(text).Padding(left: SingleLeftPaddingSize, top: 0, right: 0, bottom: 0);
        AnsiConsole.Write(padder);
    }

    void IPackagesToPromoteResolverLogger.LogPackageDependenciesSkipped(PackageIdentity identity)
    {
        if (!verbose)
        {
            return;
        }

        var text = Markup.FromInterpolated($"[gray]Dependencies are skipped.[/]");
        var padder = new Padder(text).Padding(left: SingleLeftPaddingSize, top: 0, right: 0, bottom: 0);
        AnsiConsole.Write(padder);
    }

    void IPackagesToPromoteResolverLogger.LogResolvingDependency(PackageIdentity source, DependencyDescriptor dependency)
    {
        var text = Markup.FromInterpolated($"[gray]Resolving dependency {dependency.Identity.Id} {dependency.VersionRange.PrettyPrint()}[/]");
        var padder = new Padder(text).Padding(left: SingleLeftPaddingSize, top: 0, right: 0, bottom: 0);
        AnsiConsole.Write(padder);
    }

    void IPackagesToPromoteResolverLogger.LogResolvedDependency(PackageIdentity identity, bool enqueuedForProcessing)
    {
        var versionText = Markup.FromInterpolated($"[gray]Resolved version: {identity.Version}[/]");
        var versionPadder = new Padder(versionText).Padding(left: SingleLeftPaddingSize * 2, top: 0, right: 0, bottom: 0);
        AnsiConsole.Write(versionPadder);

        if (verbose)
        {
            var statusText = Markup.FromInterpolated($"[gray]Status: {(enqueuedForProcessing ? "enqueued for processing" : "already enqueued or processed")}.[/]");
            var statusPadder = new Padder(statusText).Padding(left: SingleLeftPaddingSize * 2, top: 0, right: 0, bottom: 0);
            AnsiConsole.Write(statusPadder);
        }
    }

    void IPackagesToPromoteResolverLogger.LogResolvedPackageTree(PackageResolutionTree packageTree)
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

    void IPromotePackageLogger.LogPackagesToPromote(IReadOnlyCollection<PackageInfo> packages)
    {
        var tree = new Tree(Markup.FromInterpolated($"[bold green]Found {packages.Count} {Decl(packages.Count, "package", "packages")} to promote:[/]"));
        foreach (var identity in packages.OrderBy(x => x.Id))
        {
            var content = Markup.FromInterpolated(
                $"""
                 {identity.Id.Id} {identity.Id.Version}
                 [gray]License: {identity.License.PrettyPrint()}[/]
                 """
            );
            tree.AddNode(content);
        }

        AnsiConsole.Write(tree);
    }

    void IPromotePackageLogger.LogLicenseSummary(IReadOnlyCollection<PackageInfo> packages)
    {
        var licenseItems = packages.GroupBy(x => x.License)
                                   .Select(x => (License: x.Key, Count: x.Count()))
                                   .OrderByDescending(x => x.Count)
                                   .ThenBy(x => x.License.License);

        var tree = new Tree(Markup.FromInterpolated($"[bold green]License summary:[/]"));
        foreach (var item in licenseItems)
        {
            tree.AddNode(Markup.FromInterpolated($"{item.Count}x: {item.License.PrettyPrint()}"));
        }

        AnsiConsole.Write(tree);
    }

    void IPromotePackageLogger.LogNoPackagesToPromote()
    {
        AnsiConsole.MarkupLineInterpolated($"[bold green]There are no packages to promote.[/]");
    }

    void IPromotePackageLogger.LogDryRun()
    {
        AnsiConsole.MarkupLineInterpolated($"[bold green]Packages won't be promoted in dry run mode.[/]");
    }

    void IPackageMirroringExecutorLogger.LogStartMirroringPackagesCount(int count)
    {
        AnsiConsole.MarkupLineInterpolated($"[bold green]Promoting {count} {Decl(count, "package", "packages")}...[/]");
    }

    void IPackageMirroringExecutorLogger.LogMirrorPackage(PackageIdentity identity, int current, int total)
    {
        AnsiConsole.MarkupLineInterpolated($"[bold]({current}/{total}) Promote {identity.Id} {identity.Version}[/]");
    }

    void IPackageMirroringExecutorLogger.LogMirroredPackagesCount(int count)
    {
        AnsiConsole.MarkupLineInterpolated($"[bold green]{count} {Decl(count, "package", "packages")} promoted.[/]");
    }

    void ILicenseComplianceValidatorLogger.LogCheckingLicenseCompliance()
    {
        AnsiConsole.MarkupLineInterpolated($"[bold green]Checking license compliance...[/]");
    }

    void ILicenseComplianceValidatorLogger.LogComplianceChecksDisabled()
    {
        AnsiConsole.MarkupLineInterpolated($"[yellow]License compliance checks are disabled.[/]");
    }

    void ILicenseComplianceValidatorLogger.LogLicenseViolationsSummary(IReadOnlyCollection<LicenseComplianceViolation> violations)
    {
        var tree = new Tree(Markup.FromInterpolated($"[red]{violations.Count} license {Decl(violations.Count, "violation", "violations")} found:[/]"));

        foreach (var violation in violations.OrderBy(x => x.PackageId))
        {
            var content = Markup.FromInterpolated(
                $"""
                 [red]{violation.PackageId}[/]
                 [red]License ({violation.LicenseType.ToString().ToLowerInvariant()}): {violation.License}[/]
                 [red]Reason: {violation.Explanation}[/]
                 """
            );
            tree.AddNode(content);
        }

        AnsiConsole.Write(tree);
    }

    void ILicenseComplianceValidatorLogger.LogNoLicenseViolations()
    {
        AnsiConsole.MarkupLineInterpolated($"[green]No license violations found.[/]");
    }

    void ILicenseComplianceValidatorLogger.LogCheckingLicenseComplianceForPackage(PackageIdentity identity)
    {
        AnsiConsole.MarkupLineInterpolated($"Checking {identity.Id} {identity.Version}");
    }

    void ILicenseComplianceValidatorLogger.LogFailedToDownloadPackage(PackageIdentity identity)
    {
        var text = Markup.FromInterpolated($"[red]Failed to download package {identity.Id} {identity.Version}[/]");
        var padder = new Padder(text).Padding(left: SingleLeftPaddingSize, top: 0, right: 0, bottom: 0);
        AnsiConsole.Write(padder);
    }

    void ILicenseComplianceValidatorLogger.LogPackageLicense(PackageLicenseType licenseType, string license, IReadOnlyList<string>? warningsAndErrors)
    {
        var text = Markup.FromInterpolated($"[gray]License ({licenseType.ToString().ToLowerInvariant()}): {license}[/]");
        var padder = new Padder(text).Padding(left: SingleLeftPaddingSize, top: 0, right: 0, bottom: 0);
        AnsiConsole.Write(padder);

        if (warningsAndErrors?.Count > 0)
        {
            var tree = new Tree(Markup.FromInterpolated($"[yellow]License warnings and errors:[/]"));
            foreach (var item in warningsAndErrors)
            {
                tree.AddNode(Markup.FromInterpolated($"{item}"));
            }

            var treePadder = new Padder(tree).Padding(left: SingleLeftPaddingSize, top: 0, right: 0, bottom: 0);
            AnsiConsole.Write(treePadder);
        }
    }

    void ILicenseComplianceValidatorLogger.LogLicenseCompliance(string reason)
    {
        var text = Markup.FromInterpolated($"[gray][[v]] {reason}[/]");
        var padder = new Padder(text).Padding(left: SingleLeftPaddingSize, top: 0, right: 0, bottom: 0);
        AnsiConsole.Write(padder);
    }

    void ILicenseComplianceValidatorLogger.LogLicenseViolation(LicenseComplianceViolation violation)
    {
        var text = Markup.FromInterpolated($"[red][[x]] {violation.Explanation}[/]");
        var padder = new Padder(text).Padding(left: SingleLeftPaddingSize, top: 0, right: 0, bottom: 0);
        AnsiConsole.Write(padder);
    }

    void ILicenseComplianceValidatorLogger.LogAcceptedLicenseFileReadFailure(string acceptedFilePath, Exception exception)
    {
        var text = Markup.FromInterpolated($"[red]Failed to read accepted file {acceptedFilePath}: {exception.Message}[/]");
        var padder = new Padder(text).Padding(left: SingleLeftPaddingSize, top: 0, right: 0, bottom: 0);
        AnsiConsole.Write(padder);

    }

    private static string Decl(int count, string one, string many)
    {
        return count == 1 ? one : many;
    }
}
