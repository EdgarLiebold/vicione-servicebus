# Testing Harnesses — Test Quality Review

Date: 2026-08-24

Scope: the 105 ordinary xUnit test methods under `tests2/ViciOne.ServiceBus.Tests/Testing`, their
test-owned support types, the four `SystemTextJsonDateOnlyTimeOnlyTests`, and the complete product
paths changed by this cohort.

The inherited tests were treated only as a semantic lower bound. The replacement scenarios add
negative and failure behavior, exact exception identity, message payload and metadata, correlation,
conversation and initiator causality, lifecycle ordering and idempotence, deterministic time,
missing-instance behavior, real in-memory dispatch, and repository observation boundaries.

Every reviewed test has a behavior-specific assertion. The scope contains no skip, assertion-free
test, tautological assertion, swallowed test exception, unawaited assertion, `Thread.Sleep`, real
`Task.Delay`, wall-clock threshold, random input, external file or network dependency, or mock of
the product behavior being proved. Time-sensitive cases use a test-owned `TimeProvider`; integration
scenarios use the real in-memory transport, dependency-injection container, mediator, saga
repository, serializer, or request client as appropriate.

The `SendQuery` hardening test is intentionally not a duplicate of the inherited suite. It proves
both sides of a property-correlated saga query: the existing instance is updated and recorded once,
while a missing key executes its missing-instance policy without creating or falsely recording a
saga. A direct repository bypass makes only this test fail.

The consume-observer replacement does not preserve the inherited shared fixture. Each scenario owns
its bus or mediator, observers and message. It checks both typed and untyped observer paths, exact
request/response identity, the fault path and the distinction between a live receive-context timer
and the immutable duration captured by a recorded message. The three recorded-message wrappers use
their injected clock and expose stable payload, context, type, identifier and failure metadata.
Filter configuration is get-only; no assignment can be silently discarded.

The observation-list subcohort covers every public query shape of sent, published and received
lists using exact real records. The typed received facade is exercised independently. Shared-list
hardening proves missing-ID rejection, first-wins duplicate handling, visible filter failure and
subsequent reuse; observer tests prove exact success/fault records and configured time. Empty
sequence helpers and both sent-message deconstruction forms have literal external oracles. The only
reflection assertion protects a public get-only API boundary and is paired with behavioral filter
tests; it is not an implementation-state shortcut.

The receive-endpoint observer is not accepted merely because it existed upstream. A real dynamic
endpoint publishes from its consume context into an observer connected only through the endpoint
lifecycle. Completion is synchronized causally and the observer is inspected as an immediate
snapshot, so removing the `Ready` connection fails exactly this case without wall-clock waiting.

The DI-utility additions exercise the real container and in-memory bus for filter selection, task
registration and dynamic endpoint behavior. The one readiness test uses a narrow `DispatchProxy`
only for the two collaborator interfaces needed to keep an endpoint pending; it neither simulates
message delivery nor reconstructs product behavior. Advancing the official `FakeTimeProvider`
proves that the product's own harness budget completes the operation. The dynamic connector test
uses real endpoints and validates both callback overloads exactly once, registration-context DI,
separate delivery and correlation. Its class and file are named for that public product owner rather
than the internal bus-instance implementation.

The retry test no longer assumes ordering between two independent observers. It establishes both
subscriptions before publication and waits for terminal fault publication and terminal consume-
fault recording before taking an immediate list snapshot. Two complete parallel Testing-namespace
runs passed 105/105 after the correction.

This review does not claim complete coverage of every remaining source file in the product. The
active test-reconstruction plan continues source-owner by source-owner, and the root `TODO.md`
retains product normalizations and external-resource work that require later dedicated slices.

Verdict: **PASS**.
