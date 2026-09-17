# Iteration 189 research

## Scope

This packet admits the remaining Mediator public contracts, registration/dependency-injection
surface, scope-preserving facade, endpoint/context implementations and in-process runtime: 22
source files / 2,161 baseline lines. The lead personally read every source file and comment plus 13
direct owning fixtures / 3,252 lines before integration.

Exact path searches against the retained source-admission evidence found none of these 22 paths.
The pre-Iteration-139 connected inventory was confined to Initializers, so all 22 are new unique
admissions rather than re-admissions. Six other Mediator context/registration files already present
in earlier evidence are intentionally excluded.

## Disjoint ownership

- Agent A: configuration contracts/implementation, service-collection registration,
  `ScopedMediator`, and mediator global imports.
- Agent B: addressed/publish/request/send endpoints, client-factory context and bounded body
  serializer.
- Agent C: request extension/contracts/handlers, direct factory and `InProcessMediator` runtime.
- Lead: full source/test read, requirements, centralized integration, mutation, coverage, evidence
  and publication.

No agent may edit another scope, `CoreRequirements.json`, `.testagent` or evidence. Protected trees
remain outside every operation.

## Acceptance checklist

- Bind exact public/internal API shape, generic constraints, annotations and Async naming.
- Prove direct/container configuration, limits, duplicate registration, service lifetimes and
  scoped resolution/forwarding.
- Prove all endpoint/context identities, message/body ownership, metadata, observer ordering,
  address selection, cancellation and failures.
- Prove request/response exception identity, handler behavior, time provider usage and runtime
  cleanup under repeated/concurrent calls.
- Compile and kill isolated mutants, then finish Mediator/Core/EF gates, manifests, evidence,
  commit, tag and authorized remote publication.
