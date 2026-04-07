using CSharpFunctionalExtensions;
using Microsoft.Extensions.Time.Testing;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using Promote.NuGet.Commands.Promote;
using Promote.NuGet.Commands.Promote.Resolution;
using Promote.NuGet.Feeds;

namespace Promote.NuGet.Commands.Tests.Promote.Resolution;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class PackagesToPromoteResolverReleaseAgeTests
{
    private static readonly DateTimeOffset Now = new(2025, 6, 15, 0, 0, 0, TimeSpan.Zero);

    [Test, CancelAfter(60_000)]
    public async Task Dependency_selects_different_version_when_best_match_is_too_new()
    {
        // FindBestMatch picks the lowest satisfying version (1.0.0).
        // 1.0.0 is too new → filtered out → falls back to 2.0.0 which is old enough.
        var rootId = new PackageIdentity("Root", new NuGetVersion(1, 0, 0));
        var depId = "Dependency";
        var depV1 = new NuGetVersion(1, 0, 0);
        var depV2 = new NuGetVersion(2, 0, 0);

        var sourceRepo = CreateSourceRepository(rootId, depId, [depV1, depV2],
            v => v == depV1 ? Now - TimeSpan.FromDays(1) : Now - TimeSpan.FromDays(10));

        var destRepo = CreateEmptyDestinationRepository();
        var logger = Substitute.For<IPackagesToPromoteResolverLogger>();
        var timeProvider = new FakeTimeProvider(Now);

        var sut = new PackagesToPromoteResolver(sourceRepo, destRepo, logger, TimeSpan.FromDays(7), timeProvider);

        // Act
        var result = await sut.ResolvePackageTree(
            new HashSet<PackageIdentity> { rootId },
            new PromotePackageCommandOptions(dryRun: false, alwaysResolveDeps: false, forcePush: false),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var depPackages = result.Value.AllPackages.Where(p => p.Id.Id == depId).ToList();
        depPackages.Should().ContainSingle().Which.Id.Version.Should().Be(depV2);

        logger.Received().LogDependencyResolvedToOlderVersionDueToAge(rootId, depId, depV1, depV2);
    }

    [Test, CancelAfter(60_000)]
    public async Task Dependency_fails_when_no_version_meets_age_requirement()
    {
        var rootId = new PackageIdentity("Root", new NuGetVersion(1, 0, 0));
        var depId = "Dependency";
        var depV1 = new NuGetVersion(1, 0, 0);

        var sourceRepo = CreateSourceRepository(rootId, depId, [depV1],
            _ => Now - TimeSpan.FromDays(1));

        var destRepo = CreateEmptyDestinationRepository();
        var logger = Substitute.For<IPackagesToPromoteResolverLogger>();
        var timeProvider = new FakeTimeProvider(Now);

        var sut = new PackagesToPromoteResolver(sourceRepo, destRepo, logger, TimeSpan.FromDays(7), timeProvider);

        // Act
        var result = await sut.ResolvePackageTree(
            new HashSet<PackageIdentity> { rootId },
            new PromotePackageCommandOptions(dryRun: false, alwaysResolveDeps: false, forcePush: false),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("no version satisfying");
    }

    [Test, CancelAfter(60_000)]
    public async Task Dependency_uses_best_match_when_filter_is_null()
    {
        var rootId = new PackageIdentity("Root", new NuGetVersion(1, 0, 0));
        var depId = "Dependency";
        var depV1 = new NuGetVersion(1, 0, 0);
        var depV2 = new NuGetVersion(2, 0, 0);

        var sourceRepo = CreateSourceRepository(rootId, depId, [depV1, depV2],
            _ => Now - TimeSpan.FromDays(1));

        var destRepo = CreateEmptyDestinationRepository();
        var logger = Substitute.For<IPackagesToPromoteResolverLogger>();

        var sut = new PackagesToPromoteResolver(sourceRepo, destRepo, logger, minimumReleaseAge: null, TimeProvider.System);

        // Act
        var result = await sut.ResolvePackageTree(
            new HashSet<PackageIdentity> { rootId },
            new PromotePackageCommandOptions(dryRun: false, alwaysResolveDeps: false, forcePush: false),
            CancellationToken.None);

        // Assert — FindBestMatch picks the lowest satisfying version
        result.IsSuccess.Should().BeTrue();
        var depPackages = result.Value.AllPackages.Where(p => p.Id.Id == depId).ToList();
        depPackages.Should().ContainSingle().Which.Id.Version.Should().Be(depV1);
    }

    [Test, CancelAfter(60_000)]
    public async Task Dependency_fails_when_published_date_is_null()
    {
        var rootId = new PackageIdentity("Root", new NuGetVersion(1, 0, 0));
        var depId = "Dependency";
        var depV1 = new NuGetVersion(1, 0, 0);

        var sourceRepo = CreateSourceRepository(rootId, depId, [depV1], _ => null);

        var destRepo = CreateEmptyDestinationRepository();
        var logger = Substitute.For<IPackagesToPromoteResolverLogger>();
        var timeProvider = new FakeTimeProvider(Now);

        var sut = new PackagesToPromoteResolver(sourceRepo, destRepo, logger, TimeSpan.FromDays(7), timeProvider);

        // Act
        var result = await sut.ResolvePackageTree(
            new HashSet<PackageIdentity> { rootId },
            new PromotePackageCommandOptions(dryRun: false, alwaysResolveDeps: false, forcePush: false),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("no publish date");
    }

    private static INuGetRepository CreateSourceRepository(
        PackageIdentity rootId,
        string depId,
        NuGetVersion[] depVersions,
        Func<NuGetVersion, DateTimeOffset?> depPublishedDateProvider)
    {
        var accessor = Substitute.For<INuGetPackageInfoAccessor>();

        // Root package metadata with a dependency
        var rootMetadata = Substitute.For<IPackageSearchMetadata>();
        rootMetadata.Identity.Returns(rootId);
        rootMetadata.IsListed.Returns(true);
        rootMetadata.Published.Returns(Now - TimeSpan.FromDays(30));
        rootMetadata.LicenseMetadata.Returns((global::NuGet.Packaging.LicenseMetadata?)null);
        rootMetadata.LicenseUrl.Returns((Uri?)null);

        var depGroup = new global::NuGet.Packaging.PackageDependencyGroup(
            global::NuGet.Frameworks.NuGetFramework.AnyFramework,
            [new global::NuGet.Packaging.Core.PackageDependency(depId, VersionRange.Parse("[1.0.0,)"))]);
        rootMetadata.DependencySets.Returns(new[] { depGroup });

        accessor.GetPackageMetadata(rootId, Arg.Any<CancellationToken>())
                .Returns(Result.Success(rootMetadata));

        // Dependency versions
        accessor.GetAllVersions(depId, Arg.Any<CancellationToken>())
                .Returns(Result.Success<IReadOnlyCollection<NuGetVersion>>(depVersions));

        // Dependency metadata per version
        foreach (var v in depVersions)
        {
            var depIdentity = new PackageIdentity(depId, v);
            var depMeta = Substitute.For<IPackageSearchMetadata>();
            depMeta.Identity.Returns(depIdentity);
            depMeta.IsListed.Returns(true);
            depMeta.Published.Returns(depPublishedDateProvider(v));
            depMeta.LicenseMetadata.Returns((global::NuGet.Packaging.LicenseMetadata?)null);
            depMeta.LicenseUrl.Returns((Uri?)null);
            depMeta.DependencySets.Returns(Array.Empty<global::NuGet.Packaging.PackageDependencyGroup>());

            accessor.GetPackageMetadata(depIdentity, Arg.Any<CancellationToken>())
                    .Returns(Result.Success(depMeta));
        }

        var repo = Substitute.For<INuGetRepository>();
        repo.Packages.Returns(accessor);
        return repo;
    }

    private static INuGetRepository CreateEmptyDestinationRepository()
    {
        var accessor = Substitute.For<INuGetPackageInfoAccessor>();
        accessor.DoesPackageExist(Arg.Any<PackageIdentity>(), Arg.Any<CancellationToken>())
                .Returns(Result.Success(false));

        var repo = Substitute.For<INuGetRepository>();
        repo.Packages.Returns(accessor);
        return repo;
    }
}
