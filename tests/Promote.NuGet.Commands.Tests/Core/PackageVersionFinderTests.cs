using CSharpFunctionalExtensions;
using FluentAssertions;
using Moq;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using NUnit.Framework;
using Promote.NuGet.Commands.Core;
using Promote.NuGet.Feeds;

namespace Promote.NuGet.Commands.Tests.Core;

[TestFixture]
public class PackageVersionFinderTests
{
    [Test]
    public async Task FindLatestVersion_finds_greatest_released_package_version()
    {
        var packageId = "package-id";
        var versions = Result.Success<IReadOnlyCollection<NuGetVersion>, string>(
            new NuGetVersion[]
            {
                new(0, 1, 1),
                new(1, 0, 0),
                new(1, 0, 1, "pre.1"),
                new(1, 0, 1),
            });

        var nugetRepository = SetupRepository(packageId, versions);

        var sut = new PackageVersionFinder(nugetRepository);

        var latestVersion = await sut.FindLatestVersion(packageId);

        latestVersion.IsSuccess.Should().BeTrue();
        latestVersion.Value.Should().Be(new PackageIdentity(packageId, new NuGetVersion(1, 0, 1)));
    }

    [Test]
    public async Task FindLatestVersion_returns_error_when_there_are_no_released_packages()
    {
        var packageId = "package-id";
        var versions = Result.Success<IReadOnlyCollection<NuGetVersion>, string>(
            new NuGetVersion[]
            {
                new(0, 1, 0, "alpha.1"),
                new(0, 1, 0, "alpha.2"),
                new(1, 0, 0, "beta.1"),
            });

        var nugetRepository = SetupRepository(packageId, versions);

        var sut = new PackageVersionFinder(nugetRepository);

        var latestVersion = await sut.FindLatestVersion(packageId);

        latestVersion.IsFailure.Should().BeTrue();
        latestVersion.Error.Should().Be($"Package package-id has no released versions");
    }

    [Test]
    public async Task FindLatestVersion_returns_error_when_package_not_found()
    {
        var packageId = "package-id";
        var versions = Result.Failure<IReadOnlyCollection<NuGetVersion>, string>("error-text");

        var nugetRepository = SetupRepository(packageId, versions);

        var sut = new PackageVersionFinder(nugetRepository);

        var latestVersion = await sut.FindLatestVersion(packageId);

        latestVersion.IsFailure.Should().BeTrue();
        latestVersion.Error.Should().Be("error-text");
    }

    private static INuGetRepository SetupRepository(string packageId, Result<IReadOnlyCollection<NuGetVersion>, string> versions)
    {
        var packageInfoAccessor = new Mock<INuGetPackageInfoAccessor>();
        packageInfoAccessor.Setup(x => x.GetAllVersions(packageId, It.IsAny<CancellationToken>()))
                           .ReturnsAsync(versions);

        var nugetRepository = Mock.Of<INuGetRepository>(x => x.Packages == packageInfoAccessor.Object);
        return nugetRepository;
    }
}