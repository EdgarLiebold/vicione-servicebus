# Iteration 11 Validation

## Scope

Iteration 11 corrects four related cancellation ownership defects:

- generic and non-generic timeout wrappers preserve caller cancellation and the exact token instead
  of reporting a timeout;
- job cancellation passes the caller token through the job-completion wait and suppresses only
  job-owned cancellation;
- ActiveMQ queue and topic deletion pass the caller token into bounded executor admission; and
- state-machine saga polling passes the public operation token into its provider-backed delay.

The production behavior is covered at the real asynchronous boundary rather than only by
pre-canceled-token checks. ActiveMQ tests deliberately occupy the worker and all thirty-two bounded
queue slots before canceling a waiting delete operation.

The fresh-package run refreshed only ViciOne package `contentHash` values in the five package
consumer lock files. A zero-context diff scan confirmed that versions, dependency graphs, requested
ranges, and third-party package entries did not change.

## Red/green evidence

Before implementation, the timeout profile failed four of seven cases because both generic and
non-generic paths produced `TimeoutException` for pre-canceled and in-flight cancellation. The job
profile failed one of eight cases for the same lost caller outcome. Both saturated ActiveMQ
partitions completed and executed the supposedly canceled broker operation, and the state-poll
timer remained active after cancellation.

After implementation, all eight new behavior partitions passed. Genuine virtual-time timeout and
successful generic completion cases remained green, preserving the distinction between deadline,
cancellation, and successful completion.

## Mutation evidence

Five isolated regressions were introduced and removed:

1. Restoring timeout translation for the non-generic pre-canceled path was killed by the exact
   exception type and token assertion.
2. Removing the in-flight caller-token check from the non-generic timeout path was killed by the
   corresponding cancellation partition.
3. Omitting the caller token from the job-completion wait was killed by the job-handle test after
   virtual time reached the configured deadline.
4. Replacing the ActiveMQ topic deletion token with `CancellationToken.None` was killed only by the
   topic theory row while the queue row stayed green, proving branch specificity.
5. Replacing the state-polling delay token with `CancellationToken.None` was killed by the active
   timer ownership assertion.

Every mutation was restored before validation.

## Test-quality review

The tests avoid arbitrary sleeps. Timeout and job cases use `FakeTimeProvider`; state polling uses an
observable virtual timer; ActiveMQ uses explicit worker-entry and worker-release signals around the
real bounded `TaskExecutor`. Assertions cover exception category, exact cancellation token, absence
of the canceled broker side effect, timer disposal, and preservation of the job-owned cancellation
signal. Each new test has an exact requirement projection entry.

## Repository validation

| Gate | Result |
|---|---|
| Unit/Architecture solution Release build, warnings as errors | PASS — 0 warnings, 0 errors |
| Complete Unit/Architecture profile | PASS — 3,826 passed, 0 failed, 0 skipped |
| Engineering solution Release build, warnings as errors | PASS — 0 warnings, 0 errors |
| Fresh-package gate, normal comparison mode | PASS — 18 journeys, 30 packages, 3 executable isolated consumers, 29 runtime package APIs |
| Packed public API contract | PASS — unchanged, 24,000 lines, SHA-256 `28e7a84a58a2cbdbac689f41e193c9f54cc827dcaef80d93701da341458fb0c6` |
| Engineering format verification at warning severity | PASS |
| Requirement projections and architecture manifests | PASS — included in complete architecture profile |
| Git whitespace validation | PASS |

This is internal engineering and adversarial-review evidence, not independent external acceptance.
