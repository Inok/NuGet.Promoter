namespace Promote.NuGet.Feeds;

public class NuGetRepositoryDescriptor
{
    public string Source { get; }

    public string? ApiKey { get; }

    public NuGetRepositoryDescriptor(string source, string? apiKey)
    {
        if (string.IsNullOrEmpty(source)) throw new ArgumentException("Value cannot be null or empty.", nameof(source));
        Source = source;
        ApiKey = apiKey;
    }
}