namespace Promote.NuGet.TestInfrastructure;

public record ProcessRunResult(int ExitCode, IReadOnlyCollection<string> StdOutput, IReadOnlyCollection<string> StdError)
{
    public string GetStdOutputAsNormalizedString() => string.Join(Environment.NewLine, StdOutput.Select(x => x.TrimEnd()));
};
