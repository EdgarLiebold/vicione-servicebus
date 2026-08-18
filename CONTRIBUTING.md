# Contributing

## What you need

- .NET SDK **10.0.302** exactly. `global.json` pins it with `rollForward: disable`, so a different
  SDK fails the build rather than rolling forward silently.
- Docker, for the categories that need a real broker or database. `build/test-infrastructure/`
  carries the pinned images and the compose file.

## Restore, build, test, pack

```bash
dotnet restore ViciOne.ServiceBus.slnx
dotnet build   ViciOne.ServiceBus.slnx --configuration Release --no-restore
dotnet test    ViciOne.ServiceBus.slnx --configuration Release --no-build --no-restore
dotnet pack    ViciOne.ServiceBus.slnx --configuration Release --no-build --no-restore
```

The benchmark projects live in a second solution and are built the same way:

```bash
dotnet build ViciOne.ServiceBus.Benchmarks.slnx --configuration Release
```

Compilation output goes to `artifacts/sdk`, packages to `artifacts/packages`. There is no `bin` or
`obj` beside a project, so a build result can only come from one place.

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
