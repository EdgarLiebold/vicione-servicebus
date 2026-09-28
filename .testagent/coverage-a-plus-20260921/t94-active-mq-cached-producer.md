# T94 — ActiveMQ cached producer native contract

Exact test commit: `323dce211`.

## Product contracts checked

`NativeMessageFactory_PreservesEachShapeResultAndUsageOrderAsync` checks all
eight sync and eight async native factory shapes. Each forwards the exact
method and arguments after one usage signal, returns the native result, and
for async calls returns the identical pending native task until completion.

`NativeSend_PreservesEveryDestinationAndDeliverySettingsShapeAsync` checks
all four sync and four async send shapes, including destination and delivery
settings, usage order, task identity, and pending completion.

`NativeCloseAndDispose_ForwardExactlyOnceWithOnlyAsyncCloseReportingUsageAsync`
checks exact close and dispose calls, close task identity, and usage behavior.
`NativeProperties_ForwardGetterAndSetterWithoutReportingUsage` checks all
eight public property pairs in both directions with distinct values or
delegate identities and no usage event.

## Verification

- Focused class: 33/33 passed, no failures or skips.
- Complete ActiveMQ test project on exact commit `323dce211`: 266/266 passed,
  no failures or skips.
- A controlled async factory counterprobe dropped the text argument and failed
  the Text async row. A controlled property counterprobe forwarded
  `DeliveryDelay` to `TimeToLive` and failed the DeliveryDelay row. Both
  source changes were restored; the product file has no Git diff.
- Read-only Red Team found two P2 gaps in the initial test draft: completed
  factory tasks hid blocking or rewrapping, and the property pairs were
  untested. After correction, independent re-review: PASS with no remaining
  concrete P1/P2 finding.
- Four requirement variants are projected in `ActiveMqRequirements.json`.

## Measurement boundary

No product source changed. T85 remains the latest complete 33-profile
Line/Branch/CRAP checkpoint. Global A+ remains open; complete measurement
follows the agreed larger packet cadence.
