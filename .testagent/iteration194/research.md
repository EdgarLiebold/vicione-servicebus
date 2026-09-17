# Iteration 194 research

## Scope

Twenty-three previously unadmitted saga pipeline-configuration, behavior/activity and state-machine
surface contracts, totaling 1,744 physical lines. The lead read every selected source completely
before delegating. Admission moves cumulative exact unique source coverage to 608/4,118 current
source files (14.764%).

- Pipeline configuration: four concrete connector/configurator/specification and middleware
  extension owners (597 lines).
- Behavior/activity contracts: eleven behavior-context, event/exception binder and activity-selector
  interfaces (765 lines).
- Runtime surface contracts: eight correlation, observer, accessor, modifier, visitor and unhandled
  event interfaces (382 lines).

## Existing conventions and static pairing

- The owning project is `tests/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj`; it uses
  xUnit v3 on Microsoft Testing Platform and already references `ViciOne.ServiceBus.Sagas`.
- Tests use `[Fact]`/`[Theory]`, exact exception/parameter assertions, reflection for declaration-only
  public surfaces, and focused runtime fakes only where behavior must be observed.
- The required Roslyn pairing scan covered 4,268 source and 1,322 test files: 2,193 paired and 2,075
  statically unpaired. It identifies some multi-declaration saga contracts as unpaired and recommends
  an Architecture test project; that recommendation is a parse-only heuristic and misses extension
  method calls and reflection-driven contract tests. The established A+ owning test project remains
  authoritative for this admission.
- Existing direct evidence includes saga partitioner tests and timeout configuration surface tests;
  behavior/activity types also appear in exception, recovery, condition and activity integration
  tests, but no single exact public-shape admission exists for this packet.

## Acceptance checklist

1. Pipeline configuration rejects every missing required collaborator and invalid concurrency,
   partition and optional limiter values at the public boundary.
2. Text partitioning preserves the selected/default encoding and rejects a null key result; Guid
   partitioning and observer registration preserve the exact supplied owner/factory semantics.
3. State-machine connector/specification creation validates ownership, correlation failures,
   value-type filtering, generic type mismatch and partial-connect cleanup without leaking handles.
4. Behavior/context, event/exception binder and activity-selector interfaces preserve exact
   inheritance, variance, generic constraints, overloads and Task-return shapes.
5. Correlation, observer, accessor, modifier, visitor, unhandled-context and visitable interfaces
   preserve exact public members, cancellation defaults and callback shapes.
6. Generated tests must compile, pass focused and namespace regressions, be mutation-sensitive, and
   remain visible to the full Core harness and requirement projection.

## Initial risks

- The connector constructor stores a required state machine without an explicit null guard and wraps
  all discovery failures as configuration errors; exact failure ownership needs pinning.
- Connector partial failure cleanup depends on disposal of every already-created handle.
- Partitioner validation may be delegated too late, and delegate-returned null keys are a runtime
  boundary distinct from a null delegate.
- Declaration-only interfaces can regress silently through variance, generic constraints, hidden
  members, cancellation defaults or overload collapse unless reflected directly.
