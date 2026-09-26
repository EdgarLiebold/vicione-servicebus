# Complete product-wide measurement at 90239365c

Measured commit: `90239365cd318da3baafbc7de3bec0c1e83cfaf7`.
Source tree: `6d9e72b9ec42bac96f35f0fed1d684227b23dca0`.
Tests tree: `a33f32fa2ebc924f925de037ea30ff27b3d49264`.
This report is a documentation successor, not a new measured source/test commit.

## Validated results

All 33 expected profiles passed at one commit: 20 unit/CPU profiles and 13 local
integration profiles. All four canonical fixture groups exited zero including
cleanup. The strict aggregate validator rechecked hashes, assembly/project/CPU
coverage and common source/test trees and returned `complete`. No failed or
skipped test run contributes to this aggregate. SQL Server completed 75/75 in
5m40s; its observation waits did not trigger a restart.

Read-only adversarial accounting review independently verified all 487 receipt
file hashes and the ten-closed/four-new gap delta, with no concrete blocker.

| Measure | Result |
| --- | --- |
| Passing test executions, including CPU repetitions | 12,604 |
| Product assemblies | 32 |
| Lines | 85,107 / 93,762 = 90.7691815% |
| Conservative branches | 30,725 / 36,841 = 83.3989305% |
| Method identities, including compiler-generated identities | 26,061 |
| Methods with CRAP strictly above 30 | 0 |
| Line-gap identities | 4,506 |
| Zero / partial line coverage | 2,800 / 1,706 |
| Additional branch-only candidates | 1,509 |
| Union of line-gap and branch-only candidates | 6,015 |

Branch counts use maximum covered/valid counts per line and identity across
profiles, not the union of individual branch IDs. Method identities are not
independent product contracts. These results do not establish A+ completion.

## Delta and remaining work

Compared with 9676f7891, covered lines increase by 16 and conservative covered
branches by 7. Ten previous line-gap identities are now complete and four
formerly complete identities now show gaps, for a net reduction of six.
Production source is unchanged between these measurements.

All five deliberately addressed line gaps are closed: RabbitMQ borrowed
FaultedAsync (1/1), KillSwitch.RecordSuccess (13/13), EF NotFound (5/5), Courier
execution tag callback (4/4), and SQS ConsumeAsync body (22/22). See
[behavioral evidence and mutations](five-gap-followup.md). Some other closures
occur outside the new tests' target scope; do not attribute every delta to them.

The four newly observed gaps remain explicit review candidates:

- EventHubs `PendingConfirmationCollection.Canceled`, line 71, 0/3.
- EventHubs `PendingConfirmationCollection.Cancel`, line 77, 2/3.
- EventHubs `PendingConfirmation.Canceled`, line 60, 0/2.
- Core `ClientRequestHandle<TRequest>.Cancel`, line 189, 9/10.

These are missing execution evidence in this run, not proven product defects.
Investigate deterministic cancellation scenarios before dismissing them as
timing variation. The inline-object MessageData packet is prepared but unwritten.
Further read-only risk triage identifies payload admission for unadmitted custom
serializer bodies, mixed/contradictory SQS batch result mapping, and the contract
for secondary EventHub fault-observer errors as subsequent candidates.

| Partial-coverage hotspot | Complexity | Covered lines | CRAP |
| --- | ---: | ---: | ---: |
| DynamoDB repository configurator Validate body | 14 | 8/14 | 29.428571 |
| CronExpression.StoreExpressionValues | 26 | 15/18 | 29.129630 |
| CronExpression.ProgressNextFireTimeDay | 26 | 16/19 | 28.661029 |
| RabbitMqAddressExtensions.GetConnectionFactory | 28 | 43/46 | 28.217473 |
| OutboxMessagePipe.DeliverOutboxMessagesAsync body | 28 | 31/33 | 28.174528 |

Fully line-covered methods still include complexity/CRAP 30, for example
ValidateBucketName, ResolveBlobClient and IsValidTableName. CRAP <=30 is not A+.
The complete Roslyn API/comment audit remains subsequent to actual A+ coverage.

## Local evidence fingerprints

Raw artifacts remain ignored local files; these hashes do not publish them.

| artifacts/ file | SHA-256 |
| --- | --- |
| t29-aggregate.json | 67bb05d61a2f1d85601710273707adedd322f18d6f2767189047853a753d2577 |
| t29-all-methods.json | b97830f57186e21dfe0064e97005381406cedcd0efc6beb0ed77f0fc1f790aec |
| t29-method-gaps.json | 3e8c7ce85af55936cee3f1bf1245036544c87130bdf2faa5a68649c9b0836437 |
| t29-branch-only-gaps.json | bd12b5138880f160463679b5c1a58b77b366142aa676a8a8ae9b7bc5001834fe |
| t29-profile-progress.json | 8671ed15cea2d43f320a25f71626474158d48831dc9888b6c0cef0209c86b924 |
| t29-gap-delta.json | 4fe7326131603c0540649836edfcd399c2243f3306992185e1137e42e9c5f628 |

Orchestration: artifacts/t29-measure-all.py. Analysis:
artifacts/t29-analyze-complete.py, then artifacts/t29-compare-gaps.py.
The complete frozen measurement has finished; no measurement remains live.
