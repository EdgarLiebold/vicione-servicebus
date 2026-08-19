# Build and verification

The detailed contract. The root [README](../README.md) and [CONTRIBUTING](../CONTRIBUTING.md) stay
short and point here rather than repeating any of it.

## What the build needs

- .NET SDK **10.0.302** exactly. `global.json` pins it with `rollForward: disable`, so a different
  SDK fails the build instead of silently rolling forward.
- Exactly one package source: **nuget.org**, named in `NuGet.config`. Both `clear` elements drop
  whatever the machine has inherited, so no unnamed source can supply a package and no machine level
  entry can disable the only source that remains, and the mapping claims every pattern for it. The
  source is public and anonymous; no credentials belong in that file. It is not "the sources
  configured for the development environment" - the point is that the machine's configuration does
  not participate.

## Two solutions

| Solution | Holds |
|---|---|
| `ViciOne.ServiceBus.slnx` | the product, its tests and the analyzer |
| `ViciOne.ServiceBus.Engineering.slnx` | everything that is engineering rather than product: the benchmarks, the diagnostics and their tests, plus the product projects they reference |

Every `dotnet` command names its solution. Two solutions sit at the repository root, so an
unqualified `dotnet build` does not pick one - it exits with MSB1011 and does nothing.

```bash
dotnet restore ViciOne.ServiceBus.slnx --locked-mode
dotnet build   ViciOne.ServiceBus.slnx -c Release --no-restore
dotnet pack    ViciOne.ServiceBus.slnx -c Release --no-build --no-restore
```

## Locked restore

Every project resolves against a tracked `packages.lock.json`, and locked mode is the default rather
than a flag the caller has to remember. A restore that would resolve anything other than what the
lock file records fails, wherever it runs.

Updating a package is the one operation that may change the graph, and it says so on the command
line:

```bash
dotnet restore ViciOne.ServiceBus.slnx -p:RestoreLockedMode=false --force-evaluate
dotnet restore ViciOne.ServiceBus.Engineering.slnx -p:RestoreLockedMode=false --force-evaluate
```

`--force-evaluate` is not optional here. Without it the restore reuses the resolution it already has
for packages whose version range did not change, so a lock file can stay stale while the command
reports success. The changed lock files are then reviewed in the diff like any other change.

## Target frameworks

Every runtime, test, benchmark and tool project targets **net10.0**.

Exactly **three** projects stay on `netstandard2.0`, for two different reasons, and the exception is
granted **by path** in `Directory.Build.targets`. A project cannot grant itself one: a marker a
project sets about itself is not a control, because any project can set it, and a copied or renamed
project would then carry the exception with it.

| Project | Why |
|---|---|
| `src/ViciOne.ServiceBus.Analyzers` | a Roslyn component: the compiler loads it, and that compiler is not a `net10.0` process |
| `src/ViciOne.ServiceBus.Analyzers.CodeFixes` | the same |
| `src/ViciOne.ServiceBus.Analyzers.Package` | compiles nothing and ships no build output; its framework is the **consumer surface** of the analyzer package, because a package with no `lib/` folder is matched by the framework group of its nuspec |

The two Roslyn components declare the standard `IsRoslynComponent`, which states what they are, and
they keep an explicit `LangVersion` and `WarningLevel`: `netstandard2.0` inherits neither from the
SDK, so without them those two would build at a different language level than the rest of the
repository. That is a compatibility-bound compiler requirement of those two projects, not a general
escape. The package project sets neither, because it compiles no source at all.

Why the package project is not simply `net10.0` is a measurement, not an opinion: packed both ways at
one commit, the package contents are byte identical and one line of the nuspec changes, the empty
dependency group, from `.NETStandard2.0` to `net10.0`. That line narrows which projects may reference
the package, which is a capability. The comparison is in
`evidence/WP-F2-SERVICEBUS-A-PLUS-RECOVERY-03/record-0097/`.

`Directory.Build.targets` refuses `netstandard2.0` from a fourth project, refuses `IsRoslynComponent`
from anything but those two, and refuses the retired self-marker outright.

## The central build contract

`Directory.Build.props` is imported before a project's own content and states defaults a project may
still override. `Directory.Build.targets` is imported after it, so what it asserts is the state the
project actually ended up in. It raises errors, never warnings.

