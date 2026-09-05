# Work package F verified mutations

This is targeted pseudo-mutation evidence for the new `IConsumerKind` boundary, not a repository-wide
mutation score. Each mutation changed one production condition or call, rebuilt the Release test
module, ran the two-test extension-point class without filtering individual test methods, and was then
restored. The final unmutated source passed the focused class and the complete profile three times.

| # | One-cause mutation | First result | Closure and final result |
|---:|---|---|---|
| 1 | Remove the default `ObserveCustomKind` call from harness observation | killed | Both tests failed: the valid custom consumer was not observed and the unsupported shape no longer failed closed. |
| 2 | Omit `IConsumerKindRegistration.Configure` during endpoint materialization | killed | The real primary message was skipped and the positive case timed out; the negative shape case remained green. |
| 3 | Change explicit-owner selection from `explicitOwners.Length == 1` to `== 0` | initially survived | The probe originally had no overlapping core registration. The positive test now registers `ProbeConsumer` through both core and the custom kind. Correct code gives the explicit kind ownership and delivers both messages; the repeated mutant fails at startup with the exact multiple-owner configuration error. |

Final bounded score: 3/3 killed after test hardening; one initial survivor, one closed test gap. The
final focused repetition passed 2/2 in 2.280 seconds. No production mutant remains in the working tree.
