# C24A Cache Eviction Ordering Evidence

## Frozen correction

- parent: `7112814067d91d1c17d4db21151f85b8328cc540`
- commit: `472a1ac739785f242f31bccc9aa9b95c5e7320fc`
- tree: `959c9c99a267a70e74d1075d82fcc0bbc80fbd0c`
- `NodeTracker.cs` SHA-256: `faa000975f1e286475dc26e6397239a8a6e16588119d5eeb722b93ed870b263e`
- `GreenCacheCapacityTests.cs` SHA-256:
  `bef30f8d659d7e341dcebb42ba86e2ccf958f9a37460d2ee5d23d6471cc8b06c`

## Defect and correction

`EvictNode` previously invoked `ValueRemoved` before `BucketNode.Evict`. An observer could therefore
receive a removal notification while the node remained valid and visible through `GetAll`. The
parallel unfiltered profile reproduced the race.

The product now completes `node.Evict()` before publishing `ValueRemoved`. The test observer records
whether any removal callback received a still-valid node. Capacity tests also wait on every actual
add event corresponding to the dynamically offered value count; they do not infer observer
completion from `Index.Get` completion.

## Negative and acceptance evidence

- Reversing the product order back to observer-before-eviction built with 0 warnings and 0 errors,
  then failed the focused usage-aware test with exit 2.
- The corrected focused usage-aware test passed 10 consecutive independent native MTP processes.
- The full Release build passed with 0 warnings and 0 errors.
- Two consecutive unfiltered UnitArchitecture processes passed 805/805 with 0 skipped.
- LocalIntegration passed 3/3 with 0 skipped.
- No sleep, polling delay, wall-clock assertion, skip, or local timeout was introduced. All waiting
  remains event-driven and bounded by the single typed repository operation timeout.
