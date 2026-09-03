# V5 contract identity, endpoint QoS, and consumer concurrency validation

Date: 2026-09-03

## Bound inputs and scope

- Package: V5 integration 1/4.
- Architecture assignment: `PO-2026-09-03-SERVICEBUS-REVIEW-INTEGRATION-12` at local architecture
  commit `7eeffe9a`.
- Product baseline: `603b8771bf2bc70c6bb2e369c4747dcf98809742`, tree
  `10c9fe910a3061cf323daff80bf8c23f089edc07`.
- Protected review aggregate recorded for the immutable handoff:
  `371bf21331f0fc3316be271bce04ab37b3c54c50e13f443789d94c1f6eca1f18`.
- V5 cumulative patch SHA-256:
  `88f2c61a3b2fc470f525ce68a3972463e731bea418cfa9de0d58f4e745a1a344`.
- V5 bundle SHA-256:
  `af76f8f4266efc7aa6d2bb34d29b30c0b73bb04dee7cb8237e4ab42717e1fdf5`.
- Frozen V5 head: `d4465f57f4753985f75684a8c690e878a931fd7f`.
- Semantic donor commits: `367cf81f2ae22f5e223a41b8e2ec7d9bf2a244e9`,
  `0a0c4a06234a4ed35ccd6e802397be1f43e55066`, and
  `6dcb588e6b7bd4232ab72cf7b559cbd90453ab8c`, reconciled through hardening commit
  `6ab00c32039f88efc4d29c3c0d41d7a733ab8366`.
- `review/**` remained read-only, unmodified, untracked, and unstaged throughout.

The donor was treated as semantic evidence. Public contracts were adapted to the current native namespace,
configuration, dependency-injection, probe, test, and exception conventions instead of copying its isolated
regression project.

## Stable contract identity

`MessageContractIdentity` is a bounded persisted/wire identity made only from a stable name and positive
major version. It rejects whitespace, separators outside the documented grammar, names longer than 256
characters, version zero, and noncanonical textual versions such as leading-zero forms. It never stores an
assembly-qualified name and parsing fails without returning a partial identity.

`MessageContractAttribute` is direct and noninherited. `MessageContractCatalogBuilder` accepts explicit or
attribute declarations, rejects value types and open generics, treats only an exact type/identity repeat as
idempotent, and fails both type and identity collisions. `Build` creates immutable bidirectional frozen
maps; unknown and null lookups fail closed. The V5.1 application-composition convenience API remains in its
own later hardening package; this package establishes and proves the canonical catalog owner it will expose.

## Endpoint QoS ownership

`EndpointTransportQos` separates transport prefetch and endpoint-wide delivery concurrency from consumer
execution policy. Empty declarations are ignored; specified values are positive. Endpoint-owned discovery
paths must converge exactly. A retained consumer-definition-originated transport declaration is accepted
only for a genuinely dedicated endpoint, as required by the frozen V5 contract; adding a second distinct
consumer fails eagerly and deterministically before endpoint configuration executes.

The current dependency-injection path validates every combined endpoint declaration. It distinguishes
per-consumer endpoint settings from one shared endpoint-definition instance, so built-in Quartz and
SignalR endpoint owners are not misclassified merely because several consumers reference them. A consumer's legacy
`ConcurrentMessageLimit` now maps to the first-class consumer pipeline policy and no longer changes endpoint
prefetch or transport concurrency. Explicit endpoint-owned QoS still reaches the transport. The provider-
specific Azure Service Bus probe test was corrected from the obsolete leakage expectation and passes 25/25.

## Consumer execution ownership

`ConsumerConcurrencyPolicy` has one hard-bounded owner for parallel, serial, and fixed-partitioned modes;
the inclusive maximum is 1,024. Exact duplicate consumer-wide policies are idempotent and conflicting or
layered policies fail at configuration. A typed partition selector is registered once for its message type.

The runtime hook is installed in the actual consume pipeline, once per consumer registration. Serial and
bounded parallel modes share one consumer-wide gate across message types. Partitioned mode maps the typed
message to a fixed partition and excludes equal keys while allowing different partitions to progress.
Admission preserves waiter cancellation, pipeline failures release the slot, minimum hash codes are safe,
and disposal closes new admission while accepted active and waiting work drains. FIFO is deliberately not
claimed.

The probe graph exposes exactly one `consumerConcurrency` filter per configured consumer path, including
the mode and limit. The legacy non-management `UseConcurrentMessageLimit<TConsumer>` extension maps into
this same owner, so it cannot silently layer a second semaphore.

## Native execution evidence

All test applications use xUnit 4 on Microsoft.Testing.Platform v2. Because the restricted runner denies
the named-pipe server used by `dotnet test`, the already-built Release applications were invoked directly
and serially with exact nonzero minimum counts, a strict zero-test policy, and a 15-minute timeout.

| Scope | Result |
|---|---:|
| Abstractions | 320/320 passed, 0 skipped |
| Core | 1,635/1,635 passed, 0 skipped |
| Architecture | 167/167 passed, 0 skipped |
| Other 20 Unit executables | 1,223/1,223 passed, 0 skipped |
| Complete Unit/Architecture aggregate | 3,345/3,345 passed, 0 skipped |
| Shipping Release build | 0 warnings, 0 errors |
| Engineering Release build after final test correction | 0 warnings, 0 errors |

The first complete matrix correctly exposed one Azure Service Bus test that still expected consumer
concurrency to leak into endpoint transport QoS. The other 3,344 cases passed. After changing only that
obsolete expectation, its final Release owner passed 25/25 and the full aggregate is 3,345/3,345. The final
Azure CTRF report SHA-256 is
`de7e91b800bead4f22826dd3340f65093096dd4bbc3ac72fee64c475a8797ce6`; the final Quartz report SHA-256 is
`b4c0f3fb5b96629e930ca216cad90f68e1c479f9c3c81a98a48ddaf312b67c8e`, and the final Core report SHA-256 is
`5572dce479cbeaa1a2f1bacfce655dbcf4db325c10607920197021f2fb2ed385`.

No external provider is required to prove these provider-neutral contracts. Every provider Unit owner ran
without skips, the Azure Service Bus transport probe proves the provider projection, and all provider local-
integration projects compiled in the Engineering solution. Earlier real-provider V4 evidence is unchanged
and is not relabeled as new execution.

## Mutation and hygiene gates

Sixteen independent one-cause production mutations compiled, reached the intended native owner, and were
killed. Every target was restored before final positive execution. Full detail is in
`MUTATION_VALIDATION.md`.

- scoped `dotnet format --verify-no-changes` passes for all 40 changed/new C# files;
- all requirements JSON files parse and project in the complete native run;
- the protected manifest verifies every listed review artifact, and both V5 input hashes match;
- no protected review file is staged or modified;
- no non-build empty directory or immediately repeated `src` directory component remains;
- `git diff --check` and the final staged-index check pass;
- analyzer-active Shipping and Engineering Release builds are clean.

V5 package 1/4 is locally validation-complete. Durable sender admission/completion, payload and diagnostic
controls, analyzers/observability/final V5 reconciliation, V5.1 hardening, and the separate API review remain
active goal phases. No remote publication is authorized or implied.
