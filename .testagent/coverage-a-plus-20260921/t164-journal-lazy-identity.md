# T164 journal capture of a lazy faulted send

## Product defect

`MessageJournalCaptureFactory.CreateSend` snapshot send metadata before
materializing `MessageBody.ToArray()`. A serializer may return a lazy body and
change `SendContext.MessageId` during materialization. A send that faults in
`PreSendAsync` reaches `SendFaultAsync` before transport serialization, so the
journal can persist metadata with ID A and a serialized body containing ID B.

## Red-first behavioral evidence

`MessageJournalIntegrationTests.FaultedSend_JournalMetadataMatchesDeferredSerializedBodyAsync`
connects the real outgoing journal to a bus, injects a failing PreSend observer,
and uses a deferred serializer that changes the ID only when the body is read.
Before the fix, the journal entry's metadata retained A while its JSON body
contained B. The test also requires the original send exception to remain
visible to the caller and exactly one materialization.

## Correction and verification

The capture factory now materializes the body before reading metadata, headers,
content type or message types. This keeps the journal entry internally
consistent without changing the journal writer's failure isolation.

- Focused integration regression: 1/1 green.
- Complete Core suite: 7,394/7,394 green.
- Read-only adversarial review: no concrete P1/P2. The publish fault observer
  uses the same capture factory; a separate lazy publish test was not required
  to distinguish this shared implementation defect.
- The previous exact-commit 33-profile Line, Branch and CRAP baseline remains
  `product-wide-profile-096e8ecfa.md`. No new product-wide measurement was
  taken for this bounded product-defect iteration.
