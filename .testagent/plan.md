# Plan — native test reconstruction

## Target

Rebuild every meaningful inherited test purpose as a source-owner test under `tests2`, using xUnit 4
on Microsoft Testing Platform 2. The permanent tree mirrors `src`; only `Architecture`, `Testing`,
and `Tools` are non-product owners. No phase name, generic `Core` bucket, Python test platform,
VSTest path, receipt, interceptor, or execution sentinel is part of the design.

## Current profile floors

- `UnitArchitecture`: 635 unfiltered cases;
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

The complete Abstractions, Analyzer, SignalR, MessagePack, and StateMachineVisualizer owners and the
Cron-expression scheduling cohort of the core owner are reconstructed. MessagePack
owns 49 native cases that replace 67 inherited module obligations and the six MessagePack-body
obligations previously assigned to the inherited mixed core fixture, including the
real in-memory pipeline and redelivery boundaries. It does not reuse the inherited TestFramework or
the old parameterized fixture hierarchy. Product behavior is unchanged; the only product-project
change is the signed friend grant needed to inspect the internal hardened option owner directly.
The accepted Git commit and tree, rather than a self-referential hash inside this file, are the review
identity. StateMachineVisualizer owns seven native behavior cases plus its projection case and maps
all nine inherited visualizer obligations individually. The Cron cohort maps all 58 inherited
obligations, uses deterministic UTC or test-owned time zones, and hardens repeated-whitespace
parsing with a minimal product correction.

## Current cohort — endpoint-name formatters

Create `Configuration/EndpointNaming/EndpointNameFormatterTests.cs` in the existing core-owner test
project. Nine ordinary xUnit methods materialize 17 cases:

1. three snake-case word/digit/acronym variants;
2. nested namespace inclusion;
3. namespace plus prefix;
4. generic-consumer naming from its message type;
5. namespaced message naming;
6. four default/kebab/snake prefix-separator variants;
7. the prefix-free kebab message name;
8. the concrete consumer-definition plus kebab instance-id contract;
9. four exact `ConfigurationException` boundaries for suffix-only consumer, saga, execute activity,
   and compensate activity types.

Extend the embedded core requirement projection by one row per method and create a 17-row inherited
disposition. Run a focused locked restore/build/test first, then bounded format/analyzer checks,
assertion and gap review, targeted one-cause mutations of formatting, generic ownership,
instance-id sanitization, and suffix-only rejection. Finally run the full Release build and both
unfiltered materialized profiles with a predeclared UnitArchitecture floor of 635. Delete only
`tests/ViciOne.ServiceBus.Tests/EndpointName_Specs.cs` after all 17 rows are closed; do not change a
product file unless a native test first reproduces a product defect.
