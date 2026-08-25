# C31 product-path analysis

## Owner

`src/ViciOne.ServiceBus.Abstractions/BusControlHealthExtensions.cs` owns the public wait operation.
`IBusControl.CheckHealth()` is the observation boundary; endpoint aggregation and kill-switch state
transitions are downstream producers and remain outside this cohort.

## Complete call-path findings

- The timeout overload uses `DateTime.UtcNow` and `Task.Delay(100)` and therefore cannot be tested
  under logical time.
- Timeout returns the last unexpected enum as if the wait had succeeded. Callers must remember to
  add a second assertion and otherwise receive a false success.
- Returning only `BusHealthStatus` discards the endpoint data, description and causal exception that
  explain the failure.
- Cancellation is checked only after an initial health read; an already-canceled operation can
  therefore observe mutable product state or even report success.
- Collection overloads enumerate lazily inside LINQ and provide no explicit null-element boundary.
- The EF Core outbox delivery service is the only product caller. Remaining callers are inherited
  migration tests and are updated mechanically so the old suite continues to compile until their
  own cohorts replace it.

## A+ decision

Use one implementation driven by the standard .NET `TimeProvider`; production convenience overloads
use `TimeProvider.System`. Follow .NET asynchronous naming, return the complete health result, use a
typed timeout exception and preserve exact cancellation. Observe health once more at the exact
deadline before declaring timeout. Materialize and validate collections once, run waits concurrently
and preserve input order.
