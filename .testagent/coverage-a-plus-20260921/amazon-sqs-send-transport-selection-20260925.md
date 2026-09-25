# Amazon SQS send transport selection, 25.09.2026

## Product behavior under test

Commit `e4b0fa57b` checks that Queue addresses request queue send topology
with the exact destination name, while Topic addresses and an absolute address
with `type=topic` do not. A cancelled request must win over unsupported address
validation and must not register any send agents. A created transport owns its
scoped client supervisor and is itself registered by the parent.

## Tests and adversarial review

The focused variants passed 4/4; the complete Amazon SQS unit suite passed
288/288. A read-only Red Team review found that the initial registration-only
assertions could not distinguish queue from topic. A queue-topology spy now
rejects that branch mutation and verifies the exact queue address. The review
also found a remaining P2: replacing `TopicSendTransportContext` with
`QueueSendTransportContext` inside the correct Topic branch would pass these
tests. A provider-boundary test must prove SNS `PublishAsync` versus SQS
`SendMessageAsync`; this slice does not close that gap.
The later `0ced761dc` provider-dispatch slice closed it with a send through
the created transport and a targeted Topic-to-Queue mutation check.

## Exact-commit receipt

`artifacts/coverage-receipt-amazonsqs-e4b0fa57b/receipt.json` verifies 288
tests, 11 unchanged binaries, and 1,423 tracked product sources at commit
`e4b0fa57b`. The partial aggregate is
`artifacts/coverage-sqs-e4b0fa57b.json`.

Within this **SQS unit receipt**, the SQS assembly measures 76.45% line and
79.09% branch coverage, with one method above CRAP 30.
`ConnectionContextSupervisor.CreateSendTransportAsync` rose from 0/18 to
18/18 covered lines and fell from CRAP 42 to 6. The remaining SQS risk member
is the async connection creation path in `ConnectionContextFactory` at CRAP 42.
This is a subset measurement; a fresh complete product-wide profile and the
strict A+ gate remain open.
