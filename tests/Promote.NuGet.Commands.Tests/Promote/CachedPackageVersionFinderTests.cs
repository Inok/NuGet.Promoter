using CSharpFunctionalExtensions;
using FluentAssertions;
using Moq;
using NuGet.Versioning;
using NUnit.Framework;
using Promote.NuGet.Commands.Promote;
using Promote.NuGet.Feeds;

namespace Promote.NuGet.Commands.Tests.Promote;

[TestFixture]
public class CachedPackageVersionFinderTests
{
    [Test]
    public async Task Get_all_versions_from_repository()
    {
        var packageId = "package.1";
        var expectedVersions = new[] { new NuGetVersion(1, 0, 0), new NuGetVersion(1, 1, 0) };
        var packageInfoAccessor = CreateRepository(packageId, expectedVersions);
        var repository = Mock.Of<INuGetRepository>(r => r.Packages == packageInfoAccessor.Object);

        var sut = new CachedPackageVersionFinder(repository);

        var result = await sut.GetAllVersions("package.1", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(expectedVersions);

        packageInfoAccessor.Verify(x => x.GetAllVersions("package.1", default), Times.Once);
    }

    [Test]
    public async Task Versions_are_cached()
    {
        var packageId = "package.1";
        var expectedVersions = new[] { new NuGetVersion(1, 0, 0), new NuGetVersion(1, 1, 0) };
        var packageInfoAccessor = CreateRepository(packageId, expectedVersions);
        var repository = Mock.Of<INuGetRepository>(r => r.Packages == packageInfoAccessor.Object);

        var sut = new CachedPackageVersionFinder(repository);

        for (var i = 0; i < 5; i++)
        {
            var result = await sut.GetAllVersions("package.1", CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeSameAs(expectedVersions);

            packageInfoAccessor.Verify(x => x.GetAllVersions("package.1", default), Times.Once);
        }
    }

    [Test]
    public async Task Different_packages_has_no_effect_on_each_other()
    {
        var package1 = "package.1";
        var package1Versions = new[] { new NuGetVersion(1, 0, 0), new NuGetVersion(1, 1, 0) };

        var package2 = "package.2";
        var package2Versions = new[] { new NuGetVersion(2, 0, 0), new NuGetVersion(3, 0, 0) };

        var versions = new Dictionary<string, Result<IReadOnlyCollection<NuGetVersion>>>()
                       {
                           [package1] = package1Versions,
                           [package2] = package2Versions,
                       };

        var packageInfoAccessor = CreateRepository(versions);
        var repository = Mock.Of<INuGetRepository>(r => r.Packages == packageInfoAccessor.Object);

        var sut = new CachedPackageVersionFinder(repository);

        // Get versions from repository

        var result1 = await sut.GetAllVersions("package.1", CancellationToken.None);
        result1.IsSuccess.Should().BeTrue();
        result1.Value.Should().BeSameAs(package1Versions);

        packageInfoAccessor.Verify(x => x.GetAllVersions("package.1", default), Times.Once);

        var result2 = await sut.GetAllVersions("package.2", CancellationToken.None);
        result2.IsSuccess.Should().BeTrue();
        result2.Value.Should().BeSameAs(package2Versions);

        packageInfoAccessor.Verify(x => x.GetAllVersions("package.2", default), Times.Once);

        // Now get them from cache

        result1 = await sut.GetAllVersions("package.1", CancellationToken.None);
        result1.IsSuccess.Should().BeTrue();
        result1.Value.Should().BeSameAs(package1Versions);

        packageInfoAccessor.Verify(x => x.GetAllVersions("package.1", default), Times.Once);

        result2 = await sut.GetAllVersions("package.2", CancellationToken.None);
        result2.IsSuccess.Should().BeTrue();
        result2.Value.Should().BeSameAs(package2Versions);

        packageInfoAccessor.Verify(x => x.GetAllVersions("package.2", default), Times.Once);
    }

    [Test]
    public async Task Package_id_is_case_insensitive()
    {
        var packageId = "package.1";
        var packageVersions = new[] { new NuGetVersion(1, 0, 0), new NuGetVersion(1, 1, 0) };
        var packageInfoAccessor = CreateRepository(packageId, packageVersions);
        var repository = Mock.Of<INuGetRepository>(r => r.Packages == packageInfoAccessor.Object);

        var sut = new CachedPackageVersionFinder(repository);

        var result = await sut.GetAllVersions("package.1", CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(packageVersions);

        result = await sut.GetAllVersions("Package.1", CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(packageVersions);

        result = await sut.GetAllVersions("PACKAGE.1", CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(packageVersions);

        packageInfoAccessor.Verify(x => x.GetAllVersions("package.1", default), Times.Once);
        packageInfoAccessor.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Returns_error_if_repository_returns_error()
    {
        var packageId = "package.1";
        var failure = Result.Failure<IReadOnlyCollection<NuGetVersion>>("Ooops");
        var packageInfoAccessor = CreateRepository(packageId, failure);
        var repository = Mock.Of<INuGetRepository>(r => r.Packages == packageInfoAccessor.Object);

        var sut = new CachedPackageVersionFinder(repository);

        var result = await sut.GetAllVersions("package.1", CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Ooops");

        packageInfoAccessor.Verify(x => x.GetAllVersions("package.1", default), Times.Once);
    }

    [Test]
    public async Task Do_not_cache_errors()
    {
        var packageId = "package.1";
        var failure = Result.Failure<IReadOnlyCollection<NuGetVersion>>("Ooops");
        var packageInfoAccessor = CreateRepository(packageId, failure);
        var repository = Mock.Of<INuGetRepository>(r => r.Packages == packageInfoAccessor.Object);

        var sut = new CachedPackageVersionFinder(repository);

        var firstResult = await sut.GetAllVersions("package.1", CancellationToken.None);
        firstResult.IsFailure.Should().BeTrue();
        firstResult.Error.Should().Be("Ooops");
        packageInfoAccessor.Verify(x => x.GetAllVersions("package.1", default), Times.Exactly(1));

        var secondResult = await sut.GetAllVersions("package.1", CancellationToken.None);
        secondResult.IsFailure.Should().BeTrue();
        secondResult.Error.Should().Be("Ooops");
        packageInfoAccessor.Verify(x => x.GetAllVersions("package.1", default), Times.Exactly(2));

        firstResult.Should().NotBeSameAs(secondResult);
    }

    private static Mock<INuGetPackageInfoAccessor> CreateRepository(string packageId, Result<IReadOnlyCollection<NuGetVersion>> result)
    {
        var packages = new Dictionary<string, Result<IReadOnlyCollection<NuGetVersion>>>
                       {
                           [packageId] = result,
                       };
        return CreateRepository(packages);
    }

    private static Mock<INuGetPackageInfoAccessor> CreateRepository(IReadOnlyDictionary<string, Result<IReadOnlyCollection<NuGetVersion>>> versions)
    {
        var packageInfoAccessor = new Mock<INuGetPackageInfoAccessor>(MockBehavior.Strict);

        foreach (var pair in versions)
        {
            packageInfoAccessor.Setup(x => x.GetAllVersions(pair.Key, It.IsAny<CancellationToken>()))
                               .ReturnsAsync(pair.Value);
        }

        return packageInfoAccessor;
    }
}