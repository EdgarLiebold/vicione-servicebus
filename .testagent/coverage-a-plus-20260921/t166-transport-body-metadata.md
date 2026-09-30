# T166 transport body metadata consistency

Scope: physical send boundaries in InMemory, RabbitMQ, Azure Service Bus,
ActiveMQ, Event Hubs, Amazon SQS, SQL Server, PostgreSQL, and outgoing fault
journal capture. The user accepted the current Line and Branch coverage levels
as A+ and directed the remaining work toward real product bugs. The existing
33-profile coverage and CRAP measurement covers commit `096e8ecfa`, not T166;
no new product-wide profile was run for this correctness packet.

## Reproduced failures

| Test | Red-first result | Corrected behavior |
| --- | --- | --- |
| `SendAsync_RejectsBodyCallbackThatChangesPublishedMetadataAsync` | RabbitMQ published body A with identity or content type B | Send rejects before channel publish and restores both metadata fields |
| `Create_RejectsBodyCallbacksThatChangeSqlMessageMetadata` | SQL storage returned body A after identity or content type changed | Text and binary storage reject the mutation before database parameters are created |
| `ThrowingBodyMutation_PreservesOriginalSendFaultWithoutContradictoryJournalAsync` | Throwing serializer left identity B after failed send | Original exception is preserved, identity A restored, no false journal entry |
| `FaultedSend_UntrustedExceptionDataCannotSuppressJournalAsync` | A caller-controlled `Exception.Data` string suppressed a normal fault entry | A private object marker distinguishes transport mutation from an ordinary send fault |

`RejectedSend_JournalDoesNotRecordMutatedFaultMetadataAsync` also checks the
real bus path: a body that mutates on every read is materialized once, rejected
before delivery, and not read again by the fault journal. The SQL test checks
both identity and content type for text and binary storage.

## Verification and review

- Focused RabbitMQ send-context class: 46/46 passed.
- Focused SQL storage class: 10/10 passed.
- Focused SQL architecture rules: 2/2 passed.
- Focused Core message-journal integration class: 13/13 passed.
- Read-only adversarial review: PASS; no further concrete reproducible P1/P2
  in the current diff after closure of the spoofable exception-marker gap.
- Release build of the complete Unit solution: success, 0 warnings and 0 errors.
- Complete Unit solution: 12,268/12,269 passed. The sole failure was
  `CancelJob_CancelsTheRunningConsumerAndPublishesTheReasonAsync`, which timed
  out after 33 seconds during parallel project execution. The same test passed
  in an isolated rerun, followed by the complete Quartz project at 318/318.
  This is a parallel-load sensitivity, so the Solution run is not reported as
  fully green. Every other project in the Solution passed.
