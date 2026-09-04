# V5 API usability validation commands

Date: 2026-09-04

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

## Release graph gates

```text
/usr/local/share/dotnet/dotnet build ViciOne.ServiceBus.Tests.Unit.slnx --configuration Release \
  --no-restore --disable-build-servers /m:1 /nodeReuse:false --no-incremental

/usr/local/share/dotnet/dotnet build ViciOne.ServiceBus.Engineering.slnx --configuration Release \
  --no-restore --disable-build-servers /m:1 /nodeReuse:false --no-incremental

/usr/local/share/dotnet/dotnet build ViciOne.ServiceBus.slnx --configuration Release \
  --no-restore --disable-build-servers /m:1 /nodeReuse:false --no-incremental

/usr/local/share/dotnet/dotnet pack ViciOne.ServiceBus.slnx --configuration Release \
  --no-restore --output artifacts/packages/review-v5-api \
  --disable-build-servers /m:1 /nodeReuse:false
```

## Native MTP execution

The restricted sandbox can block the MSBuild/test-host IPC channel, so the already-built Release test applications are
executed directly and serially. Representative complete Unit invocation:

```text
/usr/local/share/dotnet/dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx --configuration Release \
  --no-build --no-restore --max-parallel-test-modules 1 --minimum-expected-tests 3477 \
  --parallel none --no-progress --show-stdout None --show-stderr None \
  --output Normal --zero-tests-policy strict --timeout 20m
```

Focused Core, EF, MessagePack, and Architecture applications use the same serial MTP policy and write CTRF results below
`artifacts/test-results/review-v5-api`.

## Packed developer and API gate

```text
PUBLIC_API_BASELINE_OUTPUT=artifacts/verification/v5-api-public-baseline.txt \
  tools/ci/verify_developer_journeys.sh
```

The gate packs eight current ViciOne packages to a temporary feed, restores the 14-scenario consumer in locked mode
without project references, builds with warnings as errors, then loads the restored package assemblies to generate the
hashed public/protected API baseline.

## Hygiene and protected-input gates

```text
/usr/local/share/dotnet/dotnet format ViciOne.ServiceBus.Engineering.slnx \
  --no-restore --verify-no-changes --verbosity minimal --include <81 changed/new C# paths>

shasum -a 256 -c review/PACKAGE_SHA256.txt
jq empty docs/provider-capabilities.json tests/*/*/Requirements/*.json
git diff --check
git diff --name-only -- review
git diff --cached --name-only -- review
```

The formatter scope is intentionally the 81 files owned by this review package. A repository-wide formatter scan also
reported inherited formatting debt in unrelated provider, benchmark, and test files; those user-owned areas were not
changed as part of this closure.
