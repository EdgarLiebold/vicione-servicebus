# Plan — native test reconstruction

## Target

Rebuild every meaningful inherited test purpose as a source-owner test under `tests2`, using xUnit 4
on Microsoft Testing Platform 2. The permanent tree mirrors `src`; only `Architecture`, `Testing`,
and `Tools` are non-product owners. No phase name, generic `Core` bucket, Python test platform,
VSTest path, receipt, interceptor, or execution sentinel is part of the design.

## Current profile floors

- `UnitArchitecture`: 402 unfiltered cases;
- `LocalIntegration`: 3 unfiltered cases.

The floors are accepted lower bounds, not completeness evidence. Each executable project embeds one
durable product-requirement projection and compares it with passive metadata compiled into the same
assembly. The inherited semantic ledger remains the migration input until every disposition is
closed; it is never the runtime test architecture.

## Implementation order

1. Complete one coherent source-owner cohort from product source, inherited behavior evidence, and
   the semantic ledger.
2. Keep unit, local-integration, broker/database, and external-resource profiles separate.
3. Run locked restore, Release build, unfiltered MTP tests, bounded formatting, static test-quality
   review, and targeted false-green mutations. Only the Lead starts .NET/MSBuild processes.
4. Commit and push a stationary accepted cohort before beginning the next one.
5. Remove an inherited file only when all meaningful behavior it owns has an accepted replacement
   or an explicit non-product/non-executing disposition.
6. Remove the inherited runner and TestFramework only after complete closure, then atomically rename
   `tests2` to `tests` and update every solution, CI, documentation, and build path.

## Current work

The complete Abstractions and Analyzer owners are remotely preserved. The current cohort adds the
complete hermetic SignalR owner: 26 executing behaviors replace the inherited local, scale-out, group,
user, connection, failure-containment, and JSON-boundary cases; two fully commented inherited methods
are explicitly disposed as non-executing. Five additional cases close the untested multi-target,
empty-target, and MessagePack paths. The project uses the product's in-memory ServiceBus and a minimal
owner-specific SignalR connection environment, with no dependency on the inherited TestFramework.
Product and inherited-test source remain unchanged. The accepted Git commit and tree, rather than a
self-referential hash inside this file, are the review identity.
