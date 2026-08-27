# Entity Framework Core / cache correction 06

## Frozen subject

- Technical commit: `9a3b9fb98f8ad1669570c16a2d18ef9642520d92`
- Technical tree: `52385f1b94f8d5cbc384f6690c9a765294b6815f`
- Parent: `f43db446c915d6554510a9eed60e019876693205`
- Authorizing architecture commit: `156f3655998319f26a46296d7aa03812d0a77503`

This evidence supersedes correction 05 as the active cache conclusion. Earlier evidence remains immutable
historical input. The EF retry contract and its 90 obligations / 156 execution identities remain
unchanged: 77 executing identities, 39 provider-neutral consolidations and 40 explicit SQL Server
External owners.

## Product result

The final cache design keeps the existing concurrent/unordered external observer contract and does
not introduce an unbounded publication queue. It closes the concrete state-safety defects:

1. Cleanup reservation is single-flight across rejection, synchronous callback-then-throw,
   reentrancy and follow-up handoff.
2. Ring segmentation never overwrites a live bucket while cleanup is queued.
3. Cleanup never clears the active bucket and applies MinAge to every eviction cause, including
   ring pressure, expiration and capacity.
4. Source-bucket count transfer is performed exactly once under the tracker lock; a late
   `Used` callback cannot decrement a reused bucket generation.
5. Bucket and node state use explicit volatile/interlocked publication. A completed eviction never
   exposes the stored value again.
6. Tracker state is committed under its lock; clock, observer, usage-event and disposal callbacks run
   outside that lock. Reset disposal begins after reset/add publication and remains non-blocking.
7. Index stale-add/stale-remove guards protect a newer same-key generation.

The separate greenfield question of a fail-closed internal index projection plus bounded external
notification channel is recorded in `TODO.md`; no incomplete queue design is present here.

## Positive execution

Every command in `FINAL_RESULTS.json` ran from the frozen technical tree. Raw output is present,
not inferred:

- locked Engineering restore: exit 0;
- Engineering Release build: exit 0, 0 warnings, 0 errors;
- unfiltered UnitArchitecture profile: 1914/1914, 0 failed, 0 skipped;
- LocalIntegration with fresh run-scoped PostgreSQL and Azurite: 70/70, 0 failed, 0 skipped;
- four focused new owners: each 1/1, 0 failed, 0 skipped, each with CTRF.

## Mutation closure

`MUTATION_MANIFEST.json` binds exact target, baseline bytes, replacement, occurrence index,
mutant hash, expanded command shape, raw logs and CTRF for M16-M23. All eight mutants build and are
killed by their owning tests with exit 2 for their individual reason. Post-restore hashes equal the
technical tree.

A ninth exploratory mutation removed only the explicit current-bucket predicate. It is honestly
classified `EQUIVALENT_NOT_COUNTED`: under the shared tracker lock, the current bucket carries the
active sentinel and cannot satisfy `IsOldEnough`. The explicit predicate remains as a readable
defense; no mutation score is claimed for it.

## Structural closure

- Technical delta: 17 paths, all within architecture scope.
- Core requirement projection: 768 unique rows, no duplicate or missing new carrier.
- UnitArchitecture floor: 1914 in CI, architecture guard, README, build documentation and active plan.
- Source/test namespace mirroring remains unchanged.
- xUnit 4 / Microsoft.Testing.Platform v2 remains the native runner architecture.
- No old test project was reintroduced; no product behavior was weakened to satisfy a test.
