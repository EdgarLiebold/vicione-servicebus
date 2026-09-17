# Iteration 186 research

## Scope

This packet admits the connected Courier activity-factory/consumer registration, request/response
proxy and lifecycle event-publication core: 28 source files / 2,208 final lines. The twenty-eighth
source, `ConsumeSendPipeAdapter.cs`, was admitted when the response tests causally exposed typed
send metadata loss during central integration. The lead
personally read every source file and comment before integration.

The directly owning baseline comprises `CourierActivityFactoryContractTests`,
`CourierRegistrationBoundaryTests`, `RoutingSlipEventPublisherContractTests`,
`RoutingSlipRequestIntegrationTests` and `RoutingSlipRequestProxyContractTests` (5 files / 2,283
lines). Existing coverage is substantial; this packet targets remaining lifetime, reflection,
registration, malformed metadata, retry/response, serializer-envelope and multi-destination
failure/cancellation gaps without duplicating end-to-end proofs.

Exact evidence search found none of the original 27 complete source paths in prior iteration
evidence. The subsequently admitted adapter was likewise absent from the evidence search. All 28
are newly admitted. Protected trees are outside every search root.

## Disjoint ownership

- Agent A: consumer kinds, Courier service registration and delegate/default factories plus new
  `CourierActivityFactoryRegistrationDeepContractTests.cs`.
- Agent B: request metadata, request proxy, variable names and response proxies plus new
  `CourierRoutingSlipRequestResponseDeepContractTests.cs`.
- Agent C: lifecycle event contracts, accessor extensions and event publisher plus new
  `CourierRoutingSlipEventPublisherDeepContractTests.cs`.
- Lead: full source/test read, requirements, centralized integration, mutation, coverage, evidence
  and publication.

No agent may edit another scope, `CoreRequirements.json`, `.testagent` or evidence.

## Acceptance checklist

- Bind factory creation, context binding, sync/async cleanup, cancellation, probe and consumer-kind
  registration/dispatcher boundaries.
- Bind request metadata, proxy preconditions, retry state, response/fault endpoints, null tasks,
  exact tokens and authoritative failure propagation.
- Bind all lifecycle contract/accessor shapes, content selection, activity filters, custom envelope
  serialization, supplemental topology routing, delivery order and multi-target failures.
- Compile and kill focused isolated mutants, restoring every temporary change.
- Finish focused/Courier/Core/EF tests, Release builds, three projections, format, manifests,
  commit, tag, atomic push and independent remote verification without pausing the goal.
