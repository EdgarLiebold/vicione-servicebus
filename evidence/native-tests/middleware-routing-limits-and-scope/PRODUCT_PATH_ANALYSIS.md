# Middleware routing, limits and scope

## Decision boundary

The five inherited fixtures are migration evidence for eight behavior identities. The connected
product paths were read through `ScopePipeContext`, payload caches, dispatch specifications,
dynamic/type/key routing, output and tee filters, concurrency adjustment and the complete circuit
state machine. Useful capabilities are retained; fixture shape, wall-clock timing and historical API
accidents are not compatibility requirements.

## A+ product contract

- a scope reads its nearest inherited payload and cancellation token, while local adds and updates
  never write back to any parent;
- dispatch invokes every compatible connected route and then the original input continuation
  exactly once; a nonmatching route does not suppress that continuation;
- a dynamic router invokes all and only type-compatible routes, and a disconnected route stops
  receiving immediately;
- keyed routing requires both a compatible context type and the exact connected key; duplicate and
  null keys fail at the routing boundary;
- converter factories and converters are explicit collaborators: missing factories/accessors,
  missing converter instances and a successful conversion with a null output fail immediately with
  stable diagnostics;
- a connected output context must implement the input context required by the continuation; an
  incompatible generic type is rejected before reflective filter construction;
- a concurrency limit admits exactly its configured maximum, queues the remainder and applies
  runtime changes without leaking permits;
- the default circuit breaker admits six consecutive failing attempts before opening, publishes one
  event carrying the exact causal exception and owns one active timer.

## Product defects corrected

1. Both `ScopePipeContext` constructors now reject a null parent at construction rather than failing
   during a later payload or cancellation access.
2. Dispatch and dynamic-router construction reject missing converter factories and key accessors at
   their public boundaries.
3. A converter factory returning null is rejected when the route is connected; it cannot survive as
   a latent null filter dependency.
4. A converter claiming success with a null output is rejected before observers or route pipes see
   an invalid context.
5. Key routing rejects a null configured key and a null key returned at runtime instead of leaking a
   `ConcurrentDictionary` implementation exception.
6. An incompatible dynamic output type now produces a stable context-contract error instead of a
   late reflection/generic-constraint diagnostic.

## Preserved intentional behavior

Dynamic dispatch is fan-out by compatible context type, not first-match routing. With multiple
output types, every compatible route runs and the original continuation runs once after all route
tasks complete. The circuit breaker's active threshold remains an activation boundary (`attempts >
ActiveThreshold`), matching the original author's explicit implementation and the historic six-send
default behavior.
