# Current review status

This page separates the latest recorded product-wide measurement from older,
chronological test and migration notes. It is a checkpoint, not a claim that
the product is defect-free.

## Product-wide coverage and CRAP checkpoint

The latest complete 33-profile measurement ran against commit
`42a028a7fa8ed6facf941da3d72064cbf438f196` on 30 September 2026. All
receipts name that commit and its `src` tree
`0871a1f1b28cfb4f3303ae8c5fbf7f47010fc6e4`. The measurement covered
all 32 product assemblies and recorded 14,235 passing test executions.

| Measure | Result |
|---|---:|
| Line coverage | 88,032 / 95,297 = 92.3765% |
| Conservative branch coverage | 32,196 / 37,822 = 85.1251% |
| Method identities | 26,317 |
| Methods with CRAP above 30 | 0 |

The [aggregate JSON](quality/t176-aggregate.json) is a byte-for-byte copy of
the local measurement output; SHA-256:
`3b02e44a4229055301a218f2c8e02ce5d7cd90554656db211f1d0fbc64f2a7d2`.
The [33 receipt records](quality/t176-receipts/) preserve their source and
test tree IDs, commands' output hashes, binary hashes, and profile outcomes.
Raw logs, binaries and coverage XML remain local generated artifacts and are
not included in this repository. The recorded result can be independently
checked by rerunning the profiles at the named commit; the compact records
alone do not reproduce the calculation from raw coverage files. Later
documentation and architecture-test changes do not change the measured
product-source tree, but this measurement is not an exact-current-HEAD run.

## Open reliability finding

The first SQL Server profile timed out in
`ParallelPublish_OneThousandMessagesFromTenPublishersArriveExactlyOnceAsync`:
the diagnostic counted 999 distinct arrivals from 1,000 publishes, one
missing application ID, no duplicates, and no remaining delivery in its
normal, error or dead-letter queue snapshot. A fresh-container retry passed
75/75 and is the receipt included in the aggregate. The missing delivery's
cause remains unproven. The successful retry does not close the finding.
The next investigation must distinguish zero routing at publish from loss
before or after the consumer by recording the publish procedure's delivery
count and the subscription, topic and message state for a missing ID.

## API inventory

The [30 September Roslyn inventory](api/roslyn-all-repos-api-2026-09-30.md)
is a dated cross-repository triage snapshot. Its presence-of-XML results do
not establish the semantic quality of every API comment. Nineteen of its
69 projects were flagged by compiler or workspace diagnostics; ServiceBus's
CodeFixes project had a workspace diagnostic. These items remain open for
the respective repository owners before any cross-repository A+ API claim.
