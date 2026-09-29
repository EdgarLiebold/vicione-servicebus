# T119 — Saga request timeout admission

## Product defects and correction

`RequestActivityImpl.SendRequestAsync` formerly sent a positive-timeout
request and stored its request ID before discovering that the context lacked
a scheduler. It also performed date arithmetic after send, so an invalid
configured timeout could leave the same partial state. The shared activity
now checks scheduler availability and configured deadline representability
before request-ID generation or transport dispatch. The normal deadline still
starts when send completes. If the clock advances into the terminal range
during send, the accepted request is scheduled for the final representable
UTC instant. The scheduler remains responsible for accepting that date.

## Behavior evidence

| Requirement | Exact test and oracle |
| --- | --- |
| Missing scheduler across four request forms | `Request_MissingTimeoutSchedulerRejectsBeforeDispatchAsync` runs normal/faulted × typed/untyped; it requires the scheduler diagnosis, original request ID, no ID generation, endpoint, send or continuation. All four cases failed against the original product. |
| Valid scheduler and post-send timeout | `Request_WithTimeoutSchedulerSendsAndSchedulesMatchingExpiryAsync` blocks send, advances a fixed context clock by 90 seconds, then requires a single send, ID persistence, matching scheduled request/correlation/message/token, the exact post-send timestamp and deadline, and continuation order. It failed against an early-deadline correction. |
| Invalid configured date range | `Request_UnrepresentableTimeoutRejectsBeforeDispatchAsync` uses `TimeSpan.MaxValue` with a valid scheduler. It requires the date-range configuration diagnosis, no transport/scheduler effects and an unchanged earlier request ID. The diagnosis test failed before the correction. |
| Terminal clock advance during accepted send | `Request_ClockAdvancesToDateLimitDuringSendStillSchedulesAsync` begins two seconds before `DateTimeOffset.MaxValue`, blocks send, advances one second, and requires the final UTC due date, same request ID, scheduling and continuation. It failed on the second post-send overflow and caught a local-offset conversion of `DateTime.MaxValue`. |

## Verification and limits

- The full Core project passed 7,132/7,132 on the final source/test bytes, zero failures or skips; `dotnet test --project tests/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj --no-restore -v quiet`.
- `git diff --check` and requirement JSON parsing passed. Independent read-only Red Team review returned PASS after its scheduler-diagnosis, fixed-clock, date-overflow and deadline-origin counterexamples were closed.
- Microsoft `code-testing-agent` Research → Plan → Implement, `test-gap-analysis`, `assertion-quality` and `run-tests` were applied. The tests assert externally visible dispatch, saga ownership, schedule payload and time rather than coverage alone.
- The positive-timeout flow still cannot guarantee atomic transport send plus scheduler acceptance if an external scheduler fails after the transport accepts the request. This existing distributed-operation limit is separate from the deterministic configuration failures corrected here.
- T114 is the latest complete 33-profile CRAP measurement. The user accepts its Line and Branch rates as A+; a fresh product-wide CRAP checkpoint on these bytes is still pending.
