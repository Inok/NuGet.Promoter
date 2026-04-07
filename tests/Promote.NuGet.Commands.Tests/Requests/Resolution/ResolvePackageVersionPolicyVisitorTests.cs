using CSharpFunctionalExtensions;
using Microsoft.Extensions.Time.Testing;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using Promote.NuGet.Commands.Requests;
using Promote.NuGet.Commands.Requests.Resolution;
using Promote.NuGet.Feeds;

namespace Promote.NuGet.Commands.Tests.Requests.Resolution;

[TestFixture]
public class ResolvePackageVersionPolicyVisitorTests
{
    private static readonly DateTimeOffset OldPublishDate = new(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Test, CancelAfter(60_000)]
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

        var sut = CreateVisitor(packageId, nugetRepository);

        var latestVersion = await sut.Visit(new LatestPackageVersionPolicy());

        latestVersion.IsSuccess.Should().BeTrue();
        latestVersion.Value.Should().BeEquivalentTo(new[] { new PackageIdentity(packageId, new NuGetVersion(1, 0, 1)) });
    }

    [Test, CancelAfter(60_000)]
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

        var sut = CreateVisitor(packageId, nugetRepository);

        var latestVersion = await sut.Visit(new LatestPackageVersionPolicy());

        latestVersion.IsFailure.Should().BeTrue();
        latestVersion.Error.Should().Be("Package package-id has no released versions");
    }

    [Test, CancelAfter(60_000)]
    public async Task Visit_LatestPackageRequest_returns_error_when_package_not_found()
    {
        var packageId = "package-id";
        var versions = Result.Failure<IReadOnlyCollection<NuGetVersion>>("error-text");

        var nugetRepository = SetupRepository(packageId, versions);

        var sut = CreateVisitor(packageId, nugetRepository);

        var latestVersion = await sut.Visit(new LatestPackageVersionPolicy());

        latestVersion.IsFailure.Should().BeTrue();
        latestVersion.Error.Should().Be("error-text");
    }

    [Test, CancelAfter(60_000)]
    public async Task Visit_LatestPackageRequest_skips_too_new_version_and_falls_back()
    {
        var packageId = "package-id";
        var now = new DateTimeOffset(2025, 6, 15, 0, 0, 0, TimeSpan.Zero);
        var oldDate = now - TimeSpan.FromDays(10);
        var newDate = now - TimeSpan.FromDays(1);

        var versions = Result.Success<IReadOnlyCollection<NuGetVersion>>(
            new NuGetVersion[]
            {
                new(1, 0, 0),
                new(2, 0, 0),
            });

        var nugetRepository = SetupRepository(packageId, versions, version =>
            version == new NuGetVersion(2, 0, 0) ? newDate : oldDate);

        var timeProvider = new FakeTimeProvider(now);
        var sut = CreateVisitor(packageId, nugetRepository, TimeSpan.FromDays(7), timeProvider);

        // Act
        var result = await sut.Visit(new LatestPackageVersionPolicy());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(new[] { new PackageIdentity(packageId, new NuGetVersion(1, 0, 0)) });
    }

    [Test, CancelAfter(60_000)]
    public async Task Visit_LatestPackageRequest_returns_error_when_all_versions_too_new()
    {
        var packageId = "package-id";
        var now = new DateTimeOffset(2025, 6, 15, 0, 0, 0, TimeSpan.Zero);
        var newDate = now - TimeSpan.FromDays(1);

        var versions = Result.Success<IReadOnlyCollection<NuGetVersion>>(
            new NuGetVersion[]
            {
                new(1, 0, 0),
                new(2, 0, 0),
            });

        var nugetRepository = SetupRepository(packageId, versions, _ => newDate);

        var timeProvider = new FakeTimeProvider(now);
        var sut = CreateVisitor(packageId, nugetRepository, TimeSpan.FromDays(7), timeProvider);

        // Act
        var result = await sut.Visit(new LatestPackageVersionPolicy());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("no released versions");
    }

    [Test, CancelAfter(60_000)]
    public async Task Visit_VersionRangePackageVersionPolicy_filters_too_new_versions()
    {
        var packageId = "package-id";
        var now = new DateTimeOffset(2025, 6, 15, 0, 0, 0, TimeSpan.Zero);
        var oldDate = now - TimeSpan.FromDays(10);
        var newDate = now - TimeSpan.FromDays(1);

        var versions = Result.Success<IReadOnlyCollection<NuGetVersion>>(
            new NuGetVersion[]
            {
                new(1, 0, 0),
                new(1, 1, 0),
                new(1, 2, 0),
            });

        var nugetRepository = SetupRepository(packageId, versions, version =>
            version == new NuGetVersion(1, 2, 0) ? newDate : oldDate);

        var timeProvider = new FakeTimeProvider(now);
        var versionRange = VersionRange.Parse("[1.0.0, 2.0.0)");
        var sut = CreateVisitor(packageId, nugetRepository, TimeSpan.FromDays(7), timeProvider);

        // Act
        var result = await sut.Visit(new VersionRangePackageVersionPolicy(versionRange));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(new[]
        {
            new PackageIdentity(packageId, new NuGetVersion(1, 0, 0)),
            new PackageIdentity(packageId, new NuGetVersion(1, 1, 0)),
        });
    }

    [Test, CancelAfter(60_000)]
    public async Task Visit_ExactPackageVersionPolicy_ignores_release_age_filter()
    {
        var packageId = "package-id";
        var now = new DateTimeOffset(2025, 6, 15, 0, 0, 0, TimeSpan.Zero);
        var newDate = now - TimeSpan.FromDays(1);
        var version = new NuGetVersion(1, 0, 0);

        var nugetRepository = SetupRepository(packageId,
            Result.Success<IReadOnlyCollection<NuGetVersion>>(new[] { version }),
            _ => newDate);

        var timeProvider = new FakeTimeProvider(now);
        var sut = CreateVisitor(packageId, nugetRepository, TimeSpan.FromDays(7), timeProvider);

        // Act
        var result = await sut.Visit(new ExactPackageVersionPolicy(version));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(new[] { new PackageIdentity(packageId, version) });
    }

    [Test, CancelAfter(60_000)]
    public async Task Visit_LatestPackageRequest_returns_error_when_published_date_is_null()
    {
        var packageId = "package-id";
        var now = new DateTimeOffset(2025, 6, 15, 0, 0, 0, TimeSpan.Zero);

        var versions = Result.Success<IReadOnlyCollection<NuGetVersion>>(
            new NuGetVersion[] { new(1, 0, 0) });

        var nugetRepository = SetupRepository(packageId, versions, _ => null);

        var timeProvider = new FakeTimeProvider(now);
        var sut = CreateVisitor(packageId, nugetRepository, TimeSpan.FromDays(7), timeProvider);

        // Act
        var result = await sut.Visit(new LatestPackageVersionPolicy());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("no publish date");
    }

    private static ResolvePackageVersionPolicyVisitor CreateVisitor(
        string packageId,
        INuGetRepository repository,
        TimeSpan? minimumReleaseAge = null,
        TimeProvider? timeProvider = null)
    {
        var logger = Substitute.For<IPackageRequestResolverLogger>();
        return new ResolvePackageVersionPolicyVisitor(packageId, repository, logger, minimumReleaseAge, timeProvider ?? TimeProvider.System);
    }

    private static INuGetRepository SetupRepository(
        string packageId,
        Result<IReadOnlyCollection<NuGetVersion>> versions,
        Func<NuGetVersion, DateTimeOffset?>? publishedDateProvider = null)
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
                metadata.Published.Returns(publishedDateProvider is not null ? publishedDateProvider(version) : OldPublishDate);

                packageInfoAccessor.GetPackageMetadata(identity, Arg.Any<CancellationToken>()).Returns(Result.Success(metadata));
            }
        }

        var nugetRepository = Substitute.For<INuGetRepository>();
        nugetRepository.Packages.Returns(packageInfoAccessor);
        return nugetRepository;
    }
}
