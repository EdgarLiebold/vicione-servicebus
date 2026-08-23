# Build and test

This repository requires exactly the .NET SDK pinned by `global.json`. NuGet sources are closed and
mapped by `NuGet.config`; no machine-level feed or credential participates.

## Build surfaces

| Target | Purpose |
|---|---|
| `ViciOne.ServiceBus.slnx` | inherited product and inherited tests during the replacement period, plus compile-verified samples |
| `ViciOne.ServiceBus.Engineering.slnx` | benchmarks, diagnostics, samples, product dependencies, and every materialized native-test project |
| `ViciOne.ServiceBus.Tests.Unit.slnx` | current hermetic unit and architecture profile |
| `ViciOne.ServiceBus.Tests.LocalIntegration.slnx` | tests against real resources of the local host |

A native profile solution is created only with its first executable cohort. The external profile is
therefore not materialized yet. Empty solution files are not valid test runs and must never be used
as zero-test sentinels.

## Locked restore and Release build

Every project tracks `packages.lock.json`, and locked mode is the default:

```bash
dotnet restore ViciOne.ServiceBus.slnx --locked-mode
dotnet restore ViciOne.ServiceBus.Engineering.slnx --locked-mode
dotnet restore ViciOne.ServiceBus.Tests.Unit.slnx --locked-mode
dotnet restore ViciOne.ServiceBus.Tests.LocalIntegration.slnx --locked-mode

dotnet build ViciOne.ServiceBus.slnx -c Release --no-restore --no-incremental
dotnet build ViciOne.ServiceBus.Engineering.slnx -c Release --no-restore --no-incremental
dotnet build ViciOne.ServiceBus.Tests.Unit.slnx -c Release --no-restore --no-incremental
dotnet build ViciOne.ServiceBus.Tests.LocalIntegration.slnx -c Release --no-restore --no-incremental
```

Only an intentional package update may rewrite lock files:

```bash
dotnet restore <target> -p:RestoreLockedMode=false --force-evaluate
```

Review every resulting lock-file change. Test-only central package versions are conditioned on
`ViciOneNativeTestTree`; they must not alter a product project's resolved graph.

## Native xUnit/MTP tests

`global.json` selects Microsoft Testing Platform. With the pinned .NET 10 SDK, run a solution using
the native MTP command form and no VSTest argument separator:

```bash
dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx \
  -c Release --no-build --no-restore \
  --results-directory artifacts/test-results/unit --minimum-expected-tests 846

VICIONE_TESTS__Profile=LocalIntegration \
dotnet test --solution ViciOne.ServiceBus.Tests.LocalIntegration.slnx \
  -c Release --no-build --no-restore \
  --results-directory artifacts/test-results/local-integration --minimum-expected-tests 3
```

The unfiltered process exit code is the verdict. `tests2/testconfig.json` turns skips and warnings
into failures, and MTP copies it into the artifact as `<AssemblyName>.testconfig.json`. Every
executable test project sets `UseMicrosoftTestingPlatformRunner=true` and references exactly one entry package,
`xunit.v3.mtp-v2`; NUnit, VSTest, test loggers, and ArchUnitNET framework adapters are forbidden in
the native tree. Architecture rules use `TngTech.ArchUnitNET` core with ordinary xUnit assertions.
The minimum count is the predeclared floor for the currently materialized profile. It is updated as
part of an accepted cohort, never inferred from the result of the run it is meant to protect. The
current Unit floor includes the architecture foundation, all currently reconstructed Abstractions
behavior, the complete inherited Analyzer and CodeFix behavior, direct tests of the projection
verifier, project-driven architecture Theory rows, the complete hermetic SignalR backplane behavior,
MessagePack serialization and transport behavior, the state-machine visualizer, Cron scheduling,
endpoint-name formatting, message-URN contracts, and deterministic request-rate behavior. The
profile also proves that a public one-dimensional array can be published and consumed as one
ordered message contract; this compatibility feature is not a batch abstraction. Future-location
URI round trips and their malformed-input boundaries are also part of the profile. The
TaskExecutor and TaskUtil cohorts add deterministic concurrency, cancellation, synchronous-wait,
completion-source, and validation contracts without timing-based behavior assertions. The task
initializer cohort adds type-safe reference and nullable-value projections, constant and lazy
synchronous/asynchronous fallback contracts, exact source-state propagation, and strict public
input boundaries. Header-initializer convention tests verify standard and typed custom headers on
the real in-memory publish path, with exact TTL state rather than wall-clock tolerance. The
state-property converter test proves integer-backed post-transition state-name projection and
custom-header delivery through a real in-memory state-machine publication. Scalar initializer
tests cover exact copies, nullable wrapping and unwrapping, invariant round-trip strings, enums,
object identity, and value-type-to-string conversion through the public initialization pipeline. The
cache cohorts cover bucket retention, direct insertion, factory arbitration, multi-index propagation,
clear/reuse, and truthful atomic removal without sleeps or wall-clock assertions. The production
endpoint-resource cache additionally proves deterministic single-flight recovery, capacity and
usage-aware retention, exact hit accounting, tracker churn, and TTL behavior through an injected
`TimeProvider`. The native core test project has a distinct SDK artifact identity so restoring the
remaining same-named NUnit project cannot overwrite its resolved package graph. The
LocalIntegration floor is independent and includes only host-resource tests in that profile.

