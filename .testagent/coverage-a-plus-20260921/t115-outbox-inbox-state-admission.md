# T115 — Outbox/inbox admission and state complexity

## Exact implementation and behavior

Implementation and test commit: `78f0f17b701fdce24d8d844370743a215be42bbc`.
Source tree: `0230be407c3400a3f35f8611502a91d042c9b447`.
Test tree: `1dfd0726941b53e58787450231bbd82b49907187`.

The reliable EF outbox formerly restored its capacity counters after a
tracking exception but left the rejected `DurableSendRecord` attached. A
subsequent `SaveChanges` could persist that send without session ownership.
The new test failed red-first for both empty and previously staged sessions:
the rejected record remained `Added`. Admission now detaches the exact
record. The test checks original exception identity, no rejected tracker or
durable row, exact capacity count/bytes, preservation of an earlier accepted
send, and a healthy subsequent send.

Adversarial review found three additional cleanup boundaries. A throwing
`StateChanged` callback masked the original exception and skipped capacity
restoration; a throwing `DetectingAllChanges` callback stopped enumeration
before detach; and a throwing `StateChanging` callback stopped detach before
the state transition. Each was proved by a red-first case. Cleanup now uses
`DbContext.Entry(record)` without a global DetectChanges enumeration,
restores capacity in `finally`, and retries selective detach once when a
pre-change callback throws. The once-failing callback preserves the original
exception and the earlier staged session. If the callback persistently
prevents detach, `ChangeTracker.Clear()` discards all tracked changes and
the staged session to prevent an orphan. That loss is explicit in a new
exception whose inner exception is the original admission failure. A test
proves that pending business data is discarded in this exceptional case,
no rejected send persists, and a fresh send can succeed.

Four complex functions were also separated by their product boundaries:
classic EF state ownership, delivery metadata and staged-message ownership;
reliable EF send ownership and capacity reservation; InMemory inbox
acquisition and leased consumer failure handling; and EF inbox dispatch and
transaction processing. The existing retry, quarantine, cancellation,
terminal-state and persisted-session tests remain in place. These are
source-level refactors of existing behavior, not tests added to touch lines.

## Tests and method risk

The five new behavior test methods have six variants:

- `TrackingFailure_DetachesRejectedSendAndPreservesHealthySessionAsync`
  (first and later send; both red-first)
- `DetachCallbackFailure_PreservesPrimaryErrorAndAllowsHealthyRetryAsync`
  (red-first)
- `DetectChangesFailure_CannotLeaveRejectedSendTrackedOrPersistedAsync`
  (red-first)
- `StateChangingFailure_CannotLeaveRejectedSendTrackedOrPersistedAsync`
  (red-first)
- `PersistentStateChangingFailure_ClearsRejectedTrackerAndAllowsRetryAsync`
  (fail-closed boundary)

The focused new variants pass **6/6**. In terminal-observed runs after the
implementation commit, the complete Core project passed **7,099/7,099** and
the complete EF unit project passed **400/400**, each with zero failures and
skips. The existing
relevant InMemory class passed 21/21 and the focused EF classes passed
71/71 during implementation. The new requirements are mapped in
`tests/Persistence/ViciOne.ServiceBus.EntityFrameworkCore.Tests/Requirements/EntityFrameworkRequirements.json`.

One direct Microsoft CodeCoverage run per affected unit project passed while
the source and test trees were still at the frozen commit, again
7,099/7,099 and 400/400 in terminal output. Its Cobertura XML
SHA-256 values are:

| Project | Cobertura SHA-256 |
| --- | --- |
| Core | `8da55a478d9e65fe775251236ace0e0baf601144288007d50e475667f1b3a8b0` |
| EF | `8fa7df50dd9c5c51d29778a7d33fd26276d65bbac7cbe10c304aa03ef0092a53` |

Merging method line hits by identity across these two reports, using the
repository's conservative method CRAP formula, yields the following maxima
for the four changed sources:

| Source | Lines | Conservative branches | Maximum CRAP | Above 30 |
| --- | ---: | ---: | ---: | ---: |
| Classic EF scoped outbox | 206/220 (93.64%) | 118/124 (95.16%) | 28.000 | 0 |
| Reliable EF scoped outbox | 265/293 (90.44%) | 130/154 (84.42%) | 27.981 | 0 |
| InMemory reliable inbox factory | 94/99 (94.95%) | 50/62 (80.65%) | 18.015 | 0 |
| EF reliable inbox factory | 219/248 (88.31%) | 89/114 (78.07%) | 22.003 | 0 |

These are affected-source measurements, not a complete product profile.
Two attempts at a fresh isolated receipt spent several minutes in restore
without yielding a test or report and were stopped; no T115 strict receipt
exists. The direct reports use the already built Release projects. Their
XML files contain the new helper methods and were generated while HEAD,
`src` and `tests` remained unchanged, but neither the XML nor a persisted
test log binds the tested binary cryptographically to that commit. The
7,099/400 counts are terminal-observed; no corresponding test logs were
saved for independent replay. T114 remains the
last complete 33-profile Line/Branch/CRAP checkpoint. The next product-wide
checkpoint must independently confirm that the five previous global
CRAP hotspots are gone and establish the new global Line and Branch rates.

## Quality review and limits

The Microsoft `code-testing-agent` guided the research/plan/test sequence;
`run-tests` supplied the .NET 10/MTP/xUnit v3 commands; `test-gap-analysis`
guided the executed counterexamples; `assertion-quality` checked exception,
tracker, ledger, durable-state and recovery assertions; and
`coverage-analysis` guided the targeted method-risk calculation. The
prior Roslyn `find-untested-sources` pairing was reused as a static pointer,
not treated as runtime coverage evidence.

Independent read-only Red Team review identified the callback and
DetectChanges failures, then passed the final product/test diff with no
remaining concrete P1/P2. The persistent callback case intentionally clears
all pending DbContext changes; callers must replay their work after the
explicit failure. The next global A+ measurement remains open.
