# Type relationships and readable-property reflection

## Decision boundary

This cohort preserves the generic-type, interface and readable-property capabilities, not the
historical MassTransit helper API. The old names mixed any-match, first-match and argument lookup;
the product now distinguishes those contracts and rejects ambiguity whenever a caller requires one
match. No compatibility alias remains.

## Complete paths read

- `TypeRelationshipExtensions`, `GenericTypeMatchCache`, `TypeExtensions`, `TypeCache` and
  `TypeNameFormatter`;
- read-only, read-write and write-property caches plus message-property discovery;
- every product call site in registration, dependency injection, initializers, message data,
  System.Text.Json conversion, topology, state machines, activities, futures, filters, dispatch and
  the retained visualizer;
- the four inherited fixtures, their test-local duplicate reflection helpers and all 37 final R0
  obligations.

The test-local helper means several inherited `GetClosingArguments` assertions did not reliably
exercise the product implementation. They remain useful intent evidence but are not an executable
product baseline.

## A+ product contract

- `ImplementsInterface` answers interface assignability, including open generic interface
  definitions.
- `ClosesGenericType` answers only whether at least one fully constructed match exists.
- `GetClosedGenericTypes` returns every fully constructed match in stable identity order through a
  read-only defensive snapshot.
- single-match accessors return false for absence and fail explicitly for ambiguity; argument
  accessors never select the first reflected interface silently.
- the cache is internal, stores non-null result sets and performs reflection once per type/definition
  pair without rooting collectible type metadata.
- readable instance/static property operations select each property by its own getter, walk bases
  before derived declarations, de-duplicate interface diamonds and preserve derived-property-wins
  ordering for the consumers that group by name.
- `TypeCache.GetShortName` formats open generic definitions without attempting to activate an open
  constructed adapter.

## Product defects corrected

1. A readable indexer no longer causes a same-named write-only indexer to be returned as readable.
2. Derived interface properties now follow inherited properties, matching class-hierarchy semantics
   and the existing last-property-wins caches.
3. Open generic names no longer fail inside reflection activation and mask the intended validation
   error.
4. Runtime future registration rejects unrelated types and state machines over a non-`FutureState`
   directly; it no longer indexes an empty argument array or leaks a later reflection failure.
5. The generic relationship, short-name and formatter caches no longer retain collectible `Type`
   instances through process-lifetime strong dictionaries.

## Test defect corrected

The existing request-client race test could cancel the caller before the send was known to have
completed. Its intermittent failure was not a product defect. The test now awaits the real
`RequestHandle.Message` completion before inducing the terminal response/cancellation race; it does
not add a delay, retry or relaxed assertion.

## Deferred connected-product decision

The connected read/write-property consumers expose a separate, larger question: public versus
non-public accessor policy is not expressed consistently, and `ReadWritePropertyCache<T>` may not
make its `includeNonPublic` choice effective. That surface is not silently changed in this cohort.
It is recorded in the repository TODO as a path-complete product-normalization slice covering every
initializer, serializer, metadata and proxy consumer.
