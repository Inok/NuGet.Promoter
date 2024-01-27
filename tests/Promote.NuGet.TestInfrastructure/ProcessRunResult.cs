namespace Promote.NuGet.TestInfrastructure;

public record ProcessRunResult(int ExitCode, IReadOnlyCollection<string> StdOutput, IReadOnlyCollection<string> StdError);
