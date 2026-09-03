# V5 durable sender validation commands

Date: 2026-09-03

All .NET commands used the repository's locked package graph and the established isolated CLI environment:

```text
DOTNET_CLI_HOME=/private/tmp/dotnet-home
DOTNET_ROOT=/usr/local/share/dotnet
DOTNET_MULTILEVEL_LOOKUP=0
DOTNET_CLI_TELEMETRY_OPTOUT=1
DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=1
MSBUILDDISABLENODEREUSE=1
NUGET_PACKAGES=/Users/edgar.liebold/.nuget/packages
```

## Release builds

```text
/usr/local/share/dotnet/dotnet build ViciOne.ServiceBus.slnx --configuration Release \
  --no-restore --disable-build-servers /m:1 /nodeReuse:false --no-incremental

/usr/local/share/dotnet/dotnet build ViciOne.ServiceBus.Engineering.slnx --configuration Release \
  --no-restore --disable-build-servers /m:1 /nodeReuse:false --no-incremental
```

Both complete with zero warnings and zero errors.

## Native MTP execution

The sandbox rejects the named-pipe server used by `dotnet test`, so each already-built Release test application was
invoked directly. Representative Core command:

```text
/usr/local/share/dotnet/dotnet \
  artifacts/sdk/bin/ViciOne.ServiceBus.Tests.Unit/release/ViciOne.ServiceBus.Tests.dll \
  --parallel none --progress off --show-stdout None --show-stderr None --output Normal \
  --minimum-expected-tests 1669 --zero-tests-policy strict --timeout 15m \
  --results-directory /private/tmp/vsb-v5-p14-final/23-core \
  --report-xunit-ctrf --report-xunit-ctrf-filename result.json
```

The other 22 applications used the same strict invocation and their exact floors. CTRF summaries were parsed and summed
to 3,402 tests, 3,402 passed, zero failed, zero skipped.

## Format and repository gates

```text
/usr/local/share/dotnet/dotnet format ViciOne.ServiceBus.Engineering.slnx \
  --no-restore --verify-no-changes --verbosity minimal --include <57 changed/new C# paths>

shasum -a 256 -c review/PACKAGE_SHA256.txt
git diff --check
find tests -path '*/Requirements/*.json' -type f -exec jq empty {} +
```

`dotnet format` was run outside the restricted sandbox because Roslyn's build host requires a local named pipe. The
bounded 57-file gate passes. The protected review manifest, requirements JSON, empty-directory, repeated-path, review-
index, and diff checks pass.
