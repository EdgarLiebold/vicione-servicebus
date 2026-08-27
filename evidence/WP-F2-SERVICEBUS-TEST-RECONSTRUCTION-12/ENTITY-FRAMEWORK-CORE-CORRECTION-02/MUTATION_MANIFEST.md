# Entity Framework Core and in-memory outbox correction mutations

The five probes start from technical commit
`6bf471d954fb50138473cb1f2c0275362df5aebe`, tree
`098f6dcb9bae0c69f626d3cecbeb41cd91327738`, in the isolated detached worktree
`/private/tmp/vsb-ef-mutation-worktree`. The Lead Architect alone started all .NET and MSBuild
processes. Each mutation built in Release with zero warnings and zero errors, then exactly its
named native xUnit 4 / Microsoft Testing Platform 2 test failed. No skip, timeout-as-oracle, or
absence-only assertion was used.

`MUTATION_EXECUTION.json` is the machine-readable contract. For every mutation it binds the exact
target path, baseline bytes, unique old and new text, resulting mutant hash, complete build and test
argument vectors, exits, raw CTRF result and post-restore closure. The build binlog and raw CTRF are
bound by `SHA256SUMS`.

## Probes

- **M07 — outer scheduler rollback delegation removed.** The real outer
  `OutboxContext.CreateCheckpoint()` / `DiscardPendingActions(checkpoint)` path no longer delegates
  to the scheduler checkpoint. The Batch carrier fails because the post-checkpoint token was not
  canceled.
- **M08 — failed rollback no longer blocks provider retry.** Removing the retry guard permits a
  second business execution after rollback cleanup failed. The carrier observes two attempts
  instead of one and protects the identity of the original failure.
- **M09 — failed unschedule is forgotten.** Removing a scheduled item in a `finally` block makes a
  failed cancellation disappear from the final cleanup set. The carrier sees one cancellation
  attempt instead of two.
- **M10 — child checkpoints omitted.** A Batch checkpoint without its child checkpoint vector is
  rejected as incomplete by the real public rollback path.
- **M11 — child rollback omitted.** Rolling back only the Batch parent leaves post-checkpoint child
  sends executable. The carrier fails on the exact retained/discarded action sequence.

## Restore closure

After M11 the mutation worktree was restored byte-for-byte to technical commit
`6bf471d954fb50138473cb1f2c0275362df5aebe`. The three mutated product files match the baseline
SHA-256 values recorded in `MUTATION_EXECUTION.json`; `git status --short` is empty and the worktree
tree remains `098f6dcb9bae0c69f626d3cecbeb41cd91327738`.
