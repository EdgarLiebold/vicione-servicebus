# T90 — message scope handler lifetime

Exact test commit: `2e153870e`.

## Contract checked

`MessageScope_RawHandlerOwnsAndReleasesSeparateScopesAfterSuccessAndFailureAsync`
configures `UseMessageScope` on a real in-memory receive endpoint with a raw
handler. It sends success, delayed failure, and success in sequence. The
handler's scoped service, scope payload and provider have matching ownership;
each message has a distinct scope and marker. An asynchronous gate proves no
scope is disposed while either successful or failing handler work remains
active. Each scope is then disposed exactly once. The one published fault is
bound to the failing message ID and the exact failure type. The final success
proves the failed message did not poison the next scope.

## Verification and adversarial checks

- Focused class: 1/1 passed, no failures or skips.
- Complete Core project on exact commit `2e153870e`: 6,957/6,957 passed,
  no failures or skips.
- Isolated counterprobe removed only `endpoint.UseMessageScope(context)`.
  The test failed and transport logs reported `PayloadNotFoundException` for
  `IServiceScope`. The line was restored and the focused class passed again.
- Read-only Red Team first found the need for an asynchronous lifetime gate,
  then identified a gate-cleanup gap and a fault-identity gap. All three were
  corrected. Final re-review: PASS, no P1/P2.
- Requirement projection: `REQ-VSB-CONTAINER-SCOPE` variant
  `message-scope-handler-isolated-and-released-after-success-and-failure`.

## Measurement boundary

No product source changed. T85 remains the latest complete 33-profile
Line/Branch/CRAP checkpoint. Global A+ is still open, and the next complete
profile follows the agreed larger packet cadence.
