# Entity Framework Core correction 04

## Subject

Technical commit `2a3926b0bb906e7b89c5001a3301634414ac9acc`, tree
`41657e4d9ee0f472c5af5261f4c0e7f8c829dff6`, direct parent
`800dc09ae4f6c5df7b3058a9153c6e908f19a4a7`.

The complete correction-04 technical chain starts at the accepted correction-03 status commit
`3834dae854c7136e83b6eb56786b5a1dc3e37dc3`. `TECHNICAL_DIFF.patch.gz` binds that complete twelve-path
delta. This append-only correction supersedes the final-review conclusion of correction 03; it does
not rewrite any earlier evidence.

## Retry failure guarantee

An execution strategy may catch the private rollback-stop sentinel and return a value. The outer
Entity Framework boundary therefore checks the retry guard after an apparently successful strategy
return and rethrows the exact original business exception through its captured exception-dispatch
information. The new test uses an `IExecutionStrategy` that deliberately consumes the sentinel. It
proves one business attempt, one rollback attempt, one provider-strategy invocation, the exact
original exception instance and the cleanup failure retained inside the consumed sentinel.

M14 removes only that post-return projection. The product still builds, but the exact owner test
fails because no exception crosses the boundary.

## Cache cleanup guarantee

The unfiltered acceptance run exposed a real pre-existing `GreenCache` race: while an already queued
cleanup waited to acquire the tracker lock, later capacity signals were discarded. If the producer
stopped after that burst, the cache could remain above its capacity indefinitely. The repair keeps a
single scheduled cleanup, records a pending follow-up under the same lock, keeps the active bucket
bounded while the first pass waits and atomically reserves one follow-up when the first pass
completes.

The internal scheduler seam changes neither the public API nor runtime scheduling; production still
uses `Task.Run`. It permits a deterministic source-mirrored test to hold the first cleanup, produce a
complete burst, release the queue and prove convergence without another cache operation, sleep or
wall-clock oracle. M15 changes only the retained-signal assignment from `true` to `false`; the owner
then observes one cleanup instead of the required two and fails in 41 ms.

## Positive results

- Locked Engineering restore: exit 0.
- Engineering Release build: exit 0, zero warnings, zero errors.
- Unfiltered UnitArchitecture: 1888/1888, zero failed, zero skipped.
- Focused Entity Framework UnitArchitecture: 56/56, zero failed, zero skipped.
- LocalIntegration against fresh run-scoped PostgreSQL and Azurite: 70/70, zero failed, zero skipped.
- Focused Entity Framework LocalIntegration against fresh run-scoped PostgreSQL: 59/59, zero failed,
  zero skipped.
- M14 and M15: build exit 0; exactly one selected test executed and failed for its own expected
  reason; test exit 2; zero skipped; both source files restored byte-for-byte to the technical tree.

The inherited Entity Framework disposition remains unchanged: 90 obligations expand to 156
execution identities, of which 77 execute, 39 are provider-neutral consolidations and 40 SQL
Server/Azure SQL identities remain visibly `EXTERNAL_PENDING`. Pending work is not counted green.

## Fail-closed diagnostics

Two non-acceptance runs are retained and clearly separated by filename. The first rejected a stale
workflow floor after the new EF case was added. The second found the lost `GreenCache` cleanup signal
after that floor was corrected. Neither failed run contributes to the positive totals; each caused a
technical correction before the final restore, build and full test runs. Their exact meaning is
recorded in `FAIL_CLOSED_DIAGNOSTICS.md`.

`SHA256SUMS` binds every evidence artifact except itself. The compressed patch files preserve the
exact unified diffs without making their diff control prefixes appear as whitespace in the evidence
commit itself. `MUTATION_MANIFEST.json` binds the exact target, baseline hash, patch, mutant hash,
command shape, causal result and post-restore hash for M14 and M15.
