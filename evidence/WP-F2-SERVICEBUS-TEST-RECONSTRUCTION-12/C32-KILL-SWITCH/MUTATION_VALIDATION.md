# C32 mutation validation — kill switch

Date: 2026-08-25

Each mutation changed one production or projection decision, rebuilt the bounded project graph and
ran the owning native xUnit/MTP test. A mutation was accepted only when that test failed for the
injected cause. Product sources were restored through explicit reverse patches, never through a Git
reset or checkout.

| # | One-cause mutation | Required red signal |
|---:|---|---|
| 1 | exact activation comparison changed from inclusive to exclusive | exact activation case did not trip |
| 2 | exact ratio comparison changed from inclusive to exclusive | exact ratio case did not trip |
| 3 | configured exception-filter result inverted | excluded or matching exception affected the wrong counter path |
| 4 | tracking-window cutoff changed at the exact deadline | expired observation remained in the new window |
| 5 | single-owner state guard removed | 32 concurrent failures produced 32 pause attempts instead of one |
| 6 | delay before a failed pause retry removed | a second pause occurred before virtual time advanced |
| 7 | failed restart no longer returned to pause/retry | the switch remained stranded after the first failed start |
| 8 | terminal-stop recovery cancellation removed | advancing virtual time caused a late restart |
| 9 | caller ambient log context was not restored | the synchronous fault observer leaked the endpoint context |
| 10 | paused endpoints were omitted from host shutdown | stopping the bus allowed a zombie restart |
| 11 | endpoint identity guard inverted | a second endpoint identity was accepted |
| 12 | recovery-owned log-context establishment removed | pause/restart executed in the foreign context |
| 13 | minimum restart delay boundary changed | the exact one-second public boundary was rejected |
| 14 | successful restart entered `Running` instead of `VerifyingRecovery` | `MatchingFailureDuringRecoveryVerification_RetripsImmediately` failed with expected `VerifyingRecovery`, actual `Running`; exit 2 |
| 15 | recovery stabilization changed from `>= ActivationThreshold` to `>` | `SuccessfulDeliveriesDuringRecoveryVerification_ReturnToRunningAtExactActivationBoundary` retained 3/3 counters in `VerifyingRecovery`; exit 2 |

Mutation 9 initially survived because the test awaited an already-completed observer task inside an
async method and therefore observed ambient context after a continuation boundary. The test was
corrected to invoke the synchronous observer directly, require an already completed task and inspect
the caller context immediately. The same mutation then failed. This was a test-quality correction,
not a product accommodation.

The first thirteen mutations restored the pre-verification sources to their recorded hashes. The
last two restored the final sources to:

- `KillSwitch.cs`: `da102d69b2fac3f7cadbcd1f70f129a41c67b3d3cae40c35d86d14ea38f07a24`;
- `KillSwitchTests.cs`: `568301ffa6d62322823156f1ee94a613784502354f35d71e19cd55d699f6599d`;
- `KillSwitchTestDriver.cs`: `53ce71b80c7cf1f39193aaa0f3f1f08f779d7ce4faf3d55faf4161e2ee34c866`.

Result: 15/15 effective mutations, no surviving mutation, and byte-exact final restoration.
