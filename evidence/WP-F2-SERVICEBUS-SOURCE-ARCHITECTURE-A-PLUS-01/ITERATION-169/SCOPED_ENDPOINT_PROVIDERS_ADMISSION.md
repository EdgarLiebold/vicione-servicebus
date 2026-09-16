# Iteration 169 — scoped endpoint providers admission

## Result

This packet personally reads both complete scoped endpoint-provider sources (76 lines) and the
complete new owning test file (143 lines). The review found the same public-boundary defect in the
publish and send variants: neither constructor rejected a missing provider or service scope, so an
invalid instance could escape construction and fail later, away from the configuration boundary.

Both public constructors now reject every missing dependency with its exact parameter name.
Endpoint resolution continues to forward the exact caller cancellation token, preserve the exact
service scope in the returned `ScopedSendEndpoint`, forward observers without substitution, and
retain the structured bus-route ownership diagnostic. Explicit guards keep every method at
cyclomatic complexity 1.

| Admission | Files / lines | Manifest SHA-256 | Chain SHA-256 |
| --- | ---: | --- | --- |
| Source | 2 / 76 | `b50fc34b89667f96f2de8f3d3e9a3ef3b039cb470ee743671f018ee848625242` | `6d9befbdece590f47130611c6f79fa4624600e51d857d69dcfb678b6fdfaeffb` |
| Tests | 1 / 143 | `eaf27fea31514b31ab4bcd2dce8e208953d050f56f23898be9be38f855d02608` | `d79685667bf9bbb75b33b5c05753a56345feec802229162ab6a9866b7ab61617` |

Manifest hashes cover the ordinally sorted `path<TAB>content-sha256` records with a terminal
newline. Chain hashes extend the Iteration 168 source/test chains with the corresponding manifest
hash. The manifests contain:

| Kind | File | Content SHA-256 |
| --- | --- | --- |
| Source | `ScopedPublishEndpointProvider.cs` | `0b635dfd8d510926c051136a69d21320b04ab74a728cffa37a40ad59a3fbb876` |
| Source | `ScopedSendEndpointProvider.cs` | `8920fd55e4a50ed258792e168d465da5585d8d58beded49baada793df06e0d42` |
| Test | `ScopedEndpointProviderTests.cs` | `f9adfd36bf28efbad19f30f2a6310eeb3bad02a6f5e00d3b18423b11fcac8aaa` |

Cumulative personal source admission is 300/4,116 current C# files.

## Proof

`Constructors_RejectEveryMissingDependency` proves all four required constructor parameters and
their exact names. `Providers_ForwardCancellationScopeObserversAndRouteOwnershipAsync` proves the
caller token on both asynchronous resolutions, the original transport endpoint, exact scope
identity in both wrappers, exact observer/handle forwarding, and the structured diagnostic for a
provider that does not expose bus-owned routes.

The two requirement variants are embedded in `CoreRequirements.json`, final SHA-256
`75ed212841fcd85745e919121b7b29374c09ff0fa1f0f072f97411cb07010079`.

Six successfully compiled single-cause mutants were killed and restored: remove each of the four
constructor guards individually, and replace either forwarded cancellation token with `default`.
Both product sources were restored to the reviewed final bytes before the final gates.

Final focused Cobertura is `/private/tmp/vicione-servicebus-iteration-169-final.cobertura.xml`,
SHA-256 `c88690c470d1e1e0d67805b4440099d11e39b8e7286667668472ce0e68c8ed4a`.
Both classes, including their asynchronous state machines, report 100% line and branch coverage.
Every reported method has complexity and CRAP 1. Unit sorted-display-name SHA-256 is
`c2e55ddf435a3d16c76fae4a70f4c82e053ae81d676071e93c98108153cac628`.

| Gate | Result |
| --- | --- |
| Focused provider owning tests | 2/2 passed |
| Full Core Release | 4,803/4,803 passed with suite parallelism disabled |
| Full EF unit | 248/248 passed |
| Strict Release product/unit/EF/local builds | 0 warnings, 0 errors |
| Product/test format | Exit 0 |
| Unit requirement projection | 1/1 passed |
| Local requirement projection | 1/1 passed |
| Mutation probes | 6/6 compiled mutants killed |

Core remains serialized because Iteration 155 demonstrated a pre-existing parallel-suite race.
Protected `review/**`, `TestResults/**` and `vicione-legacy/**` trees were not enumerated, read or
modified. Intended tag:
`servicebus-a-plus-iteration-169-scoped-endpoint-providers-admission-2026-09-16`.

Whole-fork personal reading, global API/naming/coverage and configured external-provider acceptance
remain open. Remote publication remains an independent delivery step and cannot pause or deactivate
the active goal.
