# A+ remediation iteration 3 validation

Date: 2026-09-06

## Scope

This iteration closes the direct behavior gaps for the application-facing outgoing options and reliable-messaging operations:

- all 29 properties on `SendOptions`, `PublishOptions`, `ScheduleOptions`, and `RequestOptions` are exercised through public entry points;
- unsupported partition keys fail explicitly before partial context mutation;
- a configured request identifier owns response correlation as well as the outgoing envelope;
- reliable scheduling preserves supported application metadata through durable persistence and replay;
- reliable outbox and inbox reads validate at the facade boundary and preserve exact query and cancellation inputs; and
- both inbox/outbox reference partitions are exercised across their supported and invalid operations.

## Contract decisions

A caller-supplied partition key is a requested capability, not advisory metadata. A transport context that cannot represent it fails with `NotSupportedException`; it never reports success after silently discarding the value. Capability validation precedes all context mutation.

An explicit `RequestOptions.RequestId` is the identity of the complete request operation. The request handle, registered response handlers, outgoing request envelope, and returned response therefore share the same identifier.

Reliable scheduled messages use the same application metadata semantics as immediate sends. The durable record captures the metadata required for replay while preserving the existing scheduled token as the durable idempotency identity. Inbox quarantine pagination uses a single internal validation policy shared by the facade and providers without adding a public helper API.

## TDD and mutation evidence

Against the previous secured iteration, the initial tests proved four defects: an unsupported partition key was ignored, a custom request identifier could no longer match its response, invalid inbox queries crossed the operations boundary, and the reliable scheduler rejected the new options overload. Each path is green after correction.

Four one-cause mutation groups were then killed and restored:

1. Reintroducing silent partition-key discard caused the capability test to fail because no exception was raised.
2. Removing the request-identifier assignment from the common options pipe caused all three complete-options tests to report a null value.
3. Bypassing inbox-query validation at the operations facade changed provider-call evidence from one valid call to five calls.
4. Dropping scheduled metadata application before durable serialization caused the reliable scheduler test to observe the generated identifier instead of the requested message identifier.

The custom-request-identity red run independently timed out before the request handle was corrected, demonstrating that the test observes the actual response-correlation contract rather than only envelope assignment.

## Test quality

The new tests contain no fixed sleeps, wall-clock polling, skips, swallowed or broad catches, tautological assertions, or assertion-free bodies. Each options field has an independent assertion. Boundary doubles retain exact object and cancellation-token identity, while in-memory paths prove public send, publish, schedule, request, response, and consume-context behavior.

## Repository validation

| Gate | Result |
|---|---|
| `ViciOne.ServiceBus.Tests.Unit.slnx` Release build, warnings as errors | PASS — 0 warnings, 0 errors |
| Complete Unit/Architecture profile | PASS — 3,767 passed, 0 failed, 0 skipped across 21 assemblies |
| `ViciOne.ServiceBus.Engineering.slnx` Release build, warnings as errors | PASS — 0 warnings, 0 errors |
| Engineering whitespace verification | PASS |
| Engineering style verification at warning severity | PASS |

This is internal engineering evidence, not an independent external or Red Team acceptance.
