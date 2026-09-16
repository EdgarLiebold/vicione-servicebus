# Iteration 142 — initializer cache ownership and immutable publication

## Purpose and source admission

This connected packet completes a personal source/comment read of the entire current Initializers subsystem and corrects four related ownership/publication defects. Before productive correction, all 87 then-current C# files / 7,732 lines under `src/ViciOne.ServiceBus/Initializers` were read. The frozen post-correction manifest contains the same 87 unique files / 7,742 lines.

| Admission | Manifest SHA-256 | Chain SHA-256 |
| --- | --- | --- |
| Initializers source | `1a9235e43ede49c300e7eeecd0596538c8b4554dede9fdf7e842245888e30024` | `a61575c3dd9786be3914293460268ed42df48be5f553c226a190b3f349cb2fb8` |
| Owning tests/drivers | `38cacda58f8995d529a51a9bf71b32f0ef4eb021cf32effb3e2f982ec2e96a5d` | `e9012e3f64f389322a1223afb8ddd8c33a77f95e7e3f75f1dbbed7770e76ec46` |

The source chain extends Iteration 141 manifest hash `8de8c8dfb0f28b93eba2ffe0d7dc8407d883acc7ff572521451a557b4e68521d`; the test chain extends `c6ab9271b724cf05aba63dea68367c8904f48f14b3972314c51a4669b582870e`. The owning-test admission contains seven fully read files / 1,446 lines.

## Findings and correction

| Finding | Risk | Correction |
| --- | --- | --- |
| `InitializerConventionRegistry.Conventions` returned an array as `IReadOnlyList`. | A consumer could cast it to `IList` and replace an element after the registry claimed to be frozen. | Publish one cached `ReadOnlyCollection` wrapper; preserve original element and snapshot identities. |
| `ConventionTypeCache` strongly keyed closed runtime contracts. | Collectible assemblies remained rooted for process lifetime; values also close over their key types. | Use `ConditionalWeakTable<Type, Cached>` so key/value cycles are ephemeron-owned. |
| `MessageInitializerCache<TMessage>` strongly keyed arbitrary runtime input types. | Static generic cache lifetime pinned each collectible input type and its closed initializer graph. | Use a weak type key whose `CachedInitializer` retains execution-and-publication lazy creation. |
| `TypeConverterCache` strongly retained synthesized converter instances in both a dictionary and global list. | Runtime enum/named/nullable converter graphs pinned collectible assemblies. | Keep only fixed built-ins in the strong list; weak-key exact and declared contracts, including negative lookups. |

The public API surface, supported conversion matrix, unsupported false result, live-key reference identity and single-value concurrency semantics remain unchanged. Cache misses are serialized where converter construction may recursively compose nullable converters.

## Async-boundary classification

The whole subsystem read also reclassified every remaining `WaitAsync(cancellationToken)` site. They wait on caller-owned task-valued inputs or values: task source/fallback input, task copy input, asynchronous provider inner value and message-data value. Provider, converter, variable and initializer operations accepted by the library are observed to their original terminal outcome. `TaskInitializerExtensions` documentation and existing lifetime tests expressly require prompt local wait cancellation for the caller-owned category, so no async ownership change belongs in this packet.

## Handwritten behavior evidence

Five public test methods produce seven native cases:

| Requirement variant | Cases | Principal assertions |
| --- | ---: | --- |
| Published convention snapshot is element-immutable | 1 | Indexed replacement throws when the exposed value has a list mutation surface; original element and repeated snapshot reference remain exact. |
| Convention cache does not pin collectible contracts | 1 | Repeated value identity; both runtime type and assembly die while the cache is kept alive. |
| Message initializer cache does not pin collectible input types | 1 | Repeated initializer identity; both runtime type and assembly die behind the rooted static cache. |
| Type-converter cache does not pin collectible enums | 1 | Public resolution succeeds twice with exact converter identity; both runtime enum and assembly die. |
| Live cache keys reuse one value under contention | 3 | 64 results per cache form, all reference-identical; convention factory called exactly once. |

Collectible fixtures use `AssemblyBuilderAccess.RunAndCollect`, no-inlining population boundaries, weak type and assembly references, and bounded forced full collections. The convention cache is explicitly kept alive through collection. The fixture has no assertion-free path, timing-only outcome or mock-only substitute for GC ownership.

The unchanged-product baseline compiled cleanly and passed 0/4: the snapshot mutation did not throw, and all three collectible type/assembly pairs stayed alive. The first corrected packet passed 4/4; the expanded contention packet passed 7/7. Existing TypeConverters regression passed 25/25 and existing registry/message-cache regression passed 2/2.

## Test-gap and mutation review

The mandatory static pairing engine ran once against isolated directory `/private/tmp/vsb-iteration142-pairing.YmxOFr`: 90 source files, 52 tests, 81 paired and nine unpaired. All four corrected files are paired. The nine heuristic misses are:

- `Initializers/Conventions/InitializerConvention.cs`
- `Initializers/Factories/HeaderInitializerInspector.cs`
- `Initializers/Factories/IHeaderInitializerInspector.cs`
- `Initializers/Factories/IMessageInitializerBuilder.cs`
- `Initializers/Factories/IPropertyInitializerInspector.cs`
- `Initializers/Factories/InputHeaderInitializerInspector.cs`
- `Initializers/Factories/PropertyInitializerInspector.cs`
- `Initializers/IMessageInitializerFactory.cs`
- `Initializers/MessageFactoryCache.cs`

