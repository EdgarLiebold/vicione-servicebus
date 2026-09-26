# T30: cancellation follow-up — complete measurement recorded

Base HEAD: `59925b8ab86a85c6fcce002062bc97b86b8f6e99`.
The tests were committed and measured at `3264ed26c3914a44fff2ef3291750b766efda870`.
The complete 33-profile measurement supersedes the initial working-tree runs;
see [measurement and remaining risks](product-wide-profile-3264ed26c.md).

## Event Hubs confirmation contracts

Test file: `tests/Transports/ViciOne.ServiceBus.EventHubs.LocalIntegration.Tests/EventHubIntegration/PendingConfirmationCancellationTests.cs`.

| Contract | Test |
| --- | --- |
| Individual cancellation retains its token, preserves neighboring partitions/offsets, permits a replacement, and resists late completion/failure notifications | `IndividualCancellation_PreservesOtherPartitionsOffsetsAndReplacementAsync` |
| Processor cancellation cancels all open confirmations, preserves prior success/failure, and rejects subsequent admission with the lifetime token | `ProcessorCancellation_CancelsEveryOpenConfirmationAndPreservesTerminalResultsAsync` |

Read-only review found two unbounded waits on the faulted confirmation; both now use bounded `WaitAsync`. These tests exercise the real confirmation collection and SDK event models. They do not establish broker, checkpoint, or concurrent Add-versus-Cancel behavior.

Validation completed:

- MAIN reviewed tests: 2 passed, 0 failed, 0 skipped; exit 0.
- Verify-only whitespace formatting: exit 0, no changes.
- Isolated deliberate mutation: `PendingConfirmationCollection.Canceled` forwarded to `Complete()` instead of `Canceled(token)`.
- Both tests failed at their cancellation exception assertion; exit 2. Neither timed out or failed to build.
- Mutation manually restored; isolated control: 2 passed, 0 failed, 0 skipped; exit 0.
- Isolated `git diff --exit-code -- src`: exit 0 after restoration.

## Local evidence hashes (SHA-256)

| File | SHA-256 |
| --- | --- |
| MAIN `artifacts/t30-confirmation-reviewed.log` | `f158d477775ee7d3d9634dd6b86ef4550923581aabd167af2cbdcec37bed4f00` |
| MAIN `artifacts/t30-format.log` | `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` |
| Test source | `5dc028d22fa36cae6b5ef462a13ca7b4312d25d1c38f06cdc9b2d3021a3288b2` |
| Isolated `artifacts/t30-confirmation-mutant.log` | `88819a2ae42bc7c1eedee53c10f09dbe7146ab50365031aec503476976f43fa3` |
| Isolated `artifacts/t30-confirmation-restored.log` | `a5f4ecfaab6bd0aac8af426dce8b9dae375c2abc725e7b5ea0f240956ecca991` |

The isolated directory is `/private/tmp/servicebus-reply-investigation`. Raw logs remain local; this report does not publish them.

## Request failure versus repeated cancellation

`RequestClientLifecycleTests.RepeatedCancel_DuringFailureCleanupPreservesTheOriginalSendFailureAsync`
holds real failure cleanup at timer disposal, after publication of the response
failure but before completion of `Message`. Two explicit cancellations must
leave `Message` pending. After cleanup is released, `Message` must throw the
exact transport exception and both original and subsequently requested response
tasks must retain the exact same request exception. All task waits are bounded;
the gate is released in `finally`.

Initial class run: 20 passed, 0 failed, 0 skipped, exit 0. Verify-only whitespace
formatting exited 0. The read-only adversarial reviewer found no blocker and
confirmed that removing only the terminal-state guard in `Cancel()` should
make the second pending-message assertion fail. The empirical mutation did
exactly that: the new test failed at line 663, while 19 controls passed (exit 2).
The guard is manually restored; the restored class run passes 20/20 with no
failures or skips (exit 0). `git diff --exit-code -- src` is clean. This test
does not separately prove handler disconnection or
CTS disposal, which have dedicated existing tests.

| Request evidence | SHA-256 |
| --- | --- |
| MAIN `artifacts/t30-request-lifecycle.log` | `11e333d489c12cf3ef42366f139531fb8669f91ce025348444047008b7807cfe` |
| MAIN `artifacts/t30-request-format.log` | `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` |
| Isolated `artifacts/t30-request-mutant.log` | `30e96cdb8a59b6e621ffcf5d88ea0d740d17b19b0df7f2d3827baadbee0b6c83` |
| Isolated `artifacts/t30-request-restored.log` | `5326772295a82520c23cfcc771fb9dc09f3b3d5b54bb75c80b283e222de52510` |

## Completion and remaining program work

- Complete project runs, including requirement projections, passed in all 33 canonical profiles at the exact commit.
- Adversarial reviews, deliberate mutations and restored controls are recorded; all four targeted methods have full line and observed branch coverage.
- Independent accounting review verified 487 evidence hashes and the five-closed/six-new delta. The source tree is unchanged.
- The documentation successor records the current gaps and is prepared for authorized push with the test commit.
- The six newly observed gaps and the broader A+ program remain open; focused successes do not establish A+.
