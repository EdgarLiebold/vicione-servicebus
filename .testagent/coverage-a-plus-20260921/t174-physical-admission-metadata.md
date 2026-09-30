# T174 physical payload-admission metadata boundary

## Confirmed defect

The shared `PayloadAdmissionTransportBoundary.Admit` remembered only
`MessageId`. A serializer `ContentType` getter could change the selected
`DestinationAddress` before the body was created, and the physical boundary
still admitted the send. The new test failed against the old implementation:
no exception was thrown.

## Correction and evidence

- The boundary now captures expected send metadata before any serializer or
  body callback and guards body length, proof validation, and identity binding
  under the shared pre- and postflight check. It restores mutated context
  metadata on failure.
- The focused boundary class passes 6/6, including the red-first destination
  regression. The complete Core Unit project passes 7,439/7,439.
- A source-structure architecture test was updated to require the new guarded
  body read and expected metadata capture. Its previous literal-body-access
  assertion no longer represented the product contract.
- Read-only adversarial review found no concrete P1/P2 in the change, including
  cached-body, proxy, and provider-native metadata paths.

The exact-commit full Unit and product-wide coverage/CRAP gates must be
recorded separately after the change is committed.
