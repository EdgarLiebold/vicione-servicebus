# Core state-machine integration and policy native closure

Technical subject:

- parent: `1a3abdc2360f93e2d75f8fa49c96db9b04a98f29`
- commit: `008975a80c759d9ad0bcbaf9e5a9553ea5a217fb`
- tree: `d15899e4458505dcb76dbc4dd0c9fd371de226d1`
- scope: 64 paths, +3,817/-6,531 lines

## Replacement truth

The package closes 74 selected `OBL-R0-CORE-A` records. The committed mapping contains 69
`REPLACED_EXECUTING` records and five invalid inherited placeholders: two empty/commented parallel
tests, one contradictory comment/assertion, one graph-only print without an assertion and one
discarded/commented rescue body. Thirty-five native owner methods bind the executing records.

Forty-five fully replaced inherited files and 6,473 inherited test lines are removed in the same
Technical commit. The native executable floor rises from 2,661 to 2,698 and the complete run contains
2,703 cases. The empty `Dynamic Modify` directory is absent; the remaining `Automatonymous` directory
contains two tracked tests. `build/verification/expected/core.txt` is unchanged.

## Positive execution

The accepted post-commit commands use `/usr/local/share/dotnet/dotnet` outside the restricted macOS
process sandbox. That boundary is required because the same .NET/MSBuild process family can stall when
the sandbox denies process/named-pipe inspection; no source or project workaround is used.

```text
dotnet restore ViciOne.ServiceBus.Tests.Unit.slnx --locked-mode --disable-build-servers /bl:<restore-binlog>
dotnet build ViciOne.ServiceBus.Tests.Unit.slnx -c Release --no-restore --no-incremental --disable-build-servers /bl:<build-binlog>
dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx --configuration Release --no-build --no-restore --results-directory <dir> --minimum-expected-tests 2698 --max-parallel-test-modules 1 --report-xunit-ctrf --progress off --no-ansi
```

Results:

- locked restore: exit 0;
- Engineering Release build: exit 0, zero warnings, zero errors;
- complete UnitArchitecture: 2,703/2,703, zero failed/skipped/pending/other across 21 CTRFs;
- selected state-machine carriers: 73/73;
- requirement projection and executable floor: 2/2;
- post-mutation Release build: exit 0, zero warnings, zero errors;
- scoped Roslyn format verification: PASS.

## Mutation closure

`MUTATION_MANIFEST.json` binds 15 independent, buildable product mutations to the Technical bytes.
Every entry includes the exact old/new text, occurrence count and selected occurrence, baseline,
mutant and restored SHA-256, unified patch, build binlog, MTP CTRF and named owner. All 15 builds
finish with exit 0. The 18 executed owner cases are all causally red with MTP exit 2 and zero skips.

Two exploratory changes are excluded from the kill count: a wrong ActionActivity overload remained
2/2 green and a scheduler-subcontext-only change was behaviorally equivalent because the outer
outbox still owned the deferred schedule action. The executed `M07` and `M10` target the real owners.

All 14 distinct touched product files restore byte-for-byte to the Technical tree before the final
Release build. The deterministic scheduling oracle compares the exact provider-observed deadline;
the completion/removal oracles read the real `InMemorySagaRepository<T>` only after the corresponding
consume completion. No mutation kill relies on a sleep, an absence-only window or a harness helper
that can report stale removal state.

## Repository gates

The accepted CI self-test run is the identical out-of-sandbox invocation and passes 257/257. Identity
self-tests pass 148/148, the verification model reports PASS and the generated CHANGELIST validates.
The SHA manifest covers every Evidence file except itself. No remote push is part of this package.
