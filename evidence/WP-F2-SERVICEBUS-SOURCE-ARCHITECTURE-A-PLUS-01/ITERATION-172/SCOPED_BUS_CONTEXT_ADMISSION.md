# Iteration 172 — scoped bus context admission

## Result

This packet personally reads the complete root and consume scoped-bus context sources plus their
public contract (196 lines) and the complete new owning test file (151 lines). All four public
context constructors accepted missing required dependencies. Three root-context facades and two
consume-context facades used unsynchronized `??=` caches, so concurrent callers could receive
different supposedly scope-owned objects and construct discarded client-factory wrappers.

Every required constructor dependency now fails at the public boundary with its exact parameter
name. All lazy facades use thread-safe `Lazy<T>` publication, providing one send provider, publish
endpoint and client factory per owning scope even under concurrent access. Direct consume contexts
still forward the exact ambient context, while generic consume contexts preserve that context in
their send, publish and request-client composition.

| Admission | Files / lines | Manifest SHA-256 | Chain SHA-256 |
| --- | ---: | --- | --- |
| Source | 3 / 196 | `5dc43b5a9cd44dd6a1a9e9427eb62e594b033b5b10ebbf5ee4d8ccdac5783d3f` | `559a54e23cee5566b9c6829539e11b3449947c950a175aa3397b8053ded11fba` |
| Tests | 1 / 151 | `c7326b28384736ec88c7c5182a9cd651a66bd8f6e1978efc0b10955e05c9ae50` | `077c659913f09383768ec85a81e416ec80ab4c81c2f2d165392d1416adbcaba4` |

Manifest hashes cover the ordinally sorted `path<TAB>content-sha256` records with a terminal
newline. Chain hashes extend the Iteration 171 source/test chains with the corresponding manifest
hash. The manifests contain:

| Kind | File | Content SHA-256 |
| --- | --- | --- |
| Source | `BusScopedBusContext.cs` | `864a8656e8670c9d5ff96072e972c55225fe5a830754360f5be850b7902abde2` |
| Source | `ConsumeContextScopedBusContext.cs` | `e64b087f6e7adca6537639436f4cc3ab55239b6cb6e8a318c59e486e98575441` |
| Source | `ScopedBusContext.cs` | `cebed6a2169863d0d620d58ae59ab262f35afaa61b9a10d09f0226cbf5daf7ba` |
| Test | `ScopedBusContextTests.cs` | `fd5602a3bdc569a123cd9bbe9845637b0954d5f9d9819f7ed674c908344258fa` |

Cumulative personal source admission is 307/4,116 current C# files.

## Proof

`Constructors_RejectEveryMissingDependency` proves all twelve required parameters across all four
public constructors. `RootContexts_CacheOneFacadePerScopeAndForwardWrappedEndpointsAsync` drives 64
concurrent readers through both root-context variants, proves one facade set, correct wrapper types
and exact forwarding by the delegating variant. `ConsumeContexts_PreserveAmbientContextAndCacheOneFacadePerScopeAsync`
does the same for both consume variants and proves exact ambient-context identity through send,
publish and client paths.

The three requirement variants are embedded in `CoreRequirements.json`, final SHA-256
`9f5a427627884aca584c6dc04246ab115631f46f2957f8fda755cb2af3832509`.

Six successfully compiled single-cause mutants were killed and restored: remove one required guard
from each constructor form, replace the generic root send-provider cache with per-access creation,
and replace the generic consume publish cache with per-access creation. Each identity mutant made
63 of 64 concurrent snapshots observably different. A transient mutation-edit placement error did
not compile, was corrected, and is not counted. All product sources were restored to the reviewed
final bytes before the final gates.

Final focused Cobertura is `/private/tmp/vicione-servicebus-iteration-172-final.cobertura.xml`,
SHA-256 `c4c7357ac86ffe7797ed4e795f73d5da7810de692f1f43c9ca87154f92b3656f`.
All four executable target classes report 100% line and branch coverage. Every reported method has
complexity and CRAP 1; the interface has no executable lines. Unit sorted-display-name SHA-256 is
`83b8843a12b108d938f893f5ec8da847f0a5adbcab3772f0914a4b6f18e6e1bc`.

| Gate | Result |
| --- | --- |
| Focused owning tests | 3/3 passed |
| Full Core Release | 4,811/4,811 passed with suite parallelism disabled |
| Full EF unit | 248/248 passed |
| Strict Release product/unit/EF/local builds | 0 warnings, 0 errors |
| Product/test format | Exit 0 |
| Unit requirement projection | 1/1 passed |
| Local requirement projection | 1/1 passed |
| Mutation probes | 6/6 compiled mutants killed |

Core remains serialized because Iteration 155 demonstrated a pre-existing parallel-suite race.
Protected `review/**`, `TestResults/**` and `vicione-legacy/**` trees were not enumerated, read or
modified. Intended tag:
`servicebus-a-plus-iteration-172-scoped-bus-context-admission-2026-09-16`.

Whole-fork personal reading, global API/naming/coverage and configured external-provider acceptance
remain open. Remote publication remains an independent delivery step and cannot pause or deactivate
the active goal.
