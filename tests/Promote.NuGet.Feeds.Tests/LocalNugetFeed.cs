﻿using System.Net;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Promote.NuGet.Feeds.Tests;

public sealed class LocalNugetFeed : IAsyncDisposable
{
    private readonly IContainer _container;

    public string FeedUrl { get; }

    public string ApiKey { get; }

    private LocalNugetFeed(IContainer container, string feedUrl, string apiKey)
    {
        _container = container;
        FeedUrl = feedUrl;
        ApiKey = apiKey;
    }

    public static async Task<LocalNugetFeed> Create()
    {
        var buffer = new byte[64];
        Random.Shared.NextBytes(buffer);
        var apiKey = Convert.ToBase64String(buffer);

        const ushort bagetterPort = 8080;

        var container = new ContainerBuilder()
                        .WithImage("bagetter/bagetter:1.0.0")
                        .WithEnvironment("ApiKey", apiKey)
                        .WithPortBinding(bagetterPort, true)
                        .WithWaitStrategy(Wait.ForUnixContainer()
                                              .UntilHttpRequestIsSucceeded(
                                                  r => r.ForPort(bagetterPort).ForPath("/").ForStatusCode(HttpStatusCode.OK)))
                        .Build();

        await container.StartAsync();

        var feedUrl = new UriBuilder("http", container.Hostname, container.GetMappedPublicPort(bagetterPort), "/v3/index.json").Uri.ToString();

        return new LocalNugetFeed(container, feedUrl, apiKey);
    }

    public ValueTask DisposeAsync()
    {
        return _container.DisposeAsync();
    }
}
