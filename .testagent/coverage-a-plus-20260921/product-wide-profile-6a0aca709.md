# Complete product-wide measurement at 6a0aca709

Measured commit: `6a0aca7096ad93db552fcb31fb099961033001f0`.
Source tree: `6d9e72b9ec42bac96f35f0fed1d684227b23dca0`.
Tests tree: `bd8be79de21fc6ce2893ac7de570bb3ffd3a5781`.
This document is a report successor; its commit is not a new measured commit.

## Validated results

All 33 profiles were freshly measured at the same commit: 20 unit/CPU and 13
local integration profiles. All four canonical fixture groups exited zero,
including cleanup. SQL Server passed 75/75 in 6m16s; its live run was not
restarted. Full SQS passed 300/300, including requirement projections.
The strict aggregate returned `complete`. Read-only adversarial accounting
independently verified 487 file hashes, profile/CPU/assembly completeness and
the gap delta, without an accounting blocker.

| Measure | Result |
| --- | --- |
| Passing test executions, including CPU repetitions | 12,611 |
| Product assemblies | 32 |
| Lines | 85,116 / 93,762 = 90.7787803% |
| Conservative branches | 30,736 / 36,841 = 83.4287886% |
| Method identities, including compiler-generated identities | 26,061 |
| Methods with CRAP strictly above 30 | 0 |
| Line-gap identities | 4,504 |
| Zero / partial line coverage | 2,797 / 1,707 |
| Additional branch-only candidates | 1,510 |
| Union of line-gap and branch-only candidates | 6,014 |

No failed or skipped run contributes. Branch counts use maximum covered/valid
counts per line and identity, not a union of individual branch IDs. These are
method identities, not independent product contracts. A+ remains unproven.

## Target and delta

`Batcher<TEntry>.ApplyResponse`: 20/20 lines, 16/18 conservative branches,
complexity 18, CRAP 18 (previously 14/20 lines, 12/18 branches, CRAP 26.748).
[Tests and deliberate mutations](t31-sqs-batch-followup.md) establish exact
per-caller result mapping and atomic rejection of contradictory response IDs.
The two remaining branches occur at lines 149 and 158, each observed 3/4;
these loops accept optional success/failure collections. All-success/all-failure
responses with an absent opposite collection remain a focused follow-up.

Compared with 3264ed26c, covered lines increase by 15 and conservative branches
by four. Five line-gap identities close, while two formerly complete identities
have gaps. Line-gap count decreases by three, but branch-only candidates increase
by six; the union increases from 6011 to 6014. Do not call this uniform progress
across every method or attribute all measured changes to the SQS additions.

New observations requiring separate evaluation:

- ClientRequestHandle.SendAsync body: 18/19 lines, missing line 155.
- Agent.SetCompleted: 12/14 lines, missing lines 240 and 242.

Of the six T30 observations, EF AwaitConsumerCompletionAsync, EF transaction
RollbackAsync and Future AddSubscription callback are now fully executed. No
new focused tests were added for those paths; this does not strengthen their
behavioral assertions. SagaInstance.MarkInUseAsync remains 13/16 lines; the
EventHub delayed-producer wrapper remains 3/4 and its local async body 0/3.
The causes of these observation differences have not been established.

| Partial-coverage hotspot | Complexity | Covered lines | CRAP |
| --- | ---: | ---: | ---: |
| DynamoDB repository configurator Validate body | 14 | 8/14 | 29.428571 |
| CronExpression.StoreExpressionValues | 26 | 15/18 | 29.129630 |
| CronExpression.ProgressNextFireTimeDay | 26 | 16/19 | 28.661029 |
| RabbitMqAddressExtensions.GetConnectionFactory | 28 | 43/46 | 28.217473 |
| OutboxMessagePipe.DeliverOutboxMessagesAsync body | 28 | 31/33 | 28.174528 |

Next work includes optional SQS response lists and deterministic EF missing-row
versus stale-lease transitions. The EF plan has read-only adversarial review;
it requires exact persisted-state and capacity assertions despite the incidental
line closure in this measurement. The broader risk inventory remains open.
The complete Roslyn API/comment audit follows actual A+ coverage completion.

## Local evidence fingerprints

Raw artifacts are ignored local files; these fingerprints do not publish them.

| artifacts/ file | SHA-256 |
| --- | --- |
| t31-aggregate.json | da5f691cb3c816dc5a1bd205157c250a9e42bcd6dfd5fb6ddab621e1b5da3a74 |
| t31-all-methods.json | ea95caa51f60ef6652f9e5aacb54af3bdb31d6f8fe928a33f8806f7b01b82a68 |
| t31-method-gaps.json | ad378b7088904dd451a127cddd9abd9d1d7032fa879d0322db6d0aca8e4e0338 |
| t31-branch-only-gaps.json | 13f9ecc5500be516a0b534e4e56d620af2e93664648ca28b84ec531430e16f08 |
| t31-profile-progress.json | 590d32e092a03995b002c9e6593842743f35f7bd2a9f1c1aeaca9951554197e7 |
| t31-gap-delta.json | 712f671a0348534929778f268fe1a459467a740ae137e131c4e15c074d1359f7 |

Orchestration: artifacts/t31-measure-all.py. Analysis:
artifacts/t31-analyze-complete.py, then artifacts/t31-compare-gaps.py.
All measurement and analysis processes are terminal.
