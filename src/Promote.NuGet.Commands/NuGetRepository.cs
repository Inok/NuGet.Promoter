using NuGet.Protocol.Core.Types;

namespace Promote.NuGet.Commands;

public class NuGetRepository
{
    public SourceRepository Repository { get; }

    public string? ApiKey { get; }

    public NuGetRepository(SourceRepository repository, string? apiKey)
    {
        Repository = repository ?? throw new ArgumentNullException(nameof(repository));
        ApiKey = apiKey;
    }
}