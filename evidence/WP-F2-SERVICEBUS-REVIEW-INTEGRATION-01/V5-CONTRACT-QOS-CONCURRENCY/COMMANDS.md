# V5 package 1 validation commands

Date: 2026-09-03

All .NET commands used the repository's locked package graph and this isolated CLI environment:

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

## Builds

```text
/usr/local/share/dotnet/dotnet build ViciOne.ServiceBus.slnx --configuration Release \
  --no-restore --disable-build-servers /m:1 /nodeReuse:false

/usr/local/share/dotnet/dotnet build ViciOne.ServiceBus.Engineering.slnx --configuration Release \
  --no-restore --disable-build-servers /m:1 /nodeReuse:false
```

## Native MTP execution

The restricted environment rejects the named-pipe server used by `dotnet test`. Each Release application
was invoked directly and serially. Representative Core invocation:

```text
/usr/local/share/dotnet/dotnet \
  artifacts/sdk/bin/ViciOne.ServiceBus.Tests.Unit/release/ViciOne.ServiceBus.Tests.dll \
  --parallel none --progress off --show-stdout None --show-stderr None --output Normal \
  --minimum-expected-tests 1635 --zero-tests-policy strict --timeout 15m \
  --results-directory /private/tmp/vsb-v5-p13-finalmatrix-03/23-core \
  --report-xunit-ctrf --report-xunit-ctrf-filename result.json
```

The same strict invocation ran the other 22 Unit/Architecture applications with their exact minimum test
counts. The corrected Azure Service Bus owner was rebuilt and rerun with a minimum of 25. Aggregate:
3,345 tests, 3,345 passed, zero failed, zero skipped.

## Formatting, manifest, and repository gates

```text
/usr/local/share/dotnet/dotnet format ViciOne.ServiceBus.Engineering.slnx \
  --no-restore --verify-no-changes --verbosity minimal \
  --include <40 changed/new C# paths>

(cd review && shasum -a 256 -c PACKAGE_SHA256.txt)
shasum -a 256 review/02_V5_FROZEN_OVERLAY/ViciOne_ServiceBus_V5_CUMULATIVE.patch
shasum -a 256 review/02_V5_FROZEN_OVERLAY/ViciOne_ServiceBus_V5.bundle
git diff --check
git diff --cached --check
```

Additional scoped gates parse every requirements JSON file, reject trailing whitespace in changed text,
reject non-build empty directories and repeated `src` path components, and prove that `review/**` is
untracked and unstaged.
