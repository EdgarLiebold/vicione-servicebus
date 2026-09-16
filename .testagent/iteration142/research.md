# Iteration 142 — initializer cache ownership and immutable publication research

## Bound scope

The whole-fork A+ objective remains active. This connected packet completes a personal source/comment read of all 87 current C# files / 7,732 lines under `src/ViciOne.ServiceBus/Initializers` and corrects the cache/publication findings that share the subsystem's runtime-type ownership chain.

`InitializerConventionRegistry.Conventions` is documented and enforced as frozen after first read, but it publishes its backing snapshot as an array. Consumers can cast the returned `IReadOnlyList<IInitializerConvention>` to `IList<IInitializerConvention>` and replace elements, changing the conventions observed by message-initializer construction after the registry claims immutability.

`ConventionTypeCache` holds every closed contract type in a `ConcurrentDictionary<Type, ...>`. `MessageInitializerCache<TMessage>` likewise holds every runtime input type in a process-lifetime dictionary. Their values may legitimately reference the key type through closed generic adapters; an ephemeron cache is therefore required so the key/value graph can be collected when the application no longer owns the runtime type.

`TypeConverterCache` has two independent strong roots for converters synthesized from arbitrary runtime types: a global `ConcurrentDictionary<Type, object>` and a global `List<object>`. Dynamic enum and named-value converter instances close over the runtime type and are added to both. Built-in converter instances are stable process-wide values, but dynamically closed converter graphs must be weak-keyed and must not enter the built-in list.

The remaining `WaitAsync(cancellationToken)` sites in this subsystem are deliberate caller-owned task boundaries: task-valued source/fallback input, task-valued copy input, async-provider inner value and message-data value. Accepted outer provider/converter/variable/initializer tasks are already observed directly. Public TaskInitializerExtensions documentation and existing cancellation tests explicitly require local wait cancellation, so this packet does not change that contract.

## Existing evidence and test gaps

Existing tests prove registry add/freeze sequencing, stable cache identity, converter success matrices and one runtime-converter cache's collectibility. They do not attempt element replacement through the published convention collection or keep each of the three target caches alive while releasing a collectible runtime type/assembly.

The correction must preserve exact cache identity for live keys, single published snapshot identity, unsupported-conversion false results, built-in converter reuse, exception caching through Lazy initializer values and every public API signature. No dependency, SDK, target framework or supported initializer behavior needs to change.
