# Iteration 196 research

## Scope

Ten previously unadmitted saga state-machine extension sources totaling 726 physical lines. The
lead read every selected source completely before delegation. Admission moves cumulative exact
unique source coverage to 634/4,118 current source files (15.396%).

- Activity/request composition: container-resolved activities, request lifecycle activities and
  exception-aware service-address delegates.
- State query and runtime access: query/filter construction, state reads, explicit transitions and
  next-event introspection.
- Behavior composition: missing-instance redelivery, synchronous/asynchronous delegate/factory
  activities, transitions and finalization across normal and faulted binders.

## Existing conventions and risks

- The owning xUnit v3/Microsoft Testing Platform project is
  `tests/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj`.
- Iteration 194's Roslyn pairing scan remains the current repository-wide static baseline. Focused
  search finds only narrow existing coverage for missing-instance null input and indirect
  transition execution; the extension overload families are not directly pinned as a portfolio.
- Closely related overloads apply inconsistent immediate argument validation. Invalid binders,
  callbacks, factories, expressions, states and request descriptors can therefore fail later with
  implementation-detail diagnostics or collaborator effects.
- Query/filter helpers combine user and state expressions and must preserve both predicates,
  compile semantics and exact state selection.
- Cancellation must short-circuit explicit transitions and flow through state access and
  introspection without performing state-machine work.
- Faulted transition/finalization overloads wrap activities differently from normal binders; exact
  target lookup, accessor identity, wrapper shape and returned binder identity require direct
  tests.

## Acceptance checklist

1. Every extension overload preserves exact public shape, generic constraints and return type.
2. Required owners and callbacks fail immediately with exact parameter names before collaborator
   effects; symmetric overloads follow one boundary policy.
3. Container/request activities preserve selector, binder, request-event and factory identity.
4. Query/filter composition preserves the caller predicate plus selected-state predicate and
   exposes equivalent query and compiled-filter results.
5. State access, explicit transition and next-event introspection preserve token, context, state,
   event and cancellation semantics.
6. Then/execute, transition/finalize and missing-instance redelivery preserve activity/factory,
   target/accessor, validation and configuration-exception ownership.
7. Focused evidence is assertion-rich, mutation-sensitive, warning-free and reconciled into the
   central requirement projection before the complete Core/EF gates.
