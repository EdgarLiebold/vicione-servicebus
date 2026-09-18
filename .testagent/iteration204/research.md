# Iteration 204 research

## Admission scope

The lead read all eleven sources completely before delegation:

- `ConstructorSagaInstanceFactory.cs` (30 lines)
- `DefaultSagaFactory.cs` (43 lines)
- `PropertySagaInstanceFactory.cs` (33 lines)
- `HasValueTypeSagaQueryPropertySelector.cs` (32 lines)
- `NotDefaultValueTypeSagaQueryPropertySelector.cs` (33 lines)
- `SagaQueryPropertySelector.cs` (32 lines)
- `ISagaQueryPropertySelector.cs` (14 lines)
- `SagaMetadataCache.cs` (69 lines)
- `SagaMessageConnectorDescriptor.cs` (63 lines)
- `ISagaMessageSpecification.cs` (46 lines)
- `ISagaSpecification.cs` (21 lines)

These 416 physical lines are newly unique. Cumulative exact unique source coverage becomes
674/4,118 files (16.367%).

## Findings to test

- Constructor- and property-based saga factories compile delegates once, but their constructor
  admissibility, correlation assignment and diagnostics need direct contract coverage.
- `DefaultSagaFactory` has two equivalent correlation boundaries and a send path that must preserve
  context, created instance, logging and exact downstream task/exception behavior.
- All three query-selector implementations accept callbacks without owned guards; their selector,
  context, output-assignment and success predicates need explicit null/default/value matrices.
- Metadata discovery must remain deterministic across the four saga role families, filter invalid
  message types, select the correct instance-factory precedence and retain a stable invalid-factory
  diagnostic.
- Connector descriptors defer four factory activations independently. Their type boundaries,
  generic pairing, lazy identity and failure atomicity require direct coverage.
- The three public specification/selector contracts need exact reflection coverage for
  accessibility, inheritance, variance, constraints, members, nullability and callback/pipe shapes.

## Test strategy

Use three disjoint Sol 5.6 xhigh workstreams for instance factories, query selectors and
metadata/descriptor/specification contracts. Each owns only its named sources and one new test
file. Central integration owns requirement projection, formatting, serial builds, regressions,
compiled mutation probes, coverage/CRAP, manifests, evidence, commit and annotated tag.