Those factory/reflection paths may be behaviorally owned through higher-level tests; pairing is a navigation heuristic, not assertion-strength, behavior or coverage proof.

Four one-at-a-time, compile-valid mutations were killed and immediately restored:

| Mutation | Focused result | Causal observation |
| --- | --- | --- |
| Publish an array instead of the read-only wrapper | 0/1 | No `NotSupportedException` on element replacement. |
| Replace the convention ephemeron with a strong concurrent dictionary | 0/1 | Runtime contract `Type` remained alive. |
| Replace the message-initializer ephemeron with a strong concurrent dictionary | 0/1 | Runtime input `Type` remained alive. |
| Add a process-lifetime strong list for dynamic converters | 0/1 | Runtime enum `Type` remained alive. |

The first mutation was initially executed against stale test output and passed; that artifact was rejected. Rebuilding the owning test project against the mutation produced the required red result. After all restorations and a clean owning-project rebuild, the fixture returned to 7/7 green.

## Terminal validation

| Gate | Result | Duration |
| --- | --- | ---: |
| `ViciOne.ServiceBus.slnx` strict Release build, no restore | 0 warnings / 0 errors | 56.85 s |
| `ViciOne.ServiceBus.Tests.Unit.slnx` strict Release build, no restore | 0 warnings / 0 errors | 2:11.12 |
| Engineering format verification | Exit 0, no findings | — |
| Unit format verification | Exit 0, no findings | — |
| Unfiltered native Core | 4,799 passed; 0 failed/skipped/other | 23.841 s |
| Separate native Core coverage | 4,799 passed; 0 failed/skipped/other | 25.330 s |

Builds disabled build servers and shared compilation, used one MSBuild node and no node reuse, and did not combine the strict solution gate with `--no-incremental`.

Exact CTRF multiset reconciliation retained every one of the 4,792 parent names/multiplicities, removed none and added exactly the seven new native cases. The current canonical sorted-name/newline SHA-256 is `6f8c418b9a75c807825e4758f8cc5dd92e80b16cb657b27cfe32365c249b09f5`; the final Core CTRF SHA-256 is `39aae37df60d67c58406bc29d8894b630faf43455edf6e95485447c51afa25a2`.

## Selected coverage and CRAP risk

Coverage uses repository configuration SHA-256 `3838fc1b6b73f21d8fb447beecad11a5c91cca053bd6c31f906a6036f248464d`. The selected nine-assembly product graph measured 49,677/61,228 lines (81.1344%) and 17,158/23,340 branches (73.5133%). Cobertura SHA-256 is `3aaf6183ac3b37362ef9a35f21e1381c8e9ee4177d18722b581124cdff9fcdac`; coverage CTRF SHA-256 is `f94bcbd94089a7881e771ea1314259486130984869e3bb9b1cfa461b9a89695c`.

The documented CRAP formula evaluated 18,155 methods and found 121 scores above 30. The canonical below-80%-method-line set contains 4,481 methods, including 3,690 at zero line coverage; complete identity/newline SHA-256 is `7055d81d0514f003483f6af57803c5e2a1bcac3e806bb2d539079832128085d0`.

| Rank | Selected method | Complexity | Line coverage | CRAP |
| ---: | --- | ---: | ---: | ---: |
| 1 | `RequestRateAlgorithm..ctor` | 34 | 0% | 1,190 |
| 2 | `DashedHexFormatter.Format` | 20 | 0% | 420 |
| 3 | `NewIdGenerator.NextGuid` | 18 | 0% | 342 |
| 4 | `NewId.CompareTo` | 16 | 0% | 272 |
| 5 | `AssemblyFinder.FindAssemblies.MoveNext` | 16 | 0% | 272 |
| 6 | `StateMachineSagaMessageFilter.SendAsync.MoveNext` | 38 | 48.8% | 231.39 |

These are selected Core-graph metrics, not whole-fork coverage and not causal attribution to the cache correction. Optional ReportGenerator/HTML work was not requested or run. The bundled PowerShell analyzer could not execute because `pwsh` is absent; the same documented unique-line CRAP formula and canonical method identity were computed directly from Cobertura and cross-checked against the exact known Iteration 141 totals/hash.

## Checkpoint and continuing goal

Raw artifacts remain under `/private/tmp/vsb-iteration142-final-core`, `/private/tmp/vsb-iteration142-coverage`, `/private/tmp/vsb-iteration142-*mutation*`, the baseline/candidate directories and the isolated pairing directory. Protected review, TestResults and legacy trees were neither read nor modified.

The intended annotated tag is `servicebus-a-plus-iteration-142-initializer-cache-ownership-and-immutable-publication-2026-09-16`. Commit, atomic private push and independent branch/tag-object/peeled-tag verification occur after final non-protected diff review; this report does not pre-claim them.

Whole-fork personal source/comment completion, global API/new-parameter behavior coverage, global bidirectional naming/type/file/directory consistency, legacy/dummy/directive absence, whole-fork coverage/CRAP and real durable-provider/external acceptance remain open. This subsystem packet proves the Initializers read and the four connected cache/publication corrections only.
