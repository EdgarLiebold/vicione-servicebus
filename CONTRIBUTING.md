# Contributing

## What you need

- The current stable, supported .NET 10 SDK (`10.0.x`). `global.json` selects only Microsoft Testing
  Platform; it deliberately does not pin an SDK or runtime patch. Record `dotnet --info` with release
  evidence so the resolved patch remains auditable.
- Docker, for the categories that need a real broker or database. `build/test-infrastructure/`
  carries the pinned images and the compose file.

## Restore, build, pack

```bash
dotnet restore ViciOne.ServiceBus.slnx --locked-mode
dotnet build   ViciOne.ServiceBus.slnx -c Release --no-restore
dotnet pack    ViciOne.ServiceBus.slnx -c Release --no-build --no-restore
```

Separate product, engineering, and native-test profile targets sit at the repository root, so every
command names the one it means. Benchmarks and diagnostics live in
`ViciOne.ServiceBus.Engineering.slnx`.

The current native hermetic profile is xUnit 4 on Microsoft Testing Platform 2:

```bash
dotnet restore ViciOne.ServiceBus.Tests.Unit.slnx --locked-mode
dotnet build ViciOne.ServiceBus.Tests.Unit.slnx -c Release --no-restore --no-incremental
dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx -c Release --no-build --no-restore \
  --results-directory artifacts/test-results/unit
```

There is one test architecture. Add executable checks to the source-owner xUnit project and run them
through Microsoft Testing Platform; do not introduce another discovery, execution or verdict path.

## Packages are locked

Every project resolves against a tracked `packages.lock.json`, and locked mode is the repository
default, so a restore fails if the graph would resolve to anything other than what the lock files
record. Updating a package is the one operation that may change it:

```bash
# 1. change the version in Directory.Packages.props
# 2. resolve it and write the lock files
dotnet restore ViciOne.ServiceBus.slnx -p:RestoreLockedMode=false --force-evaluate
dotnet restore ViciOne.ServiceBus.Engineering.slnx -p:RestoreLockedMode=false --force-evaluate
dotnet restore ViciOne.ServiceBus.Tests.Unit.slnx -p:RestoreLockedMode=false --force-evaluate
# 3. read the lock file diff before committing it
git diff -- '**/packages.lock.json'
```

`--force-evaluate` is not optional. Without it the restore reuses the resolution it already holds for
packages whose version range did not change, so a lock file can stay stale while the command reports
success.

Compilation output goes to `artifacts/sdk`, packages to `artifacts/packages`, and MTP results to
`artifacts/test-results`. Provider endpoint projections, outage-control files and broker logs go to
the owning `artifacts/run-output/<run>/` directory. Each provider run has one unguessable identity,
one isolated Compose project and one output root; teardown findings make an otherwise green run red.

## Tests that need infrastructure

A LocalIntegration or External solution is created only with its first executable cohort. Fixtures
own their resources and credentials per run and clean them up afterward. Missing local endpoints,
invalid ports, absent provider selection, emulator mode in an external run, or an unavailable official
credential chain must fail before the affected test executes; none becomes a skip or fallback.

## What a change has to bring

- Every relevant behavior stays inside the acceptance boundary. Assertions are not weakened, skipped
  or narrowed to make a change pass.
- A correction comes with a probe that fails without it, and that probe fails naming the assurance it
  checks rather than timing out.
- Warnings are fixed, not silenced. Every project builds at the SDK warning level; a suppression is a
  reviewed exception with its reason in the file it applies to.
- Code, identifiers and comments are English. Comments state the current invariant; how it came about
  belongs in the commit message and the evidence.
- Commit messages are English and carry no tool or co-author trailers.

## Review

`git diff --check` has to be clean, the working tree has to be clean, and every affected product,
engineering, and native-profile target has to build without warnings before review.
