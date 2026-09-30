# T167 durable sender content type consistency

Scope: the typed Durable Sender's transition from mutable send context to
persistent serialized intent. This packet follows T166's transport-body guard.
The user's accepted product-wide coverage and CRAP baseline remains
`product-wide-profile-096e8ecfa.md`; this correctness packet did not rerun
the 33-profile measurement.

## Reproduced failure and fix

`SerializerChangingContentType_RejectsBeforeDurableAdmissionAsync` has two
red-first variants. A custom serializer changes the content type either when
the lazy body is created or when its bytes are read. Before the fix, both
variants reached `IDurableSendAdmission` with body bytes produced under
`application/json` and metadata reporting `application/octet-stream`.

The Durable Sender now captures the content type before materializing the
body. It restores the original type and rejects a changed type before payload
proof or durable admission. `ThrowingSerializerChangingContentType_PreservesFaultAndRestoresContextAsync`
checks both throw phases: the exact original exception is propagated, the
content type is restored, and durable admission is never invoked. The
existing `SerializerChangingMessageId_RejectsBeforeDurableAdmissionAsync`
variants remain green.

## Verification and review

- Complete `TypedDurableSenderConfigurationTests` class: 8/8 passed.
- Complete Core project: 7,405/7,405 passed, 0 failed, 0 skipped.
- Read-only adversarial review: PASS; no concrete reachable P1/P2 in the
  product change. Its suggested throwing-serializer regression is included
  in the final test class.
