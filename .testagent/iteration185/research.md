# Iteration 185 research

## Scope

This packet admits the connected Courier routing-slip construction and subscription-state core:
three builder/extension files, five routing-slip message-model files and five subscription model /
capture files (13 source files / 1,391 final lines). The lead personally read every source file
and comment before integration.

The directly owning baseline comprises `RoutingSlipBuilderContractTests`,
`RoutingSlipSubscriptionCaptureEndpointTests` and `RoutingSlipRevisionAndSubscriptionTests`
(3 files / 1,053 lines). Existing tests already establish broad builder isolation, flag validation,
wire round-tripping, capture observers and end-to-end revision/subscription behavior. This packet
targets the remaining deep state, overload, malformed-input, ordering, failure and cancellation
gaps without duplicating those proofs.

Exact evidence search found none of the 13 complete source paths in earlier iteration evidence, so
all 13 are newly admitted. Protected trees are outside every search root.

## Disjoint ownership

- Agent A: builder interface/implementation/extensions plus new
  `CourierRoutingSlipBuilderDeepContractTests.cs`.
- Agent B: activity/log/exception/compensation/routing-slip message models plus new
  `CourierRoutingSlipMessageModelDeepContractTests.cs`.
- Agent C: subscription contracts/model/selection/capture endpoint plus new
  `CourierRoutingSlipSubscriptionDeepContractTests.cs`.
- Lead: full source/test read, requirements, centralized build and integration, mutation, coverage,
  evidence and publication.

No agent may edit another scope, `CoreRequirements.json`, `.testagent` or evidence.

## Acceptance checklist

- Bind builder overloads, exact parameter ownership, atomic variable updates, source-itinerary
  transitions and repeated detached builds.
- Bind activity/log/exception/compensation/routing-slip materialization, malformed received state,
  casing/order, element-level null rejection and shallow/deep ownership boundaries.
- Bind subscription flags and snapshots plus capture endpoint overload dispatch, observer ordering,
  cancellation, pipeline/target/observer failures and exact envelope state.
- Compile and kill focused single-cause mutants, then restore every temporary change.
- Finish focused/Courier/Core/EF tests, Release builds, three requirement projections, format,
  manifests, commit, tag, atomic push and independent remote verification without pausing the goal.
