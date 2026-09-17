# Iteration 195 research

## Scope

Sixteen previously unadmitted saga role, repository-query and registration-runtime sources totaling
467 physical lines. The lead read every selected source completely before delegation. Admission
moves cumulative exact unique source coverage to 624/4,118 current source files (15.153%).

- Public saga roles: four message-role interfaces, state-machine/version markers and the public
  saga factory delegate.
- Repository contracts: load/query/composite repositories, query factory, missing-instance
  redelivery configuration and query property extraction.
- Registration runtime: consumer-kind configuration/dispatch and saga/definition ownership
  classification.

## Existing conventions and risks

- The owning xUnit v3/Microsoft Testing Platform project is
  `tests/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj`.
- Iteration 194's Roslyn pairing scan covered 4,268 source and 1,322 test files; its static pairing
  result remains a heuristic because reflection contracts and extension-method calls are not always
  paired syntactically. Dedicated compiled tests remain the authoritative evidence here.
- `IObserves<TMessage, TSaga>` exposes `TSaga` inside an expression but does not currently declare
  the saga/class constraint carried by the surrounding repository API.
- Both query-property extension overloads dereference the query before an explicit public boundary
  check, risking an implementation-detail null diagnostic.
- Consumer-kind dispatch has several runtime-type, delegate-shape, exclusion, ordering and
  observation branches whose input/side-effect ownership must be pinned directly.
- Repository and marker interfaces are declaration-only but can regress through variance,
  inheritance, generic constraints, cancellation defaults, nullability annotations or Async naming.

## Acceptance checklist

1. Public role interfaces preserve exact exclusions, inheritance, variance and generic constraints;
   the observed saga type is constrained consistently with saga ownership.
2. Repository contracts preserve inheritance, query/load composition, Task/Async naming,
   cancellation defaults, out-nullability and exact parameter shapes.
3. Query-property extraction validates the query immediately and distinguishes supported captured
   property expressions from unsupported expression shapes without corrupting the out value.
4. Consumer-kind configuration preserves registration order, filtering, exact callbacks, generic
   diagnostics, dispatcher eligibility and test-harness observation identity.
5. Saga metadata classification distinguishes saga types, definitions, owned/non-owned state
   machines and consumer-kind exclusion without broad false positives.
6. Focused evidence is assertion-rich, mutation-sensitive, warning-free and reconciled into the
   central requirement projection before the complete Core/EF gates.
