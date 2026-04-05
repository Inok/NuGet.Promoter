# AGENTS.md

Guidance for AI coding agents working in this repository.

## Project Overview

NuGet.Promoter is a .NET 10.0 CLI tool that copies NuGet packages and their dependencies from one feed to another. CLI built with Spectre.Console.Cli.

## Build & Test

[Nuke](https://nuke.build/) runs builds. PowerShell is cross-platform and preferred; bash is a fallback.

```powershell
.\build.ps1 -Target Compile
.\build.ps1 -Target RunTests
.\build.ps1 -Target All               # build + test + pack
.\build.ps1 -Target PackNugetPackages
```

```bash
# Bash fallback (Linux/macOS)
./build.sh -Target Compile
./build.sh -Target RunTests
./build.sh -Target All
./build.sh -Target PackNugetPackages
```

For local dev, use `dotnet test` — it builds and tests in one call:

```bash
# All tests
dotnet test --verbosity quiet --nologo
# One project
dotnet test --project tests/Promote.NuGet.Commands.Tests --verbosity quiet --nologo
# One test (MTP filter syntax)
dotnet test --project tests/Promote.NuGet.Commands.Tests --verbosity quiet --nologo -- --filter "FullyQualifiedName~DistinctQueue"
```

Integration tests need Docker/Podman (Testcontainers).

### dotnet command rules

- Always pass `--verbosity quiet --nologo` to `dotnet build` / `dotnet test`.
- Never pipe dotnet output through `tail`, `grep`, or other filters. Multiple test projects print summaries at different points; filters drop some of them. `--verbosity quiet` already limits output.
- Never use `--no-build` with `dotnet test` — risks stale binaries and hidden compile errors.
- Never use `-q` — it hides everything. Use `--verbosity quiet` instead.
- Run tests after every change.

## Architecture

```
Promote.NuGet (CLI)                   ← Spectre.Console.Cli commands, entry point
  └─ Promote.NuGet.Commands           ← core promotion logic
       └─ Promote.NuGet.Feeds         ← NuGet feed abstraction (NuGet.Protocol)
```

**Flow:**
1. CLI command (`PromoteSinglePackageCommand`, `PromoteFromConfigurationCommand`, `PromoteListCommand`) parses input.
2. `PromotePackageCommand` runs the work: resolve versions → walk dependency tree → validate licenses → copy packages to destination.
3. `PackageRequestResolver` matches version policies to versions; `PackagesToPromoteResolver` builds the dependency graph.
4. All feed access goes through `INuGetRepository` / `INuGetPackageInfoAccessor`.

**Key design choices:**
- `Result<T>` (CSharpFunctionalExtensions) for error handling — expected failures return errors, not exceptions.
- Version matching via visitor pattern: `ExactPackageVersionPolicy`, `VersionRangePackageVersionPolicy`, `LatestPackageVersionPolicy`.
- No DI container — dependencies wired manually in CLI commands.

## Testing

- **Framework:** NUnit 4 on Microsoft.Testing.Platform
- **Mocking:** NSubstitute
- **Assertions:** AwesomeAssertions (fluent style)
- **Integration tests** (`Promote.NuGet.Tests`): local NuGet feed via Testcontainers, CLI run as child process (`PromoteNugetProcessRunner`)
- **Shared helpers** (`Promote.NuGet.TestInfrastructure`): `LocalNugetFeed`, `ProcessWrapper`, `TempFile`
- All tests have `[CancelAfter(60_000)]`

### Conventions

- **Class names:** `{ClassName}Tests`.
- **Method names:** underscore-separated, two formats:
  - `MemberName_does_something_under_some_conditions` for a specific member — e.g. `Visit_LatestPackageRequest_finds_greatest_released_package_version`, `NormalizeLicense_normalizes_case`
  - `Scenario_description` when no specific member or plain text is clearer — e.g. `Returns_error_if_repository_returns_error`, `Package_id_is_case_insensitive`
- **Assertions:** use AwesomeAssertions (`actualValue.Should().Be(expected)`). NUnit's `Assert.That(...)` is discouraged. One rich assertion (`actual.Should().BeEquivalentTo(expected)`) is better than many simple checks.
- Use `// Act` and `// Assert` comments when helpful. Never add `// Arrange`.
- Each test must be independent — no reliance on execution order.
- Use `[TestCase]` / `[TestCaseSource]` when inputs vary but logic is the same.
- Tests must be deterministic: no sleeps, no timeouts, no network calls outside integration tests.
- Test public APIs; use `[InternalsVisibleTo]` only when needed.

## C# Guidelines

### Style & Language

- C# 14, nullable on, warnings as errors. Fix warnings right away.
- Use modern C#: pattern matching, switch expressions, target-typed `new`, using declarations, file-scoped namespaces, primary constructors, collection expressions.
- Use `var` when the type is obvious.
- Always use braces for `if`/`else`/`for`/`foreach`/`while`/`do`, even single lines.
- Keep nesting shallow — prefer early returns and guard clauses.
- Package versions go only in `Directory.Packages.props` (central management, transitive pinning). Never add versions in `.csproj`.
- `.editorconfig` sets formatting (4-space indent, 160-char limit). Microsoft.CodeAnalysis.NetAnalyzers enforced.
- GitVersion handles versioning (continuous-deployment on `main`).

### Nullability

- Fix nullable warnings at the source, don't suppress.
- `!` (null-forgiving) is a last resort. When used, add a comment explaining WHY the value can't be null and WHY the operator can't be avoided.
- Use `[MemberNotNullWhen]`, `[NotNullIfNotNull]`, etc. for complex null contracts.

### Design

- `sealed` by default. Only unseal if the class is meant to be inherited.
- Immutable data → `sealed record`. Small value types → `readonly struct`.
- `required` for mandatory properties; `init` over `set`.
- Small, focused classes (single responsibility). Composition over inheritance.
- Private fields `readonly` when possible.
- Return `IReadOnlyCollection<T>` / `IReadOnlyList<T>` from APIs. Empty collections, not `null`.

### Async

- `async`/`await` end-to-end. Never `async void`.
- Never block with `.Result`, `.Wait()`, `.GetAwaiter().GetResult()`.
- Async methods end with `Async`.
- Pass `CancellationToken` as last parameter; check it in long operations.

### Performance

- Reduce allocations on hot paths; skip LINQ in tight loops.
- Don't enumerate `IEnumerable<T>` twice — materialize if needed.
- Use `FrozenSet<T>` / `FrozenDictionary<TKey, TValue>` for build-once, read-many collections.

### Error Handling & Logging

- Throw specific exceptions: `ArgumentException`, `ArgumentNullException`, `ArgumentOutOfRangeException` for bad inputs; `InvalidOperationException` for bad state.
- Don't swallow exceptions. Don't use exceptions for control flow — use `Result<>` types or `Try*` patterns.
- Structured logging with templates (`"Processing order {OrderId}"`) — no string interpolation.
- Never log PII or secrets.

### Documentation

- XML docs: short, focused on intent and usage. Don't repeat the signature.
- Inline comments only when code can't convey the intent: complex logic, hidden constraints, non-obvious invariants. Delete comments that just describe the next line.
