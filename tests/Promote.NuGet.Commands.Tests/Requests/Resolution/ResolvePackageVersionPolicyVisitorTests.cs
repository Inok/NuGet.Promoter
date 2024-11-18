using CSharpFunctionalExtensions;
using NuGet.Common;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using Promote.NuGet.Commands.Requests;
using Promote.NuGet.Commands.Requests.Resolution;
using Promote.NuGet.Feeds;

namespace Promote.NuGet.Commands.Tests.Requests.Resolution;

[TestFixture]
public class ResolvePackageVersionPolicyVisitorTests
{
    [Test]
    public async Task Visit_LatestPackageRequest_finds_greatest_released_package_version()
    {
        var packageId = "package-id";
        var versions = Result.Success<IReadOnlyCollection<NuGetVersion>>(
            new NuGetVersion[]
            {
                new(0, 1, 1),
                new(1, 0, 0),
                new(1, 0, 1, "pre.1"),
                new(1, 0, 1),
            });

        var nugetRepository = SetupRepository(packageId, versions);

        var sut = new ResolvePackageVersionPolicyVisitor(packageId, nugetRepository);

        var latestVersion = await sut.Visit(new LatestPackageVersionPolicy());

        latestVersion.IsSuccess.Should().BeTrue();
        latestVersion.Value.Should().BeEquivalentTo(new[] { new PackageIdentity(packageId, new NuGetVersion(1, 0, 1)) });
    }

    [Test]
    public async Task Visit_LatestPackageRequest_returns_error_when_there_are_no_released_packages()
    {
        var packageId = "package-id";
        var versions = Result.Success<IReadOnlyCollection<NuGetVersion>>(
            new NuGetVersion[]
            {
                new(0, 1, 0, "alpha.1"),
                new(0, 1, 0, "alpha.2"),
                new(1, 0, 0, "beta.1"),
            });

        var nugetRepository = SetupRepository(packageId, versions);

        var sut = new ResolvePackageVersionPolicyVisitor(packageId, nugetRepository);

        var latestVersion = await sut.Visit(new LatestPackageVersionPolicy());

        latestVersion.IsFailure.Should().BeTrue();
        latestVersion.Error.Should().Be("Package package-id has no released versions");
    }

    [Test]
    public async Task Visit_LatestPackageRequest_returns_error_when_package_not_found()
    {
        var packageId = "package-id";
        var versions = Result.Failure<IReadOnlyCollection<NuGetVersion>>("error-text");

        var nugetRepository = SetupRepository(packageId, versions);

        var sut = new ResolvePackageVersionPolicyVisitor(packageId, nugetRepository);

        var latestVersion = await sut.Visit(new LatestPackageVersionPolicy());

        latestVersion.IsFailure.Should().BeTrue();
        latestVersion.Error.Should().Be("error-text");
    }

    private static INuGetRepository SetupRepository(string packageId, Result<IReadOnlyCollection<NuGetVersion>> versions)
    {
        var packageInfoAccessor = Substitute.For<INuGetPackageInfoAccessor>();
        packageInfoAccessor.GetAllVersions(packageId, Arg.Any<CancellationToken>()).Returns(versions);

        if (versions.IsSuccess)
        {
            foreach (var version in versions.Value)
            {
                var identity = new PackageIdentity(packageId, version);

                var metadata = Substitute.For<IPackageSearchMetadata>();
                metadata.Identity.Returns(identity);
                metadata.IsListed.Returns(true);

                packageInfoAccessor.GetPackageMetadata(identity, Arg.Any<CancellationToken>()).Returns(Result.Success(metadata));
            }
        }

        var nugetRepository = Substitute.For<INuGetRepository>();
        nugetRepository.Packages.Returns(packageInfoAccessor);
        return nugetRepository;
    }
}
