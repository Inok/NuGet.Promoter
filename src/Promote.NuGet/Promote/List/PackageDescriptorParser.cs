using System.Text.RegularExpressions;
using CSharpFunctionalExtensions;
using NuGet.Versioning;
using Promote.NuGet.Commands.Requests;

namespace Promote.NuGet.Promote.List;

internal static class PackageDescriptorParser
{
    public static Result<VersionRangePackageRequest> ParseLine(string line)
    {
        var parseResult = TryParseInstallPackage(line)
                          .OnFailureCompensate(_ => TryParsePackageReference(line))
                          .OnFailureCompensate(_ => TryParseSpaceSeparated(line));

        if (parseResult.IsFailure)
        {
            return Result.Failure<VersionRangePackageRequest>($"Failed to parse '{line}'");
        }

        var (id, versionString) = parseResult.Value;

        if (NuGetVersion.TryParse(versionString, out var version))
        {
            return new VersionRangePackageRequest(id, new VersionRange(version, true, version, true));
        }

        if (VersionRange.TryParse(versionString, out var versionRange))
        {
            return new VersionRangePackageRequest(id, versionRange);
        }

        return Result.Failure<VersionRangePackageRequest>($"Cannot parse '{versionString}' as a version or version range");
    }

    private static Result<(string Id, string Version)> TryParseInstallPackage(string input)
    {
        var match = Regex.Match(input.Trim(), "^Install-Package\\s+(?<id>\\S+)\\s+-Version\\s+(?<version>\\S+)$");
        if (!match.Success)
        {
            return Result.Failure<(string Id, string Version)>("Failed to parse the specified string.");
        }

        var id = match.Groups["id"].Value;
        var version = match.Groups["version"].Value;

        return (id, version);
    }

    private static Result<(string Id, string Version)> TryParsePackageReference(string input)
    {
        var match = Regex.Match(input.Trim(), "^<PackageReference\\s+Include=\"(?<id>\\S+)\"\\s+Version=\"(?<version>\\S+)\"\\s+/>$");
        if (!match.Success)
        {
            return Result.Failure<(string Id, string Version)>("Failed to parse the specified string.");
        }

        var id = match.Groups["id"].Value;
        var version = match.Groups["version"].Value;

        return (id, version);
    }

    private static Result<(string Id, string Version)> TryParseSpaceSeparated(string input)
    {
        var match = Regex.Match(input.Trim(), "^(?<id>\\S+)\\s+(?<version>\\S+)$");
        if (!match.Success)
        {
            return Result.Failure<(string Id, string Version)>("Failed to parse the specified string.");
        }

        var id = match.Groups["id"].Value;
        var version = match.Groups["version"].Value;

        return (id, version);
    }
}
