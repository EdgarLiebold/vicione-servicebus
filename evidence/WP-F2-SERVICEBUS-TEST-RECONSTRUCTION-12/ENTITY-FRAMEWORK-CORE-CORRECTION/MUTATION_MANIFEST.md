# Entity Framework Core retry-state mutation evidence

All six probes start from technical commit `6632dfa2017348f94a9f18e35bb2a08415dadc77`, tree
`e3a039081f9a7e96917e1f08a3b82dea21627096`, in the isolated detached worktree
`/private/tmp/vsb-ef-mutation-worktree`. The Lead Architect alone started every .NET/MSBuild process.
Each probe built from locked restored assets, executed through the native xUnit 4 / Microsoft Testing
Platform 2 entry point, failed for its named behavioral reason, and was restored before the next
probe. Exact replacement bytes, baseline and mutant hashes, result counts, result paths and the
restore closure are machine-readable in `MUTATION_EXECUTION.json`.

## Mutations

- **M01 — failed EF attempts retain ChangeTracker state.** Both generic and non-generic retry owners
  fail when the catch-path clear is neutralized. Result: 53 total, 51 passed, 2 failed, 0 skipped,
  exit 2.
- **M02 — an outer transaction receives a second retry boundary.** Reversing the outer-transaction
  branch makes the real factory path enter two provider strategy executions. Result: 53 total,
  52 passed, 1 failed, 0 skipped, exit 2.
- **M03 — a failed retry attempt retains its in-memory outbox tail.** Removing checkpoint rollback
  makes the real PostgreSQL retry publish two effects instead of one. Result: 1 failed, 0 skipped,
  exit 2.
- **M04 — initial inbox persistence drops caller cancellation.** Removing the consume token from the
  first save makes one of seven observed persistence calls carry the wrong token. Result: 1 failed,
  0 skipped, exit 2.
- **M05 — a checkpoint from another outbox is accepted.** Removing owner validation crosses the
  public boundary and yields the wrong failure type before any foreign actions may be discarded.
  Result: 1 failed, 0 skipped, exit 2.
- **M06 — successful attempts are cleared.** Moving the clear into the success path removes state
  still owned by the caller. Result: 1 failed, 0 skipped, exit 2.

M03 and M04 used fresh run-scoped PostgreSQL fixtures (`vicione-b56b254a0f6b` and
`vicione-f8d4db13cbdf`). M01, M02, M05 and M06 are hermetic UnitArchitecture probes. The raw test
logs, CTRF reports where requested, Release build logs and binlogs are retained in `mutations/` and
bound by `SHA256SUMS`.

## Restore closure

After M06 the mutation worktree was restored to commit `6632dfa2017348f94a9f18e35bb2a08415dadc77`.
The three repeatedly mutated baseline files match their recorded SHA-256 values, `git status --short`
is empty, and the worktree tree remains `e3a039081f9a7e96917e1f08a3b82dea21627096`.
