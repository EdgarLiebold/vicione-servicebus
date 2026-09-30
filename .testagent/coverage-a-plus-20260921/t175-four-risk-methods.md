# T175 send/admission/journal risk-method packet

## Starting evidence

The exact T174 aggregate at `7664edbb0` covered 32 product assemblies with
14,228 passing executions. Four methods had CRAP above 30:
`MessageSendContext.get_Body` (183.612), the physical-admission closure
(61.5625), `MessageJournalCaptureFactory.CreateSend` (56.25), and
`MetadataSnapshot.ChangedIdentityField` (36).

## Changes and behavioral checks

- `MessageSendContext.Body` now calls explicit pre/post metadata checks,
  contract-type binding, and failure restoration. Four new tests cover later
  correlation, native-route and contract drift from a cached body plus a
  serializer that changes all three before throwing. They assert restored
  state, a stable rejected failure, mutation classification, and preservation
  of the original serializer exception.
- Physical admission delegates proof completeness and identity binding to
  separate operations while preserving their order under the shared body
  metadata guard.
- `MessageJournalCaptureFactory.CreateSend` now separates pre-capture
  validation, body read, and header snapshot. Three new tests assert that
  correlation, contract, and native-route drift before capture is rejected
  and restored.
- `MetadataSnapshot.ChangedIdentityField` delegates to message and causal
  identity checks, preserving the original field priority.

All four body tests passed against the pre-refactor product code and after
the refactor. All three journal tests passed before and after the refactor.
The complete Core test project passed 7,446/7,446 with coverage collection.
In this targeted Core report, the former four risk methods now measure:
`get_Body` CRAP 16 (24/24 lines), `AdmitCore` CRAP 14 (31/31),
`CreateSend` CRAP 14 (24/24), and the two identity-check methods CRAP 18
each (4/4). The old admission closure is gone. These are Core-only figures;
native update methods executed by provider tests cannot be judged from this
single report. A new exact-commit 33-profile aggregate is still required.
Read-only adversarial review found no concrete P1/P2 semantic regression in
the extracted checks or restoration order; the reviewer did not run tests.