It is a floor, not a completeness proof. Exact cohort membership is protected separately by durable
requirement projections under each owning test project. The framework-neutral verifier in
`ViciOne.ServiceBus.Tests.Infrastructure` compares each projection with passive metadata compiled
into the test assembly, returns deterministic diagnostics, and never owns a verdict. Each cohort
uses one ordinary xUnit assertion over those diagnostics.
Input the verifier cannot read at all - a wrong root, an unknown field, a non-canonical value, a
projected type that does not exist - throws instead, because reporting "no differences" over
unreadable input is how a broken projection passes.

Compile-time examples live under `samples`, never under a test project. Every sample is non-packable,
has a tracked lock file, and belongs to the Engineering solution. Native architecture tests enforce
both the delivery boundary and Engineering membership.

The nested `tests2/Directory.Build.props` and `.targets` import the root contract explicitly because
MSBuild otherwise imports only the nearest directory file. Their build errors are intentional:

| Code | Refuses |
|---|---|
| `VOSBT001` | missing root `Directory.Build.props` import |
| `VOSBT002` | missing root `Directory.Build.targets` import |
| `VOSBT003`–`VOSBT005` | dishonest or duplicate executable-test classification |
| `VOSBT006` | a forbidden direct/global test package |
| `VOSBT007`–`VOSBT009` | missing, duplicate, or noncanonical MTP configuration |
| `VOSBT010` | hybrid xUnit entry point instead of the required MTP-only runner |

All build output is under `artifacts/sdk`; no test output is written beside source files.

## Test configuration

`ViciOne.ServiceBus.Tests.Infrastructure` is the framework-neutral owner of both the typed test
configuration and the requirement-coverage projection verifier. It references no test
framework and no test platform, directly or transitively. Configuration precedence is:

1. secret-free `tests2/testsettings.json`;
2. the one shared User Secrets store for non-secret local resource coordinates;
3. `VICIONE_TESTS__...` environment values, with `__` as the hierarchy separator.

The profile is a closed enum. Local-integration runs validate host names and port ranges. External
runs must select at least one provider, require `Real` mode, validate its resource coordinates, and
fail before execution when configuration is incomplete. Azure credentials remain exclusively owned
by Azure.Identity; AWS credentials remain exclusively owned by the AWS SDK provider chain. Durable
credentials and connection strings never belong in this configuration contract.

## Product build contract

Root `Directory.Build.props` and `Directory.Build.targets` enforce locked restore, the common
artifact root, package notices, and supported target frameworks. Runtime, test, benchmark, and tool
projects target `net10.0`. Only the two Roslyn components and their source-free package surface use
`netstandard2.0`; only the two compiling Roslyn projects pin C# 14 because the older target does not
derive it from the SDK. The native-test marker is derived from the repository-relative project path;
projects cannot opt into test-only packages by setting the marker themselves.

Product projects emit full symbols in Debug and embedded symbols in Release. Native MTP test
applications emit portable PDBs in every configuration because xUnit/MTP discovery depends on that
format; the evaluated build-graph tests enforce the exception.

## Inherited verification stack

`tools/ci/**`, `build/verification/**`, and the test projects under `tests/**` are inherited
transition material. They remain available solely as behavior evidence until each cohort is replaced
and accepted. They are not the architecture, runner, inventory, completeness model, or final verdict
of the native test estate. New tests and gates must not extend that Python/VSTest/NUnit stack.

The Python policy validator (`tools/ci/policy_validator.py`, `tools/ci/policies/**`, and its self-test
suite) was a discarded Team 1 detour, not imported behavior. It is permanently deleted. Do not
reconstruct it. Any independently valid invariant is implemented once at its effective boundary: in
MSBuild for build-graph rules, or in native xUnit/MTP architecture tests for repository and test-estate
rules.

No inherited test is removed until every behavior obligation it owns has an accepted native
replacement in the correct profile. The original bytes remain recoverable through Git. A file whose
obligations are only partly replaced stays in full. For example, the abstractions MessageBody
obligations already have native replacements, while the same inherited source file still owns open
core, MessagePack, and cross-assembly obligations and therefore remains present.

## Diagnostics and benchmarks

Diagnostics under `tools/diagnostics` and benchmarks under `benchmarks` are engineering tools, not
product packages and not correctness verdicts. Benchmark follow-up work is tracked in its own
[`benchmarks/ToDo.md`](../benchmarks/ToDo.md); it must not be represented as test coverage.
