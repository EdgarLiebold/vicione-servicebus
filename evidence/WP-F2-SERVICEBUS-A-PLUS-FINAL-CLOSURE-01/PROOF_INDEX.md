# Architecture proof-contract index

This slice changes the engineering/runtime acceptance boundary; it does not redefine the listed
Suite feature semantics. Each architecture proof target below points to this slice's executing delta
evidence. The complete pre-existing Durable Sender state-machine, recovery, power-loss classification,
and mutation evidence remains under
`evidence/WP-F2-SERVICEBUS-REVIEW-INTEGRATION-01/V5-DURABLE-SENDER/` and passed again inside the
3,490-case Unit/Architecture run.

| Subjects | Delta proof in this slice |
|---|---|
| `CAP-MSG-022`, `LEG-206`, `LEG-215`, `GUA-MSG-005` | InMemory remains broker-free and completes at the real consumer boundary; provider matrix and complete regression pass. |
| `LEG-208`, `LEG-210`, `GUA-MSG-001`, `GUA-MSG-002`, `GUA-MSG-006`, `MECH-INBOX-001`, `MECH-OUTBOX-001` | Existing bounded Durable/Inbox/Outbox state machines pass complete regression; RabbitMQ adds a real persistent confirmed carrier without fallback semantics. |
| `LEG-211` | Real RabbitMQ persistent mandatory publisher-confirm acceptance and rejection; M01-M03 killed. |
| `LEG-218` | Existing bounded Durable operations/quarantine evidence passes complete regression; no operator contract is weakened. |
| `GUA-GOV-001` | Capability-based exact heritage disposition, current dependency inventory, package/API layering, and nine restored mutations. |
| `GUA-MSG-001`, `MECH-MSG-001`, `MECH-MSG-002` | Complete Unit/Architecture, Shipping/Engineering, real RabbitMQ, and package-only journey gates prove the retained messaging and composition owners. |
| `MECH-TEST-001` | 3,490/3,490 complete native tests, 27/27 real RabbitMQ, assertion/gap review, and nine killed/restored mutations. |

The detailed commands and results are in `VALIDATION.md`, `COMMANDS.md`,
`ASSERTION_AND_GAP_REVIEW.md`, and `MUTATION_VALIDATION.md`.
