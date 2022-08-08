# NuGet.Promoter
A tool to promote NuGet packages and their dependencies from one feed to another.

# Usage

## Promote

The `promote` command allows to promote a set of packages from one feed to another.

### Promote single package

The `promote package` command promotes the specified package from one feed to another. If `--version` specified, it promotes the specified version; otherwise, the latest version will be promoted.

```
> NuGet.Promoter.exe promote package --help

USAGE:
    NuGet.Promoter.exe promote package <id> [OPTIONS]

ARGUMENTS:
    <id>    Id of the package to promote

OPTIONS:
    -h, --help                   Prints help information
    -s, --source                 Source repository
        --source-api-key         Source repository's API key
    -d, --destination            Destination repository
        --destination-api-key    Destination repository's API key
        --no-cache               Do not use local cache
        --dry-run                Evaluate packages to promote, but don't actually promote them
        --verbose                Enable verbose logs
    -v, --version                Version of the package. If not specified, the most recent version will be promoted


```

Example:
```pwsh
# Promotes the latest version of a package to the specified feed.
NuGet.Promoter.exe promote package Newtonsoft.Json --destination '<TARGET-NUGET-FEED-URL>' --destination-api-key '<API-KEY>'

# Promotes the specified version of a package to the specified feed.
NuGet.Promoter.exe promote package Newtonsoft.Json --version 13.0.1 --destination '<TARGET-NUGET-FEED-URL>' --destination-api-key '<API-KEY>'
```

### Promote packages listed in a file

The `promote from-file` command promotes packages listed in a file from one feed to another. 
In a file, you can specify either the exact version to promote (e.g. `13.0.1`), or a version range (e.g. `[12.0.0,)`).

```
> NuGet.Promoter.exe promote from-file --help

USAGE:
    NuGet.Promoter.exe promote from-file <file> [OPTIONS]

ARGUMENTS:
    <file>    Path of a file with a list of packages. Each line contains package id and its version or version range. Allowed formats:
              - Space-separated: <id> <version/version-range>
              - Package Manager: Install-Package <id> -Version <version/version-range>
              - PackageReference: <PackageReference Include="<version>" Version="<version/version-range>" />

OPTIONS:
    -h, --help                   Prints help information
    -s, --source                 Source repository
        --source-api-key         Source repository's API key
    -d, --destination            Destination repository
        --destination-api-key    Destination repository's API key
        --no-cache               Do not use local cache
        --dry-run                Evaluate packages to promote, but don't actually promote them
        --verbose                Enable verbose logs
```

Example:
```pwsh
# packages.txt:
# Install-Package System.IO -Version 4.3.0
# <PackageReference Include="System.Memory" Version="4.5.3" />
# Newtonsoft.Json [12.0.0,)
# Install-Package System.Text.Json -Version [5.0.0,6)

# Determines a set of packages to promote according to the list of versions and version ranges, ant promotes them.
NuGet.Promoter.exe promote from-file packages.txt --destination '<TARGET-NUGET-FEED-URL>' --destination-api-key '<API-KEY>'

# Resolving matching packages for:
# ├── System.IO (= 4.3.0)
# ├── System.Memory (= 4.5.3)
# ├── Newtonsoft.Json (>= 12.0.0)
# └── System.Text.Json (>= 5.0.0 && < 6.0.0)
# Resolving dependencies for:
# ├── System.IO 4.3.0
# ├── System.Memory 4.5.3
# ├── Newtonsoft.Json 12.0.1
# ├── Newtonsoft.Json 12.0.2
# ├── Newtonsoft.Json 12.0.3
# ├── Newtonsoft.Json 13.0.1
# ├── System.Text.Json 5.0.0
# ├── System.Text.Json 5.0.1
# └── System.Text.Json 5.0.2
# Found 106 package(s) to promote:
# ├── Microsoft.Bcl.AsyncInterfaces 5.0.0
# ├── Microsoft.CSharp 4.3.0
# ├── Microsoft.NETCore.Platforms 1.1.0
# ├── Microsoft.NETCore.Targets 1.1.0
# ├── Microsoft.Win32.Primitives 4.3.0
# ├── NETStandard.Library 1.6.1
# ├── Newtonsoft.Json 12.0.1
# ├── Newtonsoft.Json 12.0.2
# ├── Newtonsoft.Json 12.0.3
# ...
# (1/106) Promote Microsoft.Bcl.AsyncInterfaces 5.0.0
# (2/106) Promote Microsoft.CSharp 4.3.0
# (3/106) Promote Microsoft.NETCore.Platforms 1.1.0
# (4/106) Promote Microsoft.NETCore.Targets 1.1.0
# Minimal: Package 'C:\Users\...\AppData\Local\Temp\tmp513A.tmp' already exists at feed '...'.
# (5/106) Promote Microsoft.Win32.Primitives 4.3.0
# (6/106) Promote NETStandard.Library 1.6.1
...

```


## Help
You can use `--help` argument to find all available commands and options. Example:

```
> NuGet.Promoter.exe promote --help

USAGE:
    NuGet.Promoter.exe promote [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    package <id>        Promotes the specified package and its dependencies from one feed to another
    from-file <file>    Promotes packages listed in the specified file

```
