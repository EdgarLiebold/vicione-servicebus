# Contributing

## What you need

- .NET SDK **10.0.302** exactly. `global.json` pins it with `rollForward: disable`, so a different
  SDK fails the build rather than rolling forward silently.
- Docker, for the categories that need a real broker or database. `build/test-infrastructure/`
  carries the pinned images and the compose file.

## Restore, build, pack

```bash
dotnet restore ViciOne.ServiceBus.slnx --locked-mode
dotnet build   ViciOne.ServiceBus.slnx -c Release --no-restore
dotnet pack    ViciOne.ServiceBus.slnx -c Release --no-build --no-restore
```

Two solutions sit at the repository root, so every command names the one it means; an unqualified
`dotnet build` exits with MSB1011 and does nothing. The benchmarks and the diagnostics live in
`ViciOne.ServiceBus.Engineering.slnx`.

There is deliberately no `dotnet test` line here. A blanket run over the solution starts test
projects whose fixtures are not running and cannot reach the capabilities that need a real cloud
resource, so its result would describe what happened to be reachable rather than the product. Each
category is started through its runner; see below and [docs/build.md](docs/build.md).

## Packages are locked

Every project resolves against a tracked `packages.lock.json`, and locked mode is the repository
default, so a restore fails if the graph would resolve to anything other than what the lock files
record. Updating a package is the one operation that may change it:

```bash
# 1. change the version in Directory.Packages.props
# 2. resolve it and write the lock files
dotnet restore ViciOne.ServiceBus.slnx -p:RestoreLockedMode=false --force-evaluate
dotnet restore ViciOne.ServiceBus.Engineering.slnx -p:RestoreLockedMode=false --force-evaluate
# 3. read the lock file diff before committing it
git diff -- '**/packages.lock.json'
```

`--force-evaluate` is not optional. Without it the restore reuses the resolution it already holds for
packages whose version range did not change, so a lock file can stay stale while the command reports
success.

Compilation output goes to `artifacts/sdk` and packages to `artifacts/packages`. A run's raw files -
TRX, endpoint projection, control files, broker logs - go to `artifacts/run-output/<run>/`, and its
durable category record goes to the evidence parent the caller named, in that run's own `<run>` child.
Two roots, one child of each per run: the record is the one file meant to outlive the run, so it is
written where the caller asked for it rather than under the raw output. That is where the repository's
own build and test entry points put them. It is not a property of the machine:
a tool invoked with its own output path, or an SDK feature that writes elsewhere, still writes
elsewhere. The claim is about where this repository's paths lead, not about what is possible.

## Tests that need infrastructure

A category that needs a broker or a database is started through its runner, which creates the
fixture on a random loopback port, generates a fresh secret for the run and publishes the endpoint to
the test process:

```bash
python3 tools/ci/run_broker_category.py --broker postgres --broker mssql \
    --category sql-transport \
    --project tests/Transports/ViciOne.ServiceBus.SqlTransport.Tests \
    --evidence-dir artifacts/test-evidence/sql-transport
```

Nothing falls back to a default host, port, account or secret. A fixture that was not started makes
the affected tests fail with the names of the missing variables, before any connection is attempted.

## What a change has to bring

- Every relevant inherited test stays inside the acceptance boundary. Assertions are not weakened,
  skipped or narrowed to make a change pass.
- A correction comes with a probe that fails without it, and that probe fails naming the assurance it
  checks rather than timing out.
- Warnings are fixed, not silenced. Every project builds at the SDK warning level; a suppression is a
  reviewed exception with its reason in the file it applies to.
- Code, identifiers and comments are English. Comments state the current invariant; how it came about
  belongs in the commit message and the evidence.
- Commit messages are English and carry no tool or co-author trailers.

## Review

`git diff --check` has to be clean, the working tree has to be clean, and both solutions have to
build without warnings before a change is offered for review.
