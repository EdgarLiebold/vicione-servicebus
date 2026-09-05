# Work package D verified counterexamples

This package records explicit counterexamples for the high-risk boundaries. It does not claim a
repository-wide mutation score or independent certification.

## Composition mutations

The startup matrix rejects and aggregates missing transport, missing limits, feature ownership for an
unregistered bus, duplicate feature ownership, incomplete reliable-messaging ownership, invalid
contract-catalog materialization, ambiguous serializers, endpoint-QoS conflicts, and incomplete or
duplicate journal-provider selection. Tests assert all expected lines rather than only the exception
type, so returning early or dropping one owner is detected.

The journal integration case sends and consumes through the bus-block registration and proves that
only the owning bus receives the configured journal. EF Core and Azure Table selectors are separately
materialized without external I/O; removing either provider registration breaks its provider contract
test.

## Limit mutations

The shared runtime boundary accepts exactly `MaxEnvelopeBytes` and rejects maximum plus one with exact
actual size, maximum size, and input address. Nine source-bound architecture cases prove every named
transport path reads through that guarded boundary before deserialization. Reordering body admission
after deserialization, bypassing the body property in any provider, or omitting either SQL provider
breaks an executing gate.

JSON tests distinguish depth at the inclusive boundary from boundary plus one and cover raw JSON plus
`TryGetMessage`; replacing the configured depth with the serializer default is therefore observable.

## AES-GCM mutations

- Flipping a byte in magic, version, key identifier, nonce, ciphertext, or tag fails closed.
- Reusing a key identifier with different material fails authentication.
- Rotating the current key changes the envelope key identifier while historical material remains
  decryptable; removing that key fails before plaintext exposure.
- 10,000 encrypted objects produce 10,000 distinct 96-bit nonces.
- Unsupported AES material sizes and mutable caller-owned key buffers are rejected or isolated.
- Maximum plaintext and encrypted-object bounds are checked independently.

These cases kill removal of associated data, tag verification, random nonce generation, key lookup by
envelope identifier, defensive copying, or either size boundary.

## Configuration-message mutations

The semantic normalization tool resolves the exact `ConfigurationException` type and true subtypes,
maps the actual constructor message parameter, and recognizes only the central factory or the complete
one-line format. Its accepted report is idempotent at 276/276. The architecture test walks the same
inheritance closure independently; inserting a raw message, a newline, or an unresolved exception
creation breaks the gate.
