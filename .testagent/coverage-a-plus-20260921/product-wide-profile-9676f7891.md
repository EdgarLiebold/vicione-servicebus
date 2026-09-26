# Product-wide measurement at 9676f7891

Measured commit: `9676f789120dbfe33efb3ec44b43873d7fd41a15`.
Source tree: `6d9e72b9ec42bac96f35f0fed1d684227b23dca0`.
Test tree: `0d1c4d53d9c25e2d11529303fc1ca90741d0c44e`.
This documentation is a successor to the measured commit, not a new measurement.

## Verified result

All 33 established profiles passed at that exact commit: 20 unit/CPU profiles
and 13 local integration profiles. The four canonical fixture groups all exited
zero, including cleanup. The strict aggregate validator accepted all expected
projects, CPU modes and 32 assemblies, with no mixed source/test trees, missing
profiles, failed tests or skipped tests in the accepted receipts. It rechecked
report, log and binary hashes. Read-only adversarial accounting review found no
concrete calculation or completeness blocker.

| Measure | Result |
| --- | --- |
| Test executions, including CPU repetitions | 12,597 passed |
| Lines | 85,091 / 93,762 = 90.7521170623% |
| Conservative branches | 30,718 / 36,841 = 83.3799299693% |
| Method identities, including compiler-generated methods | 26,061 |
| Methods with CRAP strictly above 30 | 0 |
| Methods with line gaps | 4,512 |
| Of those, zero covered lines | 2,802 |
| Of those, partially covered lines | 1,710 |
| Fully line-covered methods with conservative branch gaps | 1,509 |
| Union of line gaps and branch-only review candidates | 6,021 |

Branch observations use the maximum covered/valid counts per identity across
profiles, not a union of individual branch IDs. Method counts do not count
independent product contracts. Complete measurement is not an A+ acceptance.

## Progress and remaining risk

Compared with the prior complete measurement at `2464cdc45`, line coverage
increases by 123 covered lines and conservative branch coverage by 63 branches.
Of the prior line-gap identities, 33 are now fully covered; five identities that
were formerly fully covered now show gaps. The net line-gap reduction is 28.
No product source was changed by these test packets.

The five newly observed gaps require investigation, not automatic dismissal:

- `AmazonSqsMessageReceiver.ConsumeAsync`: line 95, 21/22 lines covered.
- `KillSwitch.RecordSuccess`: line 226, 12/13 lines covered.
- `EntityFrameworkReliableStore.NotFound`: lines 1049–1053, 0/5 covered.
- `ChannelContextFactory.ScopeReleasingActiveContextHandle.FaultedAsync`: line 129, 0/1 covered.
- `CourierActivity.TryStartExecution` callback: lines 21–24, 0/4 covered.

Read-only follow-up review identifies concrete missing deterministic contracts:

1. RabbitMQ first: fail a supervised operation after successful channel acquisition;
   assert the original failure and invalidation of the shared owner before reuse.
   Existing acquisition-failure tests do not guarantee this forwarding path.
2. KillSwitch next: release an already started successful consume after pause or
   terminal stop; state and counters must remain unchanged.
3. EF: repeat an operation after removing its target and require exact NotFound
   reference/null-state data with unaffected neighbors. The existing operator
   race permits InvalidState or NotFound depending on which contender wins.
4. Courier: use a real execution with an ActivityListener and parent activity;
   require tracking/activity/argument tags and trace linkage. Execution alone
   does not require the telemetry callback to run.
5. SQS: return a successful empty in-flight poll after stop is signaled; require
   complete shutdown without another poll or fault. The existing cancellation
   test forces the exception exit instead of this normal loop exit.

These are reviewed test gaps, not proven product defects. Their tests and
mutation checks remain open; RabbitMQ and KillSwitch take priority over the
prepared inline-object packet below.

Highest remaining partially covered CRAP hotspots include DynamoDB repository
configuration validation (29.4286), CronExpression.StoreExpressionValues
(29.1296), CronExpression.ProgressNextFireTimeDay (28.6610), RabbitMQ connection
factory construction (28.2175) and OutboxMessagePipe's async body (28.1745).
Fully line-covered methods can still have high complexity: ValidateBucketName,
ResolveBlobClient and IsValidTableName each retain CRAP30.

The prepared next message-data packet exercises real inline object transport
with and without repository storage, requiring zero fallback reads, plus public
send rejection of unsupported runtime values. Those tests are not implemented.
The full Roslyn API/comment audit remains subsequent to actual coverage/CRAP A+.

## Failed initial configuration and correction

The first orchestration invoked local profiles directly and inherited
`UnitArchitecture`, rather than using the required canonical fixture environment.
Azure Storage, DynamoDB, S3, Azure Table, RabbitMQ and EF local attempts failed
configuration validation. The orchestration was stopped (exit143), its last
child was confirmed terminal, and its manifest/logs were retained. This was a
measurement invocation error, not evidence of a product defect.

The corrected orchestration retained only the 20 valid unit/CPU receipts and
remeasured all 13 local profiles through `tools/ci/run_broker_category.py` with
isolated providers and `LocalIntegration`. No invalid attempt contributes to the
accepted aggregate. Original failures remain in the initial-attempt manifest
and `artifacts/t22-profile-*/` logs; corrected receipts use
`artifacts/t22-local-*-corrected/`. MAIN TestResults/ and review/ were untouched.

## Local evidence fingerprints

These raw outputs remain local ignored artifacts; fingerprints identify the
measured files but do not make those files available from the remote repository.

| Artifact | SHA-256 |
| --- | --- |
| `artifacts/t22-aggregate.json` | `69d0ca66b86efde4ff7b6edcafcd304e3ab65f14af04bd92d8353c978f70efaf` |
| `artifacts/t22-all-methods.json` | `25d7d9e59ac23e2693a45f36fecd8b94bd627c0261cdfa39cdd36fa12a394697` |
| `artifacts/t22-method-gaps.json` | `feefccd6e487aae4c7966c456b8a96f4a037018b94ce113adc5c033f4e78c867` |
| `artifacts/t22-branch-only-gaps.json` | `0796846213f6a70e038e36439a89a16de015205e733915632ab447fd7a5b6be4` |
| `artifacts/t22-profile-progress.json` | `dcbd4674f7cfbba1da4c02c52e5cf3eb8dddf3eb711cd89f1f82a573bf7f13bb` |

Collection scripts: `artifacts/t22-measure-all.py` (initial attempt),
`artifacts/t22-resume-local.py` (corrected local collection).
Analysis: `artifacts/t22-analyze-complete.py`, which invokes the strict tracked
aggregate validator before exporting the method inventory. Existing historical
inventories are preserved; the current inventory is the t22 set above.
