# Timeout and cancellation — product-path analysis

## Scope

This cohort replaces the four inherited cases in `Timeout_Specs.cs` and
`Cancellation_Specs.cs`. The complete timeout filter, all timeout pipe specifications,
configuration observers, consume and Courier context proxies, consumer fault path, transport-stop
path and public configuration extensions were read before replacement. The upstream history shows
the inherited implementation was the unchanged MassTransit v8.5.10 shape; it provided no separate
ViciOne design rationale.

## Cancellation ownership

The input context owns caller and transport cancellation. The timeout filter owns one deadline and
one linked execution token. Classification is deterministic: caller cancellation has precedence and
is normalized back to the exact caller token even when downstream code creates another linked token;
an expired owned deadline becomes `ConsumerCanceledException` with the originating cancellation as
its inner cause; independent cancellation is propagated unchanged. A transport stop never publishes
an application fault, including when timeout middleware is present.

The filter covers both downstream execution and `ConsumeCompleted`. Its deadline timer and linked
source are disposed on every terminal path. The context-scoped `TimeProvider` is the default time
owner. An explicit configured provider is supported for deterministic configuration and testing and
is captured when the pipe is built.

## Fault and configuration boundary

Fault publication occurs inside the timeout consume context before the outer filter can classify the
exception. The wrapper therefore reports the same timeout classification and inner cause in
`Fault<T>` while preserving ordinary independent cancellation and suppressing faults caused by
transport shutdown.

All five public configuration scopes remain: message pipe, complete consume pipe, consumer, saga
and handler. The complete consume observer additionally projects the feature into ordinary message,
batch, execute-activity and compensate-activity pipelines. These internal specification, observer
and context-proxy types are hidden; the public API consists of `UseTimeout`,
`ITimeoutConfigurator` and the reusable `TimeoutFilter`. A configuration delegate is mandatory,
nonpositive durations fail validation in every projected scope, and an applied specification is an
immutable snapshot even if a caller retains and later mutates its configurator reference.

## A+ disposition

- Preserve the timeout capability and every supported configuration scope.
- Use `TimeProvider` rather than wall-clock timers for deterministic, platform-standard time.
- Preserve exact cancellation ownership and causal exception chains.
- Emit a timeout fault for an actual deadline, but never for transport shutdown.
- Await the complete consume lifecycle and release every timer deterministically.
- Hide implementation-only observers, specifications and context wrappers.
- Reject incomplete configuration rather than retaining an optional callback that always produced
  an invalid zero timeout.
