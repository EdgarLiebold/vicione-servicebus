# Iteration 142 — status

## Scope and admission

- Parent: `f87495e98ec78e388ae288593de40c831f39aaad` (Iteration 141).
- Personally read before correction: every one of the 87 then-current C# files / 7,732 lines under `src/ViciOne.ServiceBus/Initializers`.
- Frozen post-correction source admission: 87 unique current files / 7,742 lines; manifest SHA-256 `1a9235e43ede49c300e7eeecd0596538c8b4554dede9fdf7e842245888e30024`; source-chain SHA-256 `a61575c3dd9786be3914293460268ed42df48be5f553c226a190b3f349cb2fb8`.
- Frozen owning-test admission: seven fully read test/driver files / 1,446 lines; manifest SHA-256 `38cacda58f8995d529a51a9bf71b32f0ef4eb021cf32effb3e2f982ec2e96a5d`; test-chain SHA-256 `e9012e3f64f389322a1223afb8ddd8c33a77f95e7e3f75f1dbbed7770e76ec46`.

## Correction

- `InitializerConventionRegistry` now publishes one `ReadOnlyCollection` view instead of a mutable array exposed as `IReadOnlyList`; indexed element replacement is rejected and snapshot identity remains stable.
- `ConventionTypeCache` and `MessageInitializerCache<TMessage>` now use ephemeron ownership through `ConditionalWeakTable`, allowing a collectible type and the closed value graph that references it to be reclaimed together.
- `TypeConverterCache` now retains only fixed built-in converter instances strongly. Runtime enum, named-value and nullable converter graphs, including negative lookups and declared contracts, are weakly keyed.
- Live-key identity, thread-safe one-time initialization, unsupported-conversion behavior and public API signatures are preserved.
- The remaining initializer-layer `WaitAsync(cancellationToken)` sites are intentionally caller-owned task boundaries and retain their documented local-wait cancellation semantics.

## Handwritten evidence

- The unchanged-product baseline compiled cleanly and failed 0/4 for the four intended reasons: mutable convention element publication and retained collectible type/assembly pairs in each target cache.
- Corrected focused runs passed 4/4, then the expanded identity/contention fixture passed 7/7. Existing type-converter regression passed 25/25; registry/message-cache regression passed 2/2.
- Assertion review found no assertion-free or tautological case: every retention test observes both the runtime type and assembly, the convention cache remains explicitly rooted, repeated values are reference-identical and the 64-way contention forms verify one published live value.
- Pseudo-mutation review killed four independently compiled single-cause regressions: mutable array publication, strong convention key, strong message-initializer key and a strong root for dynamic converters. The first snapshot mutation run used stale test output and was excluded; rebuilding the owning test project produced the required 0/1 red result.
- Restored final focused run: 7/7.

## Static pairing

The mandatory pairing workflow ran exactly once in isolated directory `/private/tmp/vsb-iteration142-pairing.YmxOFr`: 90 source files, 52 tests, 81 paired and nine unpaired. All four corrected cache files are paired. The nine heuristic misses are `InitializerConvention`, six factory/inspector abstractions, `IMessageInitializerFactory` and `MessageFactoryCache`; reflection/factory behavior can be owned indirectly, so this is navigation evidence rather than assertion or runtime coverage proof.

## Terminal gates

- Strict Release `ViciOne.ServiceBus.slnx` build: 0 warnings / 0 errors in 56.85 s.
- Strict Release `ViciOne.ServiceBus.Tests.Unit.slnx` build: 0 warnings / 0 errors in 2:11.12.
- Engineering and Unit formatting verification: both exit 0 with no changes.
- Unfiltered native Core: 4,799/4,799 passed in 23.841 s; 0 failed/skipped/other.
- Parent multiset reconciliation: all 4,792 parent names/multiplicities retained, no removals, exactly seven intended additions. Canonical sorted-name/newline SHA-256: `6f8c418b9a75c807825e4758f8cc5dd92e80b16cb657b27cfe32365c249b09f5`; final CTRF SHA-256: `39aae37df60d67c58406bc29d8894b630faf43455edf6e95485447c51afa25a2`.

## Selected coverage and risk

- Configuration SHA-256: `3838fc1b6b73f21d8fb447beecad11a5c91cca053bd6c31f906a6036f248464d`.
- Separate complete coverage run: 4,799/4,799; selected nine-assembly graph 49,677/61,228 lines (81.1344%) and 17,158/23,340 branches (73.5133%). Cobertura SHA-256 `3aaf6183ac3b37362ef9a35f21e1381c8e9ee4177d18722b581124cdff9fcdac`; coverage CTRF SHA-256 `f94bcbd94089a7881e771ea1314259486130984869e3bb9b1cfa461b9a89695c`.
- Faithful CRAP calculation evaluated 18,155 methods; 121 exceed 30. The canonical below-80%-method-line set contains 4,481 methods, including 3,690 at 0%, with identity/newline SHA-256 `7055d81d0514f003483f6af57803c5e2a1bcac3e806bb2d539079832128085d0`.
- `RequestRateAlgorithm..ctor` remains the largest selected hotspot: complexity 34, 0% coverage, CRAP 1,190. These selected-graph risks are not attributed to this cache packet.

## Checkpoint state

Raw evidence remains under `/private/tmp/vsb-iteration142-*` and the isolated pairing directory. Protected review, TestResults and legacy trees were not read or modified. Normal commit, annotated tag, atomic private push and independent three-ref verification remain the final checkpoint operations.

Whole-fork personal source/comment reading, global API/new-parameter behavior coverage, global bidirectional naming/type/file/directory consistency, legacy/dummy/directive absence, whole-fork coverage/CRAP and real durable-provider/external acceptance remain open after this connected subsystem packet.
