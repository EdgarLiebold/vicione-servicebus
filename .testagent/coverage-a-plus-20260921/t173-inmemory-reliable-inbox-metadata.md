# T173 in-memory reliable inbox metadata admission

## Defect and correction

- `InMemoryReliableInboxContext.AddSendAsync` selected `DestinationAddress`
  before payload admission. A serializer `ContentType` getter could change the
  context destination during admission. The inbox then staged a record with
  the old destination and an envelope made from the new context state.
- Body materialization also lacked a common check through payload proof and
  replay metadata capture. A header getter could change `CorrelationId` after
  the body was made, leaving the staged record inconsistent with its envelope.
- The inbox now captures the expected send metadata before admission and keeps
  the shared transport guard active through body bytes, proof, replay metadata,
  and record validation. A rejected mutation restores the context before any
  message is added to the inbox outbox.

## Test evidence

- The destination regression failed on the prior product code: no exception
  was thrown. It passes after the correction and checks restored destination
  and an empty committed inbox outbox.
- The delayed header regression mutates only on the third read after body
  materialization. It checks rejection, restored correlation identity, and an
  empty committed inbox outbox.
- Focused class: 2/2 passed. Complete Core Unit project: 7,438/7,438 passed,
  zero failures or skips.
- Read-only adversarial review: PASS, no concrete P1/P2 finding in this change.

The exact product-wide coverage and CRAP profile has not been rerun for this
batch. The last full baseline remains T171 at `15411740a`.
