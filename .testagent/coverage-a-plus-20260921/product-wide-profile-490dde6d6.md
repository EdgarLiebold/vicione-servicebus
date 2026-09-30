# T157 product-wide ServiceBus profile — `490dde6d6`

The strict aggregate `artifacts/t157-aggregate.json` (SHA-256
`7946c9eb97681510582612d3ad62fc1a05a9b0b38d5e840314faef6e79408658`)
accepted 33 verified receipts without `--partial`. Every receipt records the
same source and test trees at
`490dde6d62edb30f198d89ef8f350cdd43131a02`; all 32 product assemblies,
all Unit and LocalIntegration projects, the no-AVX2 mode and the scalar mode
are present. The selected receipts record 14,138 passed executions with no
failures or skips.

| Measure | T148 | T157 | Change |
| --- | ---: | ---: | ---: |
| Passed test executions | 14,122 | 14,138 | +16 |
| Covered physical lines | 87,549/94,777 | 87,578/94,786 | +29/+9 |
| Line rate | 92.37368% | 92.39550% | +0.02182 points |
| Conservatively covered branches | 31,820/37,382 | 31,823/37,382 | +3/0 |
| Conservative branch rate | 85.12118% | 85.12921% | +0.00803 points |
| Method identities | 26,192 | 26,194 | +2 |
| Methods with CRAP > 30 | 1 | 0 | -1 |

The previous `MessagePackSerializerContext.TryGetCancellation` CRAP hotspot
is closed in this exact-commit aggregate. The highest current CRAP value is
30, reached by eight fully covered methods. Examples include S3
`ValidateBucketName` (22/22 lines), Azure Blob `ResolveBlobClient` (39/39),
DynamoDB `IsValidTableName` (7/7), and RabbitMQ `TryClassify` (19/19). The
highest partly covered method in the risk list is
`MessageActivity.TryStartReceive`, CRAP 28.03, 30/31 lines. Its receive
parent modes and fault isolation received an independent read-only review
without a reproducible P1/P2 defect. No new test was added solely to change
that number. The aggregate observed 2,779 tracked C# source files; this is
not a count of untested methods or files.

The first Core run recorded 7,384/7,385 passes. Its lone failure was
`PropertyProviderOwnershipTests.ObjectProvider_DoesNotRetainCollectibleRuntimeTypes`,
which asserted that a collectible runtime type survived 20 forced GC cycles
while other builds ran. An isolated full Core rerun on the same commit passed
7,385/7,385 and supplies the selected `t157-profile-00-isolated` receipt.
The failing run is retained as diagnostic evidence, not included as a valid
receipt. The test's load sensitivity remains a reliability follow-up; the
isolated pass does not prove why the first run failed.

All twelve local fixture runs ended with empty teardown findings. Their
finding records are outside the receipt hash chain; the SHA-256 values below
bind the observed local runs to this report. Profile 29 used the canonical
controlled ActiveMQ outage and restored its broker.

| Profile | Run suffix | Brokers | Findings SHA-256 |
| ---: | --- | --- | --- |
| 21 | `427b9cfe2e68` | Azurite | `3d780cab5a23b33c474bc251083987f40ab0c9959a5ad4348b57cce874092a34` |
| 22 | `19cd11e227f5` | LocalStack | `d45dfa19c761800ea165cbdce3b655112aa495cbb0c2afae8af562320a7f9474` |
| 23 | `bd37dc7ce979` | LocalStack | `cc5fae30095c2ad32afbdca71316dbd30f437d5cbbfabee0a9db20e924078be5` |
| 24 | `3d39019f15b7` | Azurite | `d537d753a3aa3e91f98ec70e2bc3aac13503e7b5950e56fdcc1b78d4bc09ea81` |
| 25 | `e1c7bbec3c3b` | RabbitMQ | `eb98afc7b2f5af8dd205d3f587420451ec388da88caae7f786ef316effbec56a` |
| 26 | `ca4e75f4818d` | PostgreSQL, SQL Server | `f53435275a6fe8f175e6a92e93576957b31def46b93224db60f6853350b73c78` |
| 27 | `0d6040b8e7a7` | LocalStack | `495f3f361c0b7b717eb55a6a90386b8cae320c8130c0115ce76cc714fa2be95c` |
| 28 | `caddc7493df7` | Azurite, Event Hubs | `31beee74c008407ee20bc935ed418996a4c0971fb5f7d26f8fa670840db8e71d` |
| 29 | `c4f85e3cc322` | ActiveMQ, Artemis | `103992e94b8841f3a53983e6b46c27937233ef8207f928125bf5003c6762f291` |
| 30 | `51ec871bdcf9` | Azure Service Bus | `ec3eec338baef11d634c46754415da3632f1f07a886d538937d6acf5acfba80a` |
| 31 | `ee08e19e56da` | PostgreSQL | `b2300431b5803250e9d461be2d525f3497f537984aa5ccc92b47634d370232e3` |
| 32 | `ef38eae5d147` | SQL Server | `4df4ee31ebcaaf9eb0d6647c7244b487e999b11c7096a284e2c0964c3d285e93` |

The branch figure uses the repository's conservative Cobertura merge: it
does not infer common branch identities across independent reports. The
Microsoft `coverage-analysis` skill's PowerShell scripts could not run here
because `pwsh` is unavailable; the repository's hash-checked Python runner
and strict 33-receipt aggregator produced these method-level CRAP values.
Local emulators cannot prove real cloud IAM, lifecycle deletion, broker
failover, or every production deployment configuration. The full metric gate
is green at this commit; defect review and the cross-repository Roslyn API
review remain separate work.

Independent read-only Red Team review reran the strict aggregate and verified
all 33 receipt report, log and binary hashes, the selected zero-failure and
zero-skip logs, every fixture finding and its broker-log hash, the aggregate
SHA-256 and this report's arithmetic. Its verdict is PASS for the scoped
measurement, with the load-sensitive collectible-type test retained as a P2
reliability follow-up. It found no other concrete P1/P2 evidence defect.
