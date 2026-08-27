# Entity Framework Core correction 03

## Subject

Technical commit `36fd3460421b45e475cd190873667afca5697a29`, tree
`50205a748f4ea8a1f971c8c163129b87ff7f45a5`, direct parent
`a2555f142c3f76e054d8de3f8d9adbfac4a73968`.

This append-only correction closes the two findings raised by the independent final reviews of
correction 02. It does not rewrite that accepted evidence or its unchanged 90-to-156 provider
disposition.

## Product guarantee

When rollback of a failed in-memory outbox attempt is incomplete, the retry delegate now throws a
private stop sentinel rather than presenting the original transient business exception to the
provider again. The outer Entity Framework execution-strategy boundary retains both the original and
cleanup failures and rethrows the original failure through its captured exception dispatch info,
even if a strategy retries the sentinel, wraps it in `RetryLimitExceededException`, or consumes it.
Caller cancellation preserves the exact original `OperationCanceledException` instance and token.

The source-mirrored test uses a real `InMemoryOutboxConsumeContext<T>`, a real
`MessageSchedulerContext`, a schedule created after the checkpoint, a failing
`CancelScheduledSend`, and a retry-limiting strategy double that wraps its last failure. It proves one
business attempt, one failed cleanup attempt, no second business entry, the exact original exception,
and a schedule which remains recoverable by final cleanup.

## Results

- Locked Engineering restore: exit 0.
- Engineering Release build: exit 0, zero warnings, zero errors.
- UnitArchitecture: 1886/1886, zero failed, zero skipped.
- Focused Entity Framework UnitArchitecture: 55/55, zero failed, zero skipped.
- LocalIntegration against fresh run-scoped PostgreSQL and Azurite: 70/70, zero failed, zero skipped.
- Focused Entity Framework LocalIntegration against fresh run-scoped PostgreSQL: 59/59, zero failed,
  zero skipped.
- M12 and M13: build exit 0; exactly one selected test executed and failed for its own expected
  reason; test exit 2; zero skipped; both source files restored byte-for-byte to the technical tree.

`SHA256SUMS` binds every evidence artifact except itself. `MUTATION_MANIFEST.json` binds the exact
target, baseline hash, patch, mutant hash, command, causal result and post-restore hash for each new
mutation.
