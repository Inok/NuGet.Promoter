namespace Promote.NuGet.Feeds;

public class NuGetRepositoryDescriptor
{
    public string Source { get; }

    public string? Username { get; set; }

    public string? Password { get; set; }

    public string? ApiKey { get; }

    public NuGetRepositoryDescriptor(string source, string? apiKey)
        : this(source, null, null, apiKey)
    {
    }

    public NuGetRepositoryDescriptor(string source, string? username, string? password, string? apiKey)
    {
        if (string.IsNullOrEmpty(source)) throw new ArgumentException("Value cannot be null or empty.", nameof(source));

        Source = source;
        Username = username;
        Password = password;
        ApiKey = apiKey;
    }
}
