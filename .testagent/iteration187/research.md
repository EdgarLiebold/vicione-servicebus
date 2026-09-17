# Iteration 187 research

## Scope

This packet admits the connected Courier activity/routing-slip contracts, executor and global
correlation/JSON registration surface: 13 source files / 356 final lines. The lead personally
read every source file and comment before integration.

The directly owning baseline includes `ActivityDefinitionContractTests`,
`RoutingSlipExecutorContractTests` and `JsonMessageTypeMappingRegistryTests` (3 files / 504 lines),
plus previously admitted builder/subscription integration evidence. Existing coverage binds core
happy paths; this packet targets exact contract shape, failure/cancellation boundaries, executor
collaborator transitions and complete capability registration.

Exact evidence search found none of the 13 complete source paths in prior iteration evidence, so
all 13 are newly admitted. Protected trees are outside every search root.

## Disjoint ownership

- Agent A: activity interfaces and routing-slip transport contracts plus new
  `CourierActivityAndRoutingSlipContractDeepTests.cs`.
- Agent B: executor, builder contract and subscription target plus new
  `CourierRoutingSlipExecutorDeepContractTests.cs`.
- Agent C: correlation conventions and Courier JSON mappings plus new
  `CourierConventionSerializationDeepContractTests.cs`.
- Lead: full source/test read, requirements, centralized integration, mutation, coverage, evidence
  and publication.

No agent may edit another scope, `CoreRequirements.json`, `.testagent` or evidence.

## Acceptance checklist

- Bind public/internal API shape, generic variance/constraints, activity annotations and Async
  naming.
- Bind executor validation, snapshot ownership, exact endpoint selection, timestamps, tokens,
  cancellation checkpoints and authoritative collaborator failures.
- Bind all Courier correlation selectors and all JSON contract-to-materializer mappings,
  including idempotent concurrent registration.
- Compile and kill focused isolated mutants, restoring every temporary change.
- Finish focused/Courier/Core/EF tests, Release builds, projections, format, manifests, commit,
  annotated tag, atomic push and independent remote verification without pausing the goal.
