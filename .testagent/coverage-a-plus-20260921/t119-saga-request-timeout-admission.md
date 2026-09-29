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

- Exact source/test commit: `40082466516f51113405d4b989a8d935b6822c09`;
  source tree `3c75afd38f51` and test tree `a1076869d9f7`. Two fresh
  hash-bound Release receipts at `artifacts/t119-profile-00/receipt.json`
  and `artifacts/t119-profile-13/receipt.json` passed 7,132 Core and 400 EF
  tests on those same trees. The Core receipt observed 1,923 tracked product
  sources; the EF receipt observed 1,953. The changed Saga request async
  method has 26/26 measured lines, complexity 14 and CRAP 14.
- Across the two receipts, the four T115 affected sources have no method
  above CRAP 30. Their maxima are 28.000 for classic EF scoped outbox,
  27.981 for reliable EF scoped outbox, 18.015 for InMemory reliable inbox
  factory and 22.003 for EF reliable inbox factory. This closes the five
  specific T114 hotspots on exact current bytes. The strict partial aggregate
  is `artifacts/t119-core-ef-partial.json`; its 75 other apparent hotspots
  cannot be interpreted as product-wide because 31 receipts are absent.
- The full Core project passed 7,132/7,132 on the final source/test bytes, zero failures or skips; `dotnet test --project tests/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj --no-restore -v quiet`.
- `git diff --check` and requirement JSON parsing passed. Independent read-only Red Team review returned PASS after its scheduler-diagnosis, fixed-clock, date-overflow and deadline-origin counterexamples were closed.
- Microsoft `code-testing-agent` Research → Plan → Implement, `test-gap-analysis`, `assertion-quality` and `run-tests` were applied. The tests assert externally visible dispatch, saga ownership, schedule payload and time rather than coverage alone.
- The positive-timeout flow still cannot guarantee atomic transport send plus scheduler acceptance if an external scheduler fails after the transport accepts the request. This existing distributed-operation limit is separate from the deterministic configuration failures corrected here.
- T114 is the latest complete 33-profile CRAP measurement. The user accepts its Line and Branch rates as A+; a fresh product-wide CRAP checkpoint on these bytes is still pending. The two-receipt partial aggregate is not comparable to the complete profile.
