# Promote.NuGet

[![Nuget](https://img.shields.io/nuget/v/Promote.NuGet)](https://www.nuget.org/packages/Promote.NuGet)

A tool to promote NuGet packages and their dependencies from one feed to another.

# Usage

## Promote

The `promote` command promotes a set of packages from one feed to another.

### Promote single package

The `promote package` command promotes the specified package from one feed to another. If `--version` specified, it promotes the specified version; otherwise, the latest version will be promoted.

```
> promote-nuget promote package --help

USAGE:
    Promote.NuGet.exe promote package <id> [OPTIONS]

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
promote-nuget promote package Newtonsoft.Json --destination '<TARGET-NUGET-FEED-URL>' --destination-api-key '<API-KEY>'

# Promotes the specified version of a package to the specified feed.
promote-nuget promote package Newtonsoft.Json --version 13.0.1 --destination '<TARGET-NUGET-FEED-URL>' --destination-api-key '<API-KEY>'
```

### Promote a configured set of packages

The `promote from-config` command promotes packages specified in a configuration file from one feed to another.
In the configuration, you can specify a set of packages with a list of versions, either exact (e.g., `13.0.1`) or range (e.g., `[12.0.0,)`).

```
> promote-nuget promote from-config --help

DESCRIPTION:
Promotes packages as configured in the specified file.

USAGE:
    Promote.NuGet.dll promote from-config <file> [OPTIONS]

ARGUMENTS:
    <file>    Path to the configuraton file (in YAML format). Example:
              packages:
                - id: System.Runtime
                  versions: 4.3.1
                - id: System.Text.Json
                  versions:
                    - '[6, 7)'
                    - '[8, 9)'

OPTIONS:
    -h, --help                   Prints help information
    -s, --source                 Source repository
        --source-api-key         Source repository's API key
    -d, --destination            Destination repository
        --destination-api-key    Destination repository's API key
        --no-cache               Do not use local cache
        --dry-run                Evaluate packages to promote, but don't actually promote them
        --verbose                Enable verbose logs
        --always-resolve-deps    Always resolve dependencies of a package, even if the package itself
                                 exists in the destination repository. This option allows to restore
                                 the integrity of the destination repository by promoting missing
                                 dependencies
        --force-push             Push packages and their dependencies even if they already exist in the
                                 destination repository. Use that option to restore the integrity of
                                 the destination repository (i.e. when some packages in the feed are
                                 broken)
```

Example:
```pwsh
# packages:
#   - id: System.Globalization
#     versions: 4.3.0
#   - id: System.Runtime
#     versions:
#       - '[4.1.0,4.1.2)'
#       - 4.3.1

promote-nuget promote from-config packages.yml --destination '<TARGET-NUGET-FEED-URL>' --destination-api-key '<API-KEY>'

# Resolving package requests:
# ├── System.Globalization (= 4.3.0)
# └── System.Runtime (>= 4.1.0 && < 4.1.2), (= 4.3.1)
# Matching packages for System.Globalization (= 4.3.0): 4.3.0
# Matching packages for System.Runtime (>= 4.1.0 && < 4.1.2), (= 4.3.1): 4.1.0, 4.1.1, 4.3.1
# Resolving packages to promote:
# ├── System.Globalization 4.3.0
# ├── System.Runtime 4.1.0
# ├── System.Runtime 4.1.1
# └── System.Runtime 4.3.1
# Processing package System.Globalization 4.3.0
# New dependency to process: Microsoft.NETCore.Platforms (>=
# 1.1.0)
# New dependency to process: Microsoft.NETCore.Targets (>=
# 1.1.0)
# New dependency to process: System.Runtime (>= 4.3.0)
# ... skipped ...
# Found 13 package(s) to promote:
# ├── Microsoft.NETCore.Platforms 1.0.1
# ├── Microsoft.NETCore.Platforms 1.0.2
# ├── Microsoft.NETCore.Platforms 1.1.0
# ├── Microsoft.NETCore.Platforms 1.1.1
# ├── Microsoft.NETCore.Targets 1.0.1
# ├── Microsoft.NETCore.Targets 1.0.6
# ├── Microsoft.NETCore.Targets 1.1.0
# ├── Microsoft.NETCore.Targets 1.1.3
# ├── System.Globalization 4.3.0
# ├── System.Runtime 4.1.0
# ├── System.Runtime 4.1.1
# ├── System.Runtime 4.3.0
# └── System.Runtime 4.3.1
# (1/13) Promote Microsoft.NETCore.Platforms 1.0.1
# (2/13) Promote Microsoft.NETCore.Platforms 1.0.2
# (3/13) Promote Microsoft.NETCore.Platforms 1.1.0
# (4/13) Promote Microsoft.NETCore.Platforms 1.1.1
# (5/13) Promote Microsoft.NETCore.Targets 1.0.1
# (6/13) Promote Microsoft.NETCore.Targets 1.0.6
# (7/13) Promote Microsoft.NETCore.Targets 1.1.0
# (8/13) Promote Microsoft.NETCore.Targets 1.1.3
# (9/13) Promote System.Globalization 4.3.0
# (10/13) Promote System.Runtime 4.1.0
# (11/13) Promote System.Runtime 4.1.1
# (12/13) Promote System.Runtime 4.3.0
# (13/13) Promote System.Runtime 4.3.1
# 13 package(s) promoted.
```


## Help
You can use `--help` argument to find all available commands and options. Example:

```
> promote-nuget promote --help

DESCRIPTION:
Promote packages and their dependencies from one feed to another.

USAGE:
    Promote.NuGet.dll promote [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    package <id>          Promotes the specified package and its dependencies from one feed to another
    list <file>           Promotes packages listed in the specified file, and their dependencies
    from-config <file>    Promotes packages as configured in the specified file
```

# Third-party components

- This application uses [NuGet.Client](https://github.com/NuGet/NuGet.Client) libraries.
  These libraries are available under the [Apache 2.0 license](http://www.apache.org/licenses/LICENSE-2.0).
- This application uses [FluentValidation](https://github.com/FluentValidation/FluentValidation) library.
  The library is available under the [Apache 2.0 license](https://licenses.nuget.org/Apache-2.0).
