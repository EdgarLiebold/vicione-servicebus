# V4 final-tail validation commands

Date: 2026-09-03

All .NET commands used the repository's locked package graph and the isolated CLI environment:

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

## Build and package

```text
/usr/local/share/dotnet/dotnet build ViciOne.ServiceBus.slnx --configuration Release \
  --no-restore --disable-build-servers /m:1 /nodeReuse:false

/usr/local/share/dotnet/dotnet build ViciOne.ServiceBus.Engineering.slnx --configuration Release \
  --no-restore --disable-build-servers /m:1 /nodeReuse:false

/usr/local/share/dotnet/dotnet pack ViciOne.ServiceBus.slnx --configuration Release \
  --no-build --no-restore --disable-build-servers /m:1 /nodeReuse:false \
  --output /private/tmp/vsb-p12-pack-final
```

## Native MTP execution

The restricted runner rejects the named-pipe server used by `dotnet test`. Each already-built executable
was therefore invoked directly with the same Microsoft.Testing.Platform v2 host. Representative Core
invocation:

```text
/usr/local/share/dotnet/dotnet \
  artifacts/sdk/bin/ViciOne.ServiceBus.Tests.Unit/release/ViciOne.ServiceBus.Tests.dll \
  --parallel none --progress off --show-stdout None --show-stderr None --output Normal \
  --minimum-expected-tests 1613 --zero-tests-policy strict --timeout 15m \
  --results-directory /private/tmp/vsb-p12-core-final2 \
  --report-xunit-ctrf --report-xunit-ctrf-filename core.json
```

The identical serialized pattern was applied to the Architecture executable and the other 21 Unit
executables, each with its exact nonzero minimum count. Aggregate: 23 executables, 3,290 tests, 3,290
passed, zero failed, zero skipped.

## Formatting and repository gates

```text
/usr/local/share/dotnet/dotnet format ViciOne.ServiceBus.Engineering.slnx \
  --verify-no-changes --no-restore --verbosity minimal \
  --include <228 changed/new in-content C# paths>

git diff --cached --check
```

Additional scripted gates parse every requirements JSON file, verify every protected review manifest
entry and bundle hash, reject Testing projects in the shipping solution, reject repeated directory
components under `src`, and reject non-build empty directories.
