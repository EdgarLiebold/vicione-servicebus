# T43: EF Core inbox ownership and missing-record transitions

Base: `1ce9afe67276a995d5cf8414175ad53267da5d7d`.
Focused Microsoft code-testing-agent workflow, test-gap-analysis, assertion-quality
and run-tests with their .NET guidance. Production store, public IInboxStore and
the existing store tests were read before test changes. No production change.

The Microsoft Roslyn pairing analyzer ran on byte-identical bounded source/test
copies with their project files under `/private/tmp/servicebus-t43-pairing`.
It classified one source and one test and paired EntityFrameworkReliableStore.cs
to DurableSend/EntityFrameworkReliableStoreTests.cs. This is a static pairing
heuristic, not evidence of runtime coverage. Analyzer execution exited 0; its
trim/AOT warnings did not prevent the parse-only report.

## Behavior evidence

| Requirement | Exact test method | Cases |
| --- | --- | ---: |
| Stale consumer completion, retry and quarantine cannot alter a reclaimed owner or neighboring records | Inbox_StaleTransitionPreservesCurrentOwnerAndNeighborsAsync | 3 |
| Consumer transitions after supported removal return false, never recreate the target and preserve neighbors | Inbox_RemovedTransitionReturnsFalseWithoutResurrectionOrNeighborChangesAsync | 3 |

Both methods belong to EntityFrameworkReliableStoreTests and have matching
REQ-VSB-EF-RELIABLE-INBOX manifest entries. They use real SQLite persistence and
the public store operations. There are no mocked DbSets or reflected private calls.

Stale cases prove initial acquisition, exact expiry, reacquisition at expiry,
attempt 2, changed lease token and the persisted current owner. Each old operation
must throw the exact EF-store ownership exception. Fresh untracked reads compare
ordered complete keys and every record field before/after the rejected operation.
Current-owner completion must persist Consumed, the exact timestamp, attempt 2
and cleared lease; a later acquisition must report AlreadyConsumed with no lease.

Missing cases first quarantine and discard the target through supported operations,
verify the exact Discarded result and actual absence, then create a new store
instance. Each old consumer operation must return false, with all remaining rows
unchanged. Both test families include same-message, same-consumer and other-store
neighbors. Fixed logical timestamps avoid sleeps and scheduling-dependent expiry.

Inline assertion review found neither assertion-free nor trivial-only tests.
Both methods verify negative behavior, persisted state and complete structure;
the stale family additionally verifies the exception and successful current-owner
completion. State snapshots are fresh persisted data, not self-comparisons.

## Verification and counterprobes

- MAIN `artifacts/t43-focused.log`: 21/21, exit 0, no skips.
- MAIN `artifacts/t43-full.log`: 322/322, exit 0, no skips.
- MAIN `artifacts/t43-format.log`: verify-only whitespace formatting, exit 0.
- GATE `/private/tmp/servicebus-reply-investigation`, all deliberate faults exit 2:
  - `t43-mutant-owner.log`: treat stale owner as missing, 3 failures/18 controls.
  - `t43-mutant-missing.log`: report success for missing record, 3/18.
  - `t43-mutant-store-key.log`: omit store in existence query, 3/18.
  - `t43-mutant-consumer-key.log`: omit consumer in existence query, 3/18.
  - `t43-mutant-lease.log`: omit lease fencing from completion update, 1/20.
- Every deliberate source change manually restored. Product, test and manifest
  compare byte-identically between MAIN and GATE. GATE `artifacts/t43-restored.log`:
  restored full control 322/322, exit 0, no skips.

Bounded read-only plan, implementation and final evidence reviews found no
concrete blocker. All five failure oracles and the restored control were verified.
Exact-commit measurement at `a763757b48083befb64cd8e3e7ff3c39304b819f` passes
all 33 profiles and 12,929 executions. RequireOwnedOrMissingAsync reaches 9/9
lines, 4/4 conservative branches and CRAP 4. See
[full measurement and remaining gaps](product-wide-profile-a763757b4.md).

## Limits

The existing EF implementation passes the new tests; this packet strengthens
regression protection and does not claim to fix a reproduced product defect.
Exact exception/missing semantics are established for this implementation;
IInboxStore describes the broader fencing/bool contract. SQLite evidence does not
establish concurrent PostgreSQL/SQL Server isolation. Different neighbor lease
tokens can mask removal of individual update identity predicates; no universal
mutation kill claim is made for those predicates. All other gaps remain open.
