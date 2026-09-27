# T44: EF reliable-store contracts — in progress

Base: `0b1abbf775e1a2946deda0ec7699266e47d936f2`.
This packet remains open. No new product-wide coverage result or A+ completion
is claimed. Following user feedback, all five areas below form one coherent
iteration; run one full 33-profile measurement after the complete packet.

Microsoft code-testing-agent focused workflow, run-tests and the existing bounded
research/pairing are used. Implementation is manual. The new cases use actual
SQLite storage, public store operations and fresh persisted-state reads.

## Requirement mapping

All methods below belong to `EntityFrameworkReliableStoreTests`, with matching
entries in `EntityFrameworkRequirements.json`.

| Requirement | Exact test method | Cases |
| --- | --- | ---: |
| Exclusive composite inbox pagination across timestamp/message/consumer ties and store restarts | InboxPagination_TraversesTimestampMessageAndConsumerTiesAcrossRestartsAsync | 3 |
| Saved cursor survives removal/requeue and excludes a newer insertion | InboxPagination_SavedCursorSurvivesRemovalAndNewerInsertionAsync | 2 |
| Failed initialization preserves other storage and releases the same instance for retry | InitializationFailure_PreservesOtherStoreAndAllowsSameInstanceRecoveryAsync | 1 |
| Admission failure rolls back an already incremented capacity ledger | AdmissionSaveFailure_RollsBackCapacityAndRetriesExactIntentAsync | 1 |
| Delivery save failure rolls back an already decremented capacity ledger and preserves the lease | DeliverySaveFailure_RollsBackCapacityAndPreservesLeaseForRetryAsync | 1 |
| Explicit schedule due time overrides caller input without modifying it; restarted delivery respects the exact boundary | Schedule_OverridesDueWithoutChangingCallerAndDeliversAtExactBoundaryAfterRestartAsync | 1 |
| Stale or removed delivery/retry/quarantine transitions preserve records, neighbors and capacity | DurableTransition_WithStaleOrRemovedLeasePreservesRetainedStateAsync | 6 |
| Oversized admission preserves existing storage; the exact byte boundary remains usable | Admission_OversizedIntentPreservesStorageAndExactBoundaryRemainsUsableAsync | 1 |
| Inconsistent count/byte ledgers prevent deletion and allow explicit repair and retry | Delivery_InconsistentLedgerPreservesRecordsAndAllowsRepairAsync | 2 |

## Current evidence and open work

- Pagination baseline: `artifacts/t44-pagination.log`, 26 passed, no skips.
- Added failure atomicity: `artifacts/t44-failure-atomicity.log`, 29 passed, no skips.
- Initial scheduling/ownership build failed on the incorrect test enum name
  `Permanent`; manually corrected to the existing `NonRetryable` value.
- First combined packet run: `artifacts/t44-store-packet.log`, 37 passed,
  2 failed. Both failures were test-oracle mistakes: structural Uri comparison,
  and attempting to read an intentionally inconsistent ledger through the valid
  snapshot contract. Assertions were manually corrected to explicit message
  fields and a fresh direct ledger read. No product change was necessary.
- Corrected combined run: `artifacts/t44-store-packet-corrected.log`, terminal
  exit 0, 39 passed, no failures or skips (18 additional cases over T43).
- Bounded read-only reviews found no concrete static defect in the new areas.
  The runtime failures above show the limits of those reviews. Review of the
  corrected assertions found no remaining issue in that bounded diff.
- MAIN full project: `artifacts/t44-full.log`, exit 0, 340 passed, no skips.
  The subsequent test-only whitespace correction splits three initializer
  properties onto separate lines; it changes no behavior.
- GATE restored full control: `artifacts/t44-restored.log`, terminal exit 0,
  340 passed, no failures or skips. Corrected verify-only whitespace check:
  MAIN `artifacts/t44-format-corrected.log`, exit 0.
- Final bounded read-only evidence review confirms the five failure oracles,
  restored byte identity and all nine manifest bindings; no concrete blocker.
- Still open: canonical CHANGELIST,
  implementation commit, one exact-commit full measurement and independent
  evidence audit, final documentation and authorized push.

## Isolated counterprobes

Only GATE `/private/tmp/servicebus-reply-investigation` receives deliberate
product mutations. Each is manually restored before the next one. MAIN product
source remains unchanged.

- Inclusive inbox consumer cursor: `artifacts/t44-mutant-cursor.log` in GATE,
  exit 2, all five new pagination cases fail, 34 controls pass.
- Ignore explicit schedule due time: `artifacts/t44-mutant-schedule.log` in GATE,
  exit 2, scheduling boundary case fails, 38 controls pass.
- Omit lease predicate in TryGetOwnedAsync: `artifacts/t44-mutant-lease.log`,
  exit 2, three new stale-transition cases and one existing lifecycle case fail,
  35 controls pass.
- Commit capacity decrement before record SaveChanges: `artifacts/t44-mutant-atomicity.log`,
  exit 2, delivery failure-atomicity case detects persisted ledger inconsistency,
  38 controls pass.
- Cache initialization before a successful save: `artifacts/t44-mutant-initialization.log`,
  exit 2, same-instance initialization recovery fails, 38 controls pass.

All five mutations were manually restored; source, test and manifest compare
byte-identically between MAIN and GATE. Restored full control passes 340/340.

## Evidence limits

SavingChanges fault injection happens before the record INSERT/DELETE command.
It proves rollback of the already executed ledger change and preservation of
records, not rollback of a DELETE that already executed or a commit failure.
The inconsistent-ledger cases require explicit repair and do not establish
automatic repair. SQLite evidence does not prove PostgreSQL/SQL Server isolation
or GUID ordering. Unique leases may mask removal of individual identity filters;
no universal mutation-detection claim is made. Scheduling proves persisted store
delivery eligibility, not actual broker delivery. Existing green cases do not
make the product-wide A+ goal complete.
