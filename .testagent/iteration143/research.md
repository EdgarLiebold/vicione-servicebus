# Iteration 143 — initializer capability and runtime dispatcher cache research

## Bound source

The packet personally reads every C# file in `src/ViciOne.ServiceBus.Initializers` plus the direct advanced capability contracts and runtime send/publish/response dispatchers in `ViciOne.ServiceBus.Abstractions`.

The eight capability-package files contain thin validation/forwarding APIs and two initialization-context variables. Their current comments match their behavior. Existing owning tests cover overload argument identity, generic response/contract selection, cancellation-token forwarding, capability rejection, all required value/pipe/callback inputs, context-wide variable sharing, cross-context isolation, injected time and caller cancellation.

## Confirmed defect

`SendEndpointDispatcher`, `PublishEndpointDispatcher` and `ResponseEndpointDispatcher` each store every supplied runtime contract `Type` in a process-lifetime `ConcurrentDictionary<Type, Lazy<TConverter>>`. The converter is a closed generic instance that also references the key type. A runtime contract from a collectible assembly therefore remains rooted even after its caller, message and assembly are otherwise unreachable.

These caches require ephemeron ownership: a live runtime type must still resolve one lazily initialized converter, while the key/value graph must be reclaimable when no external owner retains the type. Public signatures, validation order, task identity, assignability checks and generic dispatch must remain unchanged.

The directly analogous risk exists in all three dispatchers and shares one implementation/test contract, so correcting fewer than all three would leave an inconsistent architecture.
