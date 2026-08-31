# Core state-machine exception and observation native closure

## Frozen subjects

- State-machine Technical commit: `fc818a1ac037746ab15918c3d895bd2feb22ffcd`
- State-machine Technical tree: `e5f7414f9e3825ea4c18d65c5623e3f817216afd`
- Direct Technical parent: `43d5d5b96041f8c946ae664fcf268bca3f842e11`
- Diagnostic test-oracle correction: `9eb4cdc6f14481ba7868febd5ebff9aba58b0f5a`
- Diagnostic correction tree: `57a35031b4dec1088b9512ef9153c19947989b30`

`TECHNICAL.patch.gz` binds only the state-machine Technical delta. `JOB_ORACLE_CORRECTION.patch.gz`
binds the one-file diagnostic correction, and `FULL_TECHNICAL.patch.gz` binds the complete range from
the parent through that correction. `TECHNICAL_SCOPE.json` enumerates every changed path.

## Closure and retirement

- The terminal map contains exactly 102 unique inherited obligations. Every row is
  `REPLACED_EXECUTING` in `UnitArchitecture` and resolves to one of the native owners.
- Nine xUnit owner methods materialize 16 executable cases: six observation cases and ten exception
  cases across both declarative and dynamic construction styles where that distinction is relevant.
- The carriers assert complete transition/event sequences, exact exception identity and type,
  catch-pipeline continuation/termination, data-event payloads and selected-event filtering. They do
  not use sleeps, absence-only success, or wall-clock timing as a correctness oracle.
- Four fully replaced inherited files and 1,861 legacy lines are deleted atomically. The
  state-machine Technical delta is +943/-1,866, a net reduction of 923 lines while the executable
  floor rises from 2,581 to 2,597.
- The two containing legacy directories remain intentionally present because they still contain 35
  and 26 tracked test files. No empty directory remains, and the frozen historical expected list is
  unchanged.

## Positive execution

```text
/usr/local/share/dotnet/dotnet restore ViciOne.ServiceBus.Engineering.slnx --locked-mode --disable-build-servers /p:NuGetAudit=false /bl:<technical-engineering-restore.binlog>
/usr/local/share/dotnet/dotnet build ViciOne.ServiceBus.Engineering.slnx --configuration Release --no-restore --disable-build-servers /m:1 /nr:false /p:UseSharedCompilation=false /p:NuGetAudit=false /bl:<technical-engineering-build.binlog>
/usr/local/share/dotnet/dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx --configuration Release --no-build --no-restore --results-directory <run-root> --minimum-expected-tests 2597 --max-parallel-test-modules 1 --fail-skips on --report-xunit-ctrf --parallel none /bl:<unit-test-dotnet-test.binlog>
```

- Focused state-machine execution: 16/16, zero failed/skipped.
- Complete UnitArchitecture: 2,602/2,602 across 21 CTRFs, zero
  failed/skipped/pending/other, above the fail-closed floor of 2,597.
- Requirement projection: 1/1; the separate 102-row terminal map binds every inherited obligation.
- Locked Engineering restore, both Engineering Release builds, Unit solution build, correction build
  and scoped format verification are successful. Build diagnostics report zero warnings/errors.
- The post-mutation focused build exits zero and its 16/16 control uses byte-restored product files.

## Diagnostic correction exposed by the full profile

The first complete Unit run was 2,601/2,602. The only failure was the pre-existing
`NamedRecurringJobs_KeepDistinctStableIdentitiesAcrossRunsUpdatesAndNoOpUpdates` test. It compared
the next occurrences of `*/2` and `*/10` schedules sharing the same start, although both schedules
can legally select the same next second. The unchanged test passed when immediately repeated,
confirming that the inequality was not a deterministic product invariant.

The direct correction commit changes only that test method. It gives the update and no-op an update
start twelve hours after the original start and independently proves the original next occurrence is
before that boundary, the updated occurrence is after it, and the identical no-op preserves it. A
single control is 1/1 and the complete final profile is 2,602/2,602. Product bytes and test count are
unchanged by this diagnostic correction. `JOB_ORACLE_CORRECTION.json` binds all four observations.

## Causal mutation closure

M01-M10 each replace one exact product-code occurrence against frozen commit `fc818a1a`. The
manifest records the independent literal occurrence count and selected occurrence index; this is
material for the repeated catch, selected-event and state-observer statements. Reconstructing each
mutant from the frozen blob yields the recorded mutant SHA-256.

Every mutant builds with zero errors and executes the complete two-case owner theory. All 20 mutated
cases fail, with no skip; the paired construction styles therefore cannot hide a one-style survivor.
The ten axes cover nonmatching catch propagation, matched catch execution for data and non-data
events, post-catch continuation, state-change publication, selected-event Pre/Post filtering,
state-event Post publication and both transition lifecycle events. After restoration all five
product-source hashes match the frozen Technical bytes, the focused build succeeds, and 16/16 pass.

## Repository and environment gates

- CI tool self-tests: 257/257. Their initial sandbox run failed only where macOS denied `ps`; the
  identical command outside that sandbox boundary is the canonical green run.
- Identity self-tests: 148/148 after the complete Evidence inventory and generated CHANGELIST are
  staged.
- Verification Model: PASS.
- Generated CHANGELIST: PASS after the Evidence inventory is complete.
- Binary-log inspection records successful restore/build results rather than inferring success from
  file presence. CTRF summaries are the primary test-result evidence; the outer `dotnet test`
  binlog is retained as invocation evidence and is not misrepresented as an MSBuild build summary.

No cloud fixture was required. No remote push was performed. Independent read-only acceptance,
architecture binding and any later remote publication remain separate actions.