| Code | Refuses |
|---|---|
| `VOSB0001` | a project that turned `RestorePackagesWithLockFile` off |
| `VOSB0006` | a project that turned `RestoreLockedMode` off in itself rather than on the command line |
| `VOSB0002` | a packable project without a licence expression or file |
| `VOSB0003` | a packable project without the readme the notice promises |
| `VOSB0004` | a target framework this product does not support |
| `VOSB0005` | `netstandard2.0` from a project that is not one of the three named by path |
| `VOSB0007` | `IsRoslynComponent` from a project that is not one of the two Roslyn components |
| `VOSB0008` | the retired self-marker, which a project used to grant itself the framework exception with |

Both files are imported by every project, including a project built directly rather than through its
solution. There is no documented way to leave the contract: an earlier version of this page suggested
`-p:ImportDirectoryBuildTargets=false` for a tooling experiment, and that is a general bypass of every
late gate which nobody authorised. The one command line property that is allowed is the narrow restore
property of a package update, above.

Six properties are reserved for the two root files, and
`check_no_project_leaves_the_central_contract` refuses every one of them in any project, in any
project-local `.props` or `.targets`, and in any `Directory.Build.props` further down the tree:

| Reserved property | What writing it does |
|---|---|
| `ImportDirectoryBuildTargets` | skips the late central gates entirely |
| `DirectoryBuildTargetsPath` | points the late contract at another file |
| `CustomBeforeMicrosoftCommonTargets` | injects a file ahead of the contract |
| `CustomAfterMicrosoftCommonTargets` | injects a file behind the contract |
| `RestoreLockedMode` | resolves past a lock file the project still carries |
| `RestoreLockedModeFromCommandLine` | hands the project the exception the command line exists to make visible |

The rule parses the MSBuild XML rather than searching its text, and reads every `PropertyGroup`
wherever it stands, including inside a `Choose` or a `Target`. It searched for exact XML text before,
and three executed counterexamples walked past it: `RestoreLockedMode` written as `False`, a
conditional `ImportDirectoryBuildTargets` and a conditional `DirectoryBuildTargetsPath` redirect.

## Verification

`build/verification/VERIFICATION_MODEL.json` is the one active truth about what this product carries
and how each of it is verified. Every project of this repository belongs to exactly one capability
there. Every required run names the job that starts it, the category it starts, the project it runs,
the number of cases it must not fall below, and every case it may leave unexecuted with a reason.

```bash
python3 tools/ci/verification_model.py     # print the model
python3 tools/ci/policy_validator.py       # check every invariant before a restore
```

The check runs both ways: a declared run whose job does not start it is refused, and a required job
that no capability explains is refused as well.

## Running the tests

There is no single command that proves this product locally. A blanket
`dotnet test ViciOne.ServiceBus.slnx` starts test projects whose fixtures are not running, and the
capabilities that need a real cloud resource have no local fixture at all - so a green result from it
would be a statement about what happened to be reachable, not about the product.

Each category is started through its runner, which owns the fixture:

```bash
python3 tools/ci/run_test_category.py --category core \
  --project tests/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj \
  --evidence-dir artifacts/required/core

python3 tools/ci/run_broker_category.py --broker rabbitmq --category rabbitmq \
  --project tests/Transports/ViciOne.ServiceBus.RabbitMqTransport.Tests/ViciOne.ServiceBus.RabbitMqTransport.Tests.csproj \
  --evidence-dir artifacts/required/rabbitmq
```

The broker runner starts the pinned fixture, reads back the ephemeral loopback ports Docker bound,
generates a run-scoped account, hands all of it to the test process alone and removes the fixture
afterwards. A fixture that was not started makes the affected tests fail with a named missing contract
rather than falling back to a default host or secret.

Every run owns everything it writes. One identity per run gives it its compose project and a root under
`artifacts/run-output/<identity>/`, and the raw TRX, the endpoint projection, the control directory and
the broker logs all live there. A directory named with `--evidence-dir` is a **parent**: the record
lands in `<evidence-dir>/<identity>/`, never in a file a second invocation of the same category would
overwrite. A category started directly, without the broker runner, creates the same kind of root for
itself.

`REAL_EPHEMERAL_CLOUD` capabilities - Azure Service Bus, Event Hubs, Amazon SQS, S3, Azure Table,
Azure Blob and DynamoDB - are not verified locally and are not verified in the required profile
either. The model says so per capability; nothing claims otherwise.

## Diagnostics

Two deliberately started scenarios live in `tools/diagnostics/ViciOne.ServiceBus.Diagnostics` and
gate nothing. See their [README](../tools/diagnostics/ViciOne.ServiceBus.Diagnostics/README.md).
