# Iteration 12 Validation

## Scope

Iteration 12 makes asynchronous intent and cancellation shape executable repository contracts.
The bidirectional Roslyn gate examines every evaluated product and native-test method, including
local functions. Task-based operations require the `Async` suffix, while synchronous methods may
not use that suffix or an `AsyncCore` marker. Asynchronous delegate-configuration overloads remain
explicit because their name describes the delegate contract and prevents accidental `async void`
overload selection. Explicit interface implementations and test entry-point names retain names
owned by their external framework contracts.

All request-handle factory APIs now place `RequestTimeout` before the final `CancellationToken`.
Interfaces, implementations, dependency-injection adapters, mediator adapters, positional call
sites, XML parameter order, package consumers, and the versioned packed API contract were updated
as one atomic API change. Extension methods whose receiver is itself a token and APIs that
intentionally distinguish two cancellation roles remain structurally valid exceptions.

Four synchronous test helpers were converted to synchronous contracts instead of manufacturing
completed tasks. Two formerly synchronous SignalR methods no longer carry a misleading
`AsyncCore` marker.

## Red/green evidence

The initial cancellation-shape scan reported fifty-two candidates. Forty-eight were genuine
request-contract violations; four were intrinsic extension-receiver or dual-token shapes. The
refined rule rejected all forty-eight genuine declarations and passed only after the complete
contract family and its callers were normalized.

The asynchronous scan exposed the two SignalR `AsyncCore` violations. The accompanying adversarial
inventory also identified four synchronous test helpers that returned already-completed tasks;
their signatures and callers were converted to synchronous code. The final bidirectional scan is
green across the complete evaluated product and native-test source graph.

Two argument-recording behavior tests cover the dependency-injection request wrapper for both a
typed request and initializer values. They assert the same input object, exact timeout, exact
cancellation token, and identical returned request handle.

## Mutation evidence

Three isolated regressions were introduced and removed:

1. Restoring `AddGroupAsyncCore` on the synchronous SignalR method was killed by the bidirectional
   naming gate with the exact file, line, and member.
2. Moving one public cancellation token before its timeout was killed by the cancellation-shape
   gate with the exact member and following parameter.
3. Dropping the caller's timeout at the generic dependency-injection wrapper compiled successfully
   but was killed by the typed forwarding test's exact timeout assertion.

Every mutation was restored before final validation.

## Test-quality review

The architecture checks use evaluated MSBuild compile items rather than filesystem guesses, cover
product and native-test local functions, and report every violation deterministically. The
forwarding tests use an invocation-recording interface proxy and make no timing assumptions. Their
assertions distinguish the two overloads and every reordered argument rather than checking only
that a call occurred. Each new test has an exact requirement projection entry.

## Repository validation

| Gate | Result |
|---|---|
| Unit/Architecture solution Release build, warnings as errors | PASS — 0 warnings, 0 errors |
| Complete Unit/Architecture profile | PASS — 3,830 passed, 0 failed, 0 skipped |
| Engineering solution Release build, warnings as errors | PASS — 0 warnings, 0 errors |
| Fresh-package gate, normal comparison mode | PASS — 18 journeys, 30 packages, 3 executable isolated consumers, 29 runtime package APIs |
| Packed public API contract | PASS — 24,000 lines, SHA-256 `36a9b02c2417bfe12abf7be4858236cc23604afffa0fadb7fe38972217f510ec` |
| Requirement projections and architecture manifests | PASS — included in the complete profile |
| Warning-level repository format verification | PASS — no reported formatting violations |
| Git whitespace validation | PASS |

This is internal engineering and adversarial-review evidence, not independent external acceptance.
