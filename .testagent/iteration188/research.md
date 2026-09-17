# Iteration 188 research

## Scope

This packet admits the Courier dependency-injection activity-scope lifecycle, providers,
registrations and scoped factories: 17 source files / 1,047 baseline lines. The lead personally
reads every source file and comment before integration.

Direct owning baselines are `ActivityScopeLifecycleTests`, `CourierActivityScopeProviderTests`,
`ActivityRegistrationLifecycleTests`, `CourierActivityFactoryContractTests` and
`CourierRegistrationBoundaryTests`. Existing evidence is substantial; this packet targets remaining
API-shape, dual-failure, idempotency, cancellation, ownership and registration-transition gaps.

## Disjoint ownership

- Agent A: execute/compensate scope providers and their internal contracts.
- Agent B: activity-scope lifetime and created/borrowed scope contexts.
- Agent C: activity registrations/configurators and scoped activity factories.
- Lead: full source/test read, requirements, centralized integration, mutation, gates, evidence and
  publication.

No agent may edit another scope, `CoreRequirements.json`, `.testagent` or evidence. Protected trees
remain outside every operation.

## Acceptance checklist

- Bind exact internal API, generic variance/constraints and Async naming.
- Prove created versus borrowed scope ownership, restoration order, idempotency, concurrent dispose
  and dual-failure precedence.
- Prove provider creation/reuse, activity resolution, exact tokens, cancellation and cleanup after
  every success/failure boundary.
- Prove registration definition caching, action selection, exclusion transitions and endpoint
  configuration identity.
- Prove scoped factory acquisition, pipeline, cleanup, probe and null/failure contracts.
- Finish mutation, coverage, Core/EF gates, manifests, commit, tag and authorized remote publication.
