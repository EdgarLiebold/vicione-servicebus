# Azure Service Bus receiver error classification

The complete product profile at `151bf0dc1` measured the receiver's
`ExceptionHandlerAsync` at CRAP 57.95 (complexity 50, 29/34 lines covered).
The receiver now separates recycle classification, log selection, quiet
conditions, and notification. The original reason precedence, log templates,
structured values, and `NotifyFaultedAsync` before `TrySetConsumeException`
order remain unchanged. `SessionReceiver` still inherits the handler.

`QueueProcessorErrorCallback_LogsTheClassifiedFailureWithExactStructuredIdentityAsync`
checks transient and nontransient communication errors, WebSocket failures
with an inner timeout, a transient generic warning, an unknown error, all
four expected quiet conditions, cancellation, and an inner timeout. It checks
the recycle notification, log level, exact exception, template, input address,
SDK action or socket code, dispatch count, and recycle flag. The existing
`QueueProcessorErrorCallback_WaitsForFaultNotificationBeforeCompletingAsync`
now proves that the receiver remains active while fault notification is
pending and completes only after notification is released.

The focused Microsoft CodeCoverage report is
`artifacts/coverage-asb-receiver-classification-20260924/raw/final5/coverage.cobertura.xml`,
SHA-256 `28a2bcdc933b0fd10fd8fc33c4586b9568a8dcbe130825b855626da05336dcf0`.
The generated async handler is 6/6 lines, CRAP 2. The extracted methods
`RequiresRecycle`, `LogProcessorError`, `ShouldSuppressProcessorLog`, and
`IsQuietReceiverError` are respectively 16/16, 18/18, 3/3, and 4/4 lines,
with CRAP scores 18, 18, 4, and 12. These are focused Azure unit results,
not a new product-wide profile.

The final Azure Service Bus suite passed 239/239. The Release Unit/Architecture
gate passed 10,258/10,258 with no failures or skips. The final Release
solution build had zero warnings and errors. A read-only adversarial
review first identified missing logging and notification-order oracles, then
specific qualifier mutants; after the assertions were strengthened, it
returned PASS without a remaining concrete product regression.

The last complete product-wide profile remains `151bf0dc1`: 89.6626% line,
81.8534% conservative branch observation, and 52 methods with CRAP > 30.
Global A+ remains open until a fresh all-assembly profile of current bytes
is collected and the remaining gaps are resolved.
