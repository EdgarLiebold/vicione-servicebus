# V5 payload, diagnostics, and analyzers validation commands

Date: 2026-09-03

All .NET commands used the locked dependency graph and this isolated CLI environment:

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

## Final Release builds

```text
/usr/local/share/dotnet/dotnet build ViciOne.ServiceBus.slnx --configuration Release \
  --no-restore --disable-build-servers /m:1 /nodeReuse:false --no-incremental

/usr/local/share/dotnet/dotnet build ViciOne.ServiceBus.Engineering.slnx --configuration Release \
  --no-restore --disable-build-servers /m:1 /nodeReuse:false --no-incremental
```

Both complete with zero warnings and zero errors.

## Complete native MTP regression

```text
/usr/local/share/dotnet/dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx \
  --configuration Release --no-build --no-restore \
  --results-directory artifacts/test-results/v5-payload-diagnostics-analyzers/unit-serial \
  --minimum-expected-tests 3460 --max-parallel-test-modules 1 --parallel none \
  --progress off --show-stdout None --show-stderr None --output Normal
```

Result: 3,460 total, 3,460 passed, zero failed, zero skipped. `dotnet test` ran outside the restricted
sandbox because Microsoft.Testing.Platform requires local IPC/named-pipe access. Serial xUnit execution prevents
unrelated process-global Activity listeners from overlapping while retaining module-level orchestration.

The focused Core, MessagePack, analyzer, architecture, requirements, and MessageData regression owners were also run
directly from their Release test applications. The deterministic JSON transport class passed 16/16 in three independent
processes.

## Format and repository gates

```text
/usr/local/share/dotnet/dotnet format ViciOne.ServiceBus.Engineering.slnx \
  --no-restore --include <51 changed/new C# paths> \
  --verify-no-changes --verbosity minimal

(cd review && shasum -a 256 -c PACKAGE_SHA256.txt)
jq empty tests/ViciOne.ServiceBus.Tests/Requirements/CoreRequirements.json
jq empty tests/ViciOne.ServiceBus.MessagePack.Tests/Requirements/MessagePackRequirements.json
jq empty tests/ViciOne.ServiceBus.Analyzers.Tests/Requirements/AnalyzerRequirements.json
jq empty tests/Architecture/ViciOne.ServiceBus.Architecture.Tests/Requirements/ArchitectureFoundationRequirements.json
git diff --check
```

The bounded format gate ran outside the sandbox because Roslyn's workspace host requires a local named pipe. All gates
pass. The manifest must run from `review/` because its protected paths are relative to that directory.
