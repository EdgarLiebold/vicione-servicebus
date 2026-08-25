# Request, response, mediator, and multi-bus product-path analysis

## Decision boundary

Backward API compatibility with MassTransit is not a product requirement. This cohort preserves
useful ViciOne.ServiceBus capabilities and may change signatures when that produces a clearer,
safer API. Tests therefore bind observable capabilities and stable error contracts, not historical
method counts, names, parameter order, or overload ownership.

The current typed-message, initialized-values, one/two/three-response, configuration-pipe,
transport, mediator, outbox, scoped-filter, and multi-bus capabilities remain useful. Retaining
those distinct capabilities is intentional; retaining any particular legacy overload is not.

## Complete paths read

- `IRequestClient<TRequest>`, `IClientFactory`, `IScopedClientFactory`, `RequestHandle`,
  `RequestTimeout`, and both response wrapper structs;
- every implementation under `src/ViciOne.ServiceBus/Clients`;
- bus, receive-endpoint, scoped, dependency-injection, and mediator client factories;
- request response/fault endpoint resolution, request filters, in-memory outbox bypass, and request
  expiration propagation;
- mediator dispatch and dependency-injection construction, including `TimeProvider` ownership;
- default and secondary bus registration, scopes, providers, routing, source address, request and
  conversation identity, and header flow;
- all six inherited fixtures and all 40 corresponding R0 ledger rows.

## Product corrections

1. Request deadline and transport TTL are independent. Disabling outgoing TTL no longer disables
   the client deadline.
2. Request deadlines and response/fault TTL calculations use the context-owned `TimeProvider`.
   Dependency-injection mediators resolve the registered standard .NET provider. The
   `ClientFactoryContext` contract requires every implementation to expose its provider explicitly;
   it has no default interface fallback that could silently reintroduce process time.
3. Typed and initialized-value request entry points reject missing messages before any send begins;
   request clients and handles reject missing dependencies at their public boundary.
4. Multi-response wrappers reject missing tasks explicitly instead of leaking a
   `NullReferenceException`, delegate the complete `MessageContext`, and have deterministic
   first-declared priority if more than one branch has already completed.
5. Every request send endpoint bypasses deferred outbox delivery in the shared
   `RequestSendEndpoint<TRequest>` base. The invariant is no longer repeated across four concrete
   endpoint implementations and therefore also applies to future request endpoint implementations.

## Additional source-derived coverage

The inherited suite did not fully cover:

- three-response selection and cancellation over transport and mediator;
- exact cancellation-versus-deadline first-winner semantics under virtual time;
- independence of request deadline and transport TTL;
- dependency-injection `TimeProvider` flow;
- duplicate response-handler rejection;
- all typed and initialized-value public input boundaries;
- response-address, missing-header fallback, ordinal case-insensitive accepted-type matching;
- complete multi-response `MessageContext` delegation and constructor boundaries;
- the outbox-bypass invariant itself. The inherited nested-request fixture created its client from
  the raw bus and therefore remained green even when the concrete endpoint's `SkipOutbox` call was
  removed. A direct transport-backed behavior test now proves the shared invariant, while the
  nested integration test proves the end-to-end request and deferred-side-effect ordering.

Those are native tests in addition to the one-to-one inherited disposition.

## Rejected inherited mechanisms

Forced garbage collection and process-global unobserved-exception events are not retained. They do
not prove ownership of a failure and can pass or fail because of unrelated process state. Native
tests await every public response/message task and assert its exact terminal state and exception.

No sleep, random input, production time, receipt/interceptor, Python policy model, or inherited
TestFramework fixture is part of this cohort.
