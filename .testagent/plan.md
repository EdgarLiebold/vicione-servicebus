# Plan — native test reconstruction

## Target

Rebuild every meaningful inherited test purpose as a source-owner test under `tests2`, using xUnit 4
on Microsoft Testing Platform 2. The permanent tree mirrors `src`; only `Architecture`, `Testing`,
and `Tools` are non-product owners. No phase name, generic `Core` bucket, Python test platform,
VSTest path, receipt, interceptor, or execution sentinel is part of the design.

## Current profile floors

- `UnitArchitecture`: 652 unfiltered cases;
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

## Accepted cohort — endpoint-name formatters

`Configuration/EndpointNaming/EndpointNameFormatterTests.cs` in the existing core-owner test project
contains nine ordinary xUnit methods that materialize 17 cases:

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

The embedded core requirement projection has one row per method and the inherited disposition has
one terminal row per old case. Focused and full Release checks, bounded formatting/analyzers, static
assertion and gap review, and four targeted product mutations cover formatting, generic ownership,
instance-id sanitization, and suffix-only rejection. The predeclared UnitArchitecture floor is 635.
Only `tests/ViciOne.ServiceBus.Tests/EndpointName_Specs.cs` was removed after all 17 rows closed; no
product file changed.

## Accepted cohort — message URNs

Use the existing `ViciOne.ServiceBus.Abstractions.Tests` project and add two source-mirrored files:

1. `MessageUrnTests.cs` owns attributed/default/custom URNs, attributed arrays, plain/nested/closed
   generic names, consistent null/open-generic rejection, and four `Deconstruct` shapes;
2. `Attributes/MessageUrnAttributeTests.cs` owns null, empty, whitespace, duplicate-default-prefix,
   and invalid-custom-URI validation.

Thirteen ordinary xUnit methods materialize 17 cases. One passive requirement-projection row per
method and the terminal disposition map close the 12 old MessageUrn fixture obligations, the two
attribute obligations from `MessageType_Specs.cs`, and the existing Deconstruct gap. The validation
methods assert the public exception contract directly and deliberately do not preserve the
inherited static-cache wrapper. The UnitArchitecture floor is 652. Bounded format/analyzer checks,
assertion/gap review, and five one-cause mutations for derived names, array attribute propagation,
runtime-overload validation, constructor validation, and deconstruction pass. Both complete Release
profiles pass at 652/652 and 3/3. `MessageUrnSpecs.cs` was removed only after its 12 rows closed;
`MessageType_Specs.cs` remains untouched while its array transport behavior is open.
