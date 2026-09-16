# Iteration 168 — scoped consume endpoint providers admission

## Result

This packet personally reads both complete scoped consume endpoint-provider sources (91 lines) and
the complete new owning test file (156 lines). The review found two public-boundary defects shared
by the send and publish variants: neither constructor rejected missing required dependencies, and
both asynchronous endpoint resolutions discarded the caller's cancellation token by passing
`default` to the underlying provider.

Both public constructors now reject every missing dependency with the exact parameter name. Both
resolution paths forward the caller token, retain the exact ambient consume context, preserve the
exact service scope in the outer `ScopedSendEndpoint`, retain the consume-aware inner endpoint,
forward observers, and preserve the existing bus-route ownership diagnostic. Explicit guards keep
every method at cyclomatic complexity 1.

| Admission | Files / lines | Manifest SHA-256 | Chain SHA-256 |
| --- | ---: | --- | --- |
| Source | 2 / 91 | `b92c19eb57d3fdacc4baaa38d2200c5396afe4805ab686a74865b2c9fd02c881` | `1dec0bc66130eaab4cff86a8af5c8745426066e7ceb40b79af0fb7c7f31f5200` |
| Tests | 1 / 156 | `c410f5ea04c1ba5c41a30d5a5e8b3fec19807d3d45b7cd80c165d660c835dc94` | `c236ae2a4c50bd48720f3e631b5815f4b87749d710bd484efd80e5c98bc74269` |

Manifest hashes cover the ordinally sorted `path<TAB>content-sha256` records with a terminal
newline. Chain hashes extend the Iteration 167 source/test chains with the corresponding manifest
hash. The manifests contain:

| Kind | File | Content SHA-256 |
| --- | --- | --- |
| Source | `ScopedConsumePublishEndpointProvider.cs` | `8f6a6b6394123ea035fe6c98caebf88383125877799a0c69e7b32f3046a5eb7d` |
| Source | `ScopedConsumeSendEndpointProvider.cs` | `e7842042d63a5079977cc3b5e4016473547d73a3a9f51c3bbb0a4bafcf640a7f` |
| Test | `ScopedConsumeEndpointProviderTests.cs` | `add659aee2ba4185850195839814701d167019dee1837dc7444ca5b70b957b8f` |

Cumulative personal source admission is 298/4,116 current C# files.

## Proof

`Constructors_RejectEveryMissingDependency` proves all six required constructor parameters and
their exact names. `Providers_ForwardCancellationScopeObserversAndRouteOwnershipAsync` proves the
caller token on both asynchronous resolutions, the exact two-layer consume/scope wrapper chain,
the original transport endpoint, ambient context and service-provider identities, exact observer
forwarding, and the structured diagnostic for a provider that does not expose bus-owned routes.

The two requirement variants are embedded in `CoreRequirements.json`, final SHA-256
`496c591a3cbe7d99c371370f7b68b5db7bfb0904a6c00f2f44edf5d5a226bcd0`.

Six successfully compiled single-cause mutants were killed and restored: remove the publish
provider guard, remove the send scope guard, replace either forwarded cancellation token with
`default`, and omit either outer `ScopedSendEndpoint` wrapper. Both product sources were restored
to the reviewed final bytes before the final gates.

Final focused Cobertura is `/private/tmp/vicione-servicebus-iteration-168-final.cobertura.xml`,
SHA-256 `4c7ec797075777fbe7ee9b78dff454f258071a1e14fc5f7e5bce4cd565ad8382`.
Both classes report 100% line and branch coverage. Every reported method has complexity and CRAP 1;
the pre-simplification constructor form had complexity and CRAP 6 at the same coverage. Unit
sorted-display-name SHA-256 is
`976748a9817a3823db0b24589fced37d72048a4be95486a33b75e24055c80eab`.

| Gate | Result |
| --- | --- |
| Focused provider owning tests | 2/2 passed |
| Full Core Release | 4,801/4,801 passed with suite parallelism disabled |
| Full EF unit | 248/248 passed |
| Strict Release product/unit/EF/local builds | 0 warnings, 0 errors |
| Product/test format | Exit 0 |
| Unit requirement projection | 1/1 passed |
| Local requirement projection | 1/1 passed |
| Mutation probes | 6/6 compiled mutants killed |

Core remains serialized because Iteration 155 demonstrated a pre-existing parallel-suite race.
Protected `review/**`, `TestResults/**` and `vicione-legacy/**` trees were not enumerated, read or
modified. Intended tag:
`servicebus-a-plus-iteration-168-scoped-consume-endpoint-providers-admission-2026-09-16`.

Whole-fork personal reading, global API/naming/coverage and configured external-provider acceptance
remain open. Remote publication remains an independent delivery step and cannot pause or deactivate
the active goal.
