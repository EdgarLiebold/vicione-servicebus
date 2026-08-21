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

The check runs both ways: every declared category is reached by exactly one required job's selection,
and a required job the model's job map does not explain is refused as well.

## Running the tests

One command, and it takes a named scope:

```bash
python3 tools/ci/verify.py --selection all
python3 tools/ci/verify.py --selection activemq
python3 tools/ci/verify.py --selection core --record-expected   # regenerate an expected set
```

A blanket `dotnet test ViciOne.ServiceBus.slnx` starts test projects whose fixtures are not running,
and the capabilities that need a real cloud resource have no local fixture at all - so a green result
from it would be a statement about what happened to be reachable, not about the product.

What a selection contains, which categories need which brokers, how long each may take and which
identities it is expected to execute are all in the verification model. The entry point resolves the
selection, owns a unique run root per category, starts the child in a session of its own under the
modelled budget, takes the whole tree down if that budget expires, removes the fixture and writes one
receipt. `tools/ci/run_test_category.py` and `tools/ci/run_broker_category.py` are still there and do
the work; they are components behind the entry point rather than things a workflow calls.

The answer is a set, not a count. A category passes when the identities it executed are exactly the
ones the model expects of it: nothing missing, nothing unexpected, nothing twice, nothing failed, and
nothing skipped that the model did not approve. A count stays constant when one case is dropped and
another is added, which is precisely the change a floor cannot see; `minimumExecutedCases` remains as a
fast regression floor and is explicitly secondary.

An expected set is generated by `--record-expected` from a clean run of that category and is never
edited by hand. `null` means no complete clean run has recorded one, and a category in that state
cannot be part of a passing receipt - the receipt says which ones those are.

Every run owns everything it writes. One identity per run gives it its compose project and a root under
`artifacts/run-output/<identity>/`, which carries a token file: a run root is minted by the process
that owns it or handed down with the token that proves it, and a bare path in the environment is
refused. The raw result file, the endpoint projection, the control directory and the broker logs live
there; the durable record lands under the caller's evidence parent in that run's own child of it.

The broker runner starts the pinned fixture, reads back the ephemeral loopback ports Docker bound,
generates a run-scoped account, hands all of it to the test process alone and removes the fixture
afterwards. A fixture that was not started makes the affected tests fail with a named missing contract
rather than falling back to a default host or secret. A fixture it could not clean before starting is a
startup failure, and a control thread that is still alive after its bounded join stops the fixture from
being removed underneath it.

## The receipt, and what it is worth

What authorises a pass is the exit status of `tools/ci/verify.py` inside the required check. The
receipt is derived audit evidence, and it is worth being precise about the difference: a JSON document
cannot show that a test process ever started, whoever wrote it.

Every run writes one, under its run root and beside the caller's evidence. It binds the commit, the
tree, the hash of the model it was read against, the selection and its resolved categories, and it
carries the full identity lists rather than summaries.

```bash
python3 tools/ci/validate_receipt.py --receipt <file> --selection all
```

The reader refuses a receipt of another commit, of another model, of another schema, from a working
tree that was not clean, and a narrower selection presented as a wider one. Beyond that it refuses one
whose numbers do not follow from its own facts: every derived field - passed, missing, unexpected,
duplicate, the unapproved skips, the findings and the terminal result - is recomputed from the
expected, executed, failed and skipped sets the record states, and the record has to agree. The shape
is closed in both directions, so a missing field and a field this reader does not check are both
refusals. What the record says it ran is compared with the model: the project, the brokers, the outage
permission and the whole child command.

Where the native result files are still beside the receipt they are hashed and parsed again, and the
record has to agree with them. That part, and only that part, is evidence from outside the receipt,
and the reader says how much of it there was. A receipt handed over without those files is a
consistency checked record of what a run reported - it is not independent proof that the run happened,
and it does not claim to be. Nothing here is protection against somebody with administrative rights
over this repository; branch protection, the required check and review are.

`REAL_EPHEMERAL_CLOUD` capabilities - Azure Service Bus, Event Hubs, Amazon SQS, S3, Azure Table,
Azure Blob and DynamoDB - are not verified locally and are not verified in the required profile
either. The model says so per capability; nothing claims otherwise.

## Diagnostics

Two deliberately started scenarios live in `tools/diagnostics/ViciOne.ServiceBus.Diagnostics` and
gate nothing. See their [README](../tools/diagnostics/ViciOne.ServiceBus.Diagnostics/README.md).

## Native test tree (tests2)

The native test estate builds and runs through the Microsoft Testing Platform. `global.json` selects
the runner, so the .NET 10 SDK form applies: a named target and no `--` separator.

```bash
dotnet restore ViciOne.ServiceBus.Tests.Unit.slnx /bl:{}
dotnet build   ViciOne.ServiceBus.Tests.Unit.slnx -c Release --no-restore --no-incremental /bl:{}
dotnet test    --solution ViciOne.ServiceBus.Tests.Unit.slnx -c Release --no-build --no-restore
```

The test run is unfiltered and its exit code is the verdict. Skipped tests and warnings are failures
through the single `tests2/xunit.runner.json`, and zero discovered tests stays the platform's exit
code 8 rather than becoming success.

Three profiles exist: `ViciOne.ServiceBus.Tests.Unit.slnx` runs without external infrastructure and
is the profile the foundation checkpoint runs; `...Tests.LocalIntegration.slnx` and
`...Tests.External.slnx` carry no project yet and are expected to report exit code 8 until their
cohorts arrive.

Test configuration has one owner. Checked-in, secret-free defaults live in `tests2/testsettings.json`;
local overrides go into the single User Secrets store of the tree; CI supplies values through the
`VICIONE_TESTS__` namespace, where a double underscore separates configuration levels, for example
`VICIONE_TESTS__LocalInfrastructure__RabbitMqPort=5673`. Cloud credentials are never part of this:
Azure authenticates through the `Azure.Identity` chain and AWS through the AWS SDK provider chain.
