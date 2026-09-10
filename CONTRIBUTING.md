# Contributing

## Prerequisites

- A current stable .NET 10 SDK (`10.0.x`).
- Docker for tests that use local brokers or databases.
- Git configured to preserve the repository's line endings.

`NuGet.config` defines the package sources. Every project has a tracked `packages.lock.json`, and
locked restore is the normal path.

## Restore, build, and pack

```bash
dotnet restore ViciOne.ServiceBus.slnx --locked-mode
dotnet restore ViciOne.ServiceBus.Engineering.slnx --locked-mode
dotnet restore ViciOne.ServiceBus.Tests.Unit.slnx --locked-mode

dotnet build ViciOne.ServiceBus.slnx -c Release --no-restore -warnaserror
dotnet build ViciOne.ServiceBus.Engineering.slnx -c Release --no-restore -warnaserror
dotnet build ViciOne.ServiceBus.Tests.Unit.slnx -c Release --no-restore -warnaserror

dotnet pack ViciOne.ServiceBus.slnx -c Release --no-build --no-restore
```

Build output is written under `artifacts/sdk`, packages under `artifacts/packages`, and test results
under `artifacts/test-results`.

## Unit tests

```bash
dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx -c Release --no-build --no-restore \
  --results-directory artifacts/test-results/unit --minimum-expected-tests 4896 \
  --max-parallel-test-modules 1
```

xUnit owns discovery and assertions; Microsoft Testing Platform owns execution and the process exit
code. `tests/testconfig.json` makes warnings and skipped tests fail. Do not add a second runner or
weaken an assertion to make a change pass.

## Formatting and package examples

```bash
dotnet format --verify-no-changes ViciOne.ServiceBus.Engineering.slnx
dotnet format --verify-no-changes ViciOne.ServiceBus.Tests.Unit.slnx
tools/ci/verify_developer_journeys.sh
```

The journey gate packs all thirty delivery packages, restores the samples from those packages, and
compiles all eighteen examples plus three isolated provider-testing consumers without a
source-project reference. A fourth package-only consumer restores all twenty-nine runtime packages;
their reflected public surface must match `docs/api/packed-public-api.txt` exactly. When an API
change is intentional, update that contract explicitly with
`tools/ci/verify_developer_journeys.sh --update-public-api-contract`. macOS `protoc` startup
troubleshooting is documented in [docs/build.md](docs/build.md).

## Dependency updates

Change package versions only in `Directory.Packages.props`, then refresh every affected lock graph:

```bash
dotnet restore ViciOne.ServiceBus.slnx -p:RestoreLockedMode=false --force-evaluate
dotnet restore ViciOne.ServiceBus.Engineering.slnx -p:RestoreLockedMode=false --force-evaluate
dotnet restore ViciOne.ServiceBus.Tests.Unit.slnx -p:RestoreLockedMode=false --force-evaluate
git diff -- '**/packages.lock.json'
```

Read the complete lock-file diff before committing it.

## Change requirements

- Preserve every affected behavior and add a deterministic regression test for a correction.
- Keep cancellation causal and propagate caller tokens through asynchronous operations.
- Fix warnings and formatting errors at their source.
- Keep application APIs in `ViciOne.ServiceBus`, configuration entry points in
  `Microsoft.Extensions.DependencyInjection` or `ViciOne.ServiceBus.Configuration`, and extension
  contracts in their documented advanced, provider, operations, or testing layer.
- Write code, identifiers, comments, XML documentation, and commit messages in English.
- Comments describe current behavior, rationale, invariants, or operational constraints.
- Run `git diff --check`, the affected builds, and the affected test profiles before committing.

Provider-backed commands and their prerequisites are documented in [docs/build.md](docs/build.md).
