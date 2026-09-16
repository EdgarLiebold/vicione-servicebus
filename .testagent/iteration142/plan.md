# Iteration 142 — handwritten test and correction plan

1. Add five handwritten causal methods (seven native cases) and unique requirement bindings before changing productive cache behavior.
2. Prove that a published registry snapshot rejects indexed element replacement and retains the original convention/reference identity.
3. Populate a live `ConventionTypeCache` with a collectible runtime contract, then prove the type and assembly can be reclaimed while the cache remains rooted.
4. populate `MessageInitializerCache<TMessage>` from a collectible runtime input type and prove the closed initializer graph does not pin that type or assembly.
5. Resolve a converter for a collectible runtime enum through the public `TypeConverterCache` API and prove the dynamic converter/cache graph is reclaimable.
6. Capture the unchanged-parent behavioral baseline. Compiler or sandbox failures are not behavioral evidence.
7. Publish a read-only registry wrapper; replace both runtime-type dictionaries with ephemeron caches; split stable built-in converters from weakly keyed dynamically synthesized converters.
8. Re-run focused tests, then review assertion quality, pseudo-mutation gaps, exact identity semantics and all affected comments.
9. Compile and kill one single-cause mutation for each corrected root: mutable snapshot, strong convention key, strong initializer key and strong dynamic converter retention.
10. Run strict Release builds, both formatting gates, unfiltered Core reconciliation and selected coverage/CRAP. Freeze the 87-file source manifest and owning-test admission, update evidence, commit, annotated-tag, atomically push and independently verify all three remote refs.

## Requirement-to-test map

| Requirement | Planned method |
| --- | --- |
| Published convention snapshot is element-immutable | `ConventionSnapshot_RejectsElementReplacement` |
| Convention dispatch cache does not pin runtime contract types | `ConventionTypeCache_DoesNotRetainCollectibleContractTypes` |
| Message initializer cache does not pin runtime input types | `MessageInitializerCache_DoesNotRetainCollectibleInputTypes` |
| Dynamic type-converter cache does not pin runtime enum types | `TypeConverterCache_DoesNotRetainCollectibleEnumTypes` |
| All three caches preserve one live value under contention | `Caches_ReuseOneLiveValueUnderContention` (three forms) |
