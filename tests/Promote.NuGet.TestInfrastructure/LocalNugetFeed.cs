using System.Diagnostics;
using System.Net;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;

namespace Promote.NuGet.TestInfrastructure;

public sealed class LocalNugetFeed : IAsyncDisposable
{
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    private readonly IContainer _container;
    private readonly TempFile _certFile;

    public string FeedUrl { get; }

    public string ApiKey { get; }

    private LocalNugetFeed(IContainer container, string feedUrl, string apiKey, TempFile certFile)
    {
        _container = container;
        FeedUrl = feedUrl;
        ApiKey = apiKey;
        _certFile = certFile;
    }

    public static async Task<LocalNugetFeed> Create()
    {
        var certFile = TempFile.Create();
        var certPassword = Guid.NewGuid().ToString("N");

        var process = Process.Start("dotnet", $"dev-certs https -ep {certFile.Path} --password {certPassword}");
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            certFile.Dispose();
            throw new InvalidOperationException("Failed to export dev certificate.");
        }

        var buffer = new byte[64];
        Random.Shared.NextBytes(buffer);
        var apiKey = Convert.ToBase64String(buffer);

        const ushort bagetterHttpPort = 8080;
        const ushort bagetterHttpsPort = 8081;
        const string certInContainerPath = "/https-certs/bagetter.pfx";

        var container = new ContainerBuilder()
                        .WithImage("bagetter/bagetter:1.6.0")
                        .WithLogger(TestcontainersLogger.Instance)
                        .WithEnvironment("ApiKey", apiKey)
                        .WithEnvironment("ASPNETCORE_HTTP_PORTS", bagetterHttpPort.ToString())
                        .WithEnvironment("ASPNETCORE_HTTPS_PORTS", bagetterHttpsPort.ToString())
                        .WithEnvironment("Kestrel__Certificates__Default__Path", certInContainerPath)
                        .WithEnvironment("Kestrel__Certificates__Default__Password", certPassword)
                        .WithBindMount(certFile.Path, certInContainerPath, AccessMode.ReadOnly)
                        .WithPortBinding(bagetterHttpPort, true)
                        .WithPortBinding(bagetterHttpsPort, true)
                        .WithWaitStrategy(Wait.ForUnixContainer()
                                              .UntilHttpRequestIsSucceeded(
                                                  r => r.ForPort(bagetterHttpPort).ForPath("/").ForStatusCode(HttpStatusCode.OK)))
                        .Build();

        await _semaphore.WaitAsync();
        try
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await container.StartAsync(cts.Token);
        }
        catch (Exception)
        {
            var logs = await container.GetLogsAsync();
            await TestContext.Out.WriteLineAsync(logs.Stdout);
            await TestContext.Out.WriteLineAsync(logs.Stderr);
            throw;
        }
        finally
        {
            certFile.Dispose();
            _semaphore.Release();
        }

        var feedUrl = new UriBuilder("https", "localhost", container.GetMappedPublicPort(bagetterHttpsPort), "/v3/index.json").Uri.ToString();

        return new LocalNugetFeed(container, feedUrl, apiKey, certFile);
    }

    public ValueTask DisposeAsync()
    {
        _certFile.Dispose();
        return _container.DisposeAsync();
    }
}
