# T92 — durable delivery cancellation and timeout race

Exact test commit: `1c1daf17d7573f2e30dd8cf3edf1891f38ac2ff3`.

## Product contracts checked

`DispatchCancellation_RetainsTheIntentForDeliveryAfterTheLeaseExpiresAsync`
proves that cancellation during transport dispatch propagates the caller's
token, leaves the durable intent leased rather than removing it, and replays
the same attempt number after the lease expires. The second dispatch retires
the intent.

`StateTransitionCancellation_AfterAcceptedDispatchReplaysThePersistedIntentAsync`
proves the same retention and replay when cancellation arrives after transport
acceptance but before `MarkDeliveredAsync` can persist the terminal state. At
least once delivery can repeat an accepted dispatch; the attempt counter is not
persistently advanced by cancellation.

`ConsumerCompletion_DuringTimeoutQuarantineWinsWithoutFalseEvidenceAsync`
places a real consumer completion inside the store's timeout-quarantine call.
The completion succeeds, the quarantine transition loses its lease race, the
intent is removed, no quarantine entry is produced, and the scoped delivery
meter reports `awaiting-consumer-completion` followed by `delivered`, with no
false quarantined outcome.

## Verification

- Focused `DurableSenderDeliveryTests`: 20/20 passed, no failures or skips.
- Complete Core project on exact commit `1c1daf17d`: 6,963/6,963 passed,
  no failures or skips.
- An isolated dispatch-cancellation counterprobe returned instead of
  rethrowing. The new test failed because no exception was thrown.
- An isolated timeout-branch counterprobe treated `Pending` as the
  completion-wait state. Three tests failed, including the new race test,
  which observed a second dispatch. The product source was restored; its
  final Git diff is empty and the focused suite passed again.
- Read-only Red Team found three P2 assertion gaps: exact cancellation-token
  identity, unchanged attempt numbering on replay, and proof of the
  quarantine-race outcome in both store state and telemetry. All were fixed;
  independent re-review is PASS with no remaining P1/P2 finding.
- The three new requirement variants are projected in `CoreRequirements.json`.

## Measurement boundary

No product source changed. T85 remains the latest complete 33-profile
Line/Branch/CRAP checkpoint. Global A+ remains open; complete measurement
follows the agreed larger packet cadence.
