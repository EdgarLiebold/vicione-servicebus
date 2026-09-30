# T168 outgoing journal metadata consistency

Scope: outgoing journal capture after a successful or faulted send. The user's
accepted product-wide coverage and CRAP baseline remains
`product-wide-profile-096e8ecfa.md`; this correctness packet does not rerun
the 33-profile measurement.

## Reproduced failures and decision

`SuccessfulSend_JournalDoesNotRecordContentTypeChangedAfterDeliveryAsync`
failed red-first because the second body materialization changed the send
context from the delivered `ContentType` to a new type. The delivered message
had the original type, but the journal used the new type. The corrected path
restores delivered metadata and drops the capture without changing delivery.

The read-only Red Team found the same inconsistency in fault captures. Four
red-first variants of
`FaultedSend_BodyMutationCannotWriteContradictoryEntryOrLeakContextAsync`
changed either `MessageId` or `ContentType` after preparing body bytes, then
returned or threw. Before the fix, the context retained the changed metadata;
the returning variants could write a contradictory fault entry. The guard now
restores the context and drops the entry while preserving the original send
fault. `SuccessfulSend_ThrowingJournalBodyRestoresDeliveredContentTypeAsync`
checks the corresponding post-delivery throwing path.

Any ID or content-type mutation during a fault capture is now rejected,
including a lazy serializer that changes metadata before creating bytes.
Generic message-body bytes do not expose a reliable way to distinguish that
case from a mutation after byte creation. The prior regression test is renamed
`FaultedSend_JournalRejectsLazyIdentityChangeDuringCaptureAsync` and verifies
that no contradictory entry is written and the original send fault survives.
Normal fault captures with stable metadata remain covered by
`OutgoingJournal_RecordsTheExactSendAndPublishFaultOutcomesAsync`.

## Verification and review

- Journal integration class: 19/19 passed after the first correction.
- Complete Core project: 7,411/7,411 passed, 0 failed, 0 skipped.
- Red Team discovered the faulted-send P2; final read-only verdict: PASS, no
  further concrete reproducible P1/P2 in this diff. It confirmed the
  conservative lazy-fault capture tradeoff is observable but justified by
  the journal's best-effort contract and the generic-body ambiguity.
