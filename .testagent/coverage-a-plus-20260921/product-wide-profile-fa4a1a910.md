# T85 complete product-wide measurement — `fa4a1a910`

The strict aggregate `artifacts/t85-aggregate.json` accepted all 33 fresh
receipts from exact source/test commit
`fa4a1a910a69c2fe6b0207aa62baa6d2c097cb61`, without `--partial`.
All 32 product assemblies, every required unit and local integration project,
and the no-AVX2 and scalar-fallback profiles are present. All 13,500 test
executions passed without failures or skips. The source tree is
`08cf8fabfa2528b0f54d74ae3d463937538ad6a8`; the test tree is
`4e441d97f7d0e24c9a421cc055d306bb33d11a00`.

| Measure | T74 frozen commit | T85 frozen commit | Change |
| --- | ---: | ---: | ---: |
| Passed test executions | 13,389 | 13,500 | +111 |
| Covered physical lines | 86,296/93,951 | 86,545/93,965 | +249/+14 |
| Line rate | 91.85214% | 92.10344% | +0.25131 points |
| Conservatively covered branches | 31,191/36,923 | 31,287/36,933 | +96/+10 |
| Conservative branch rate | 84.47580% | 84.71286% | +0.23706 points |
| Method identities | 26,093 | 26,094 | +1 |
| Methods with CRAP > 30 | 0 | 0 | unchanged |

The highest observed CRAP is 30. The conservative branch figure is not an
exact union of branch identities across independent Cobertura reports. Line
and branch A+ work remains open. A direct union of covered physical source
lines from the 33 XML reports leaves 7,420 lines uncovered across 2,776
observed tracked C# source files. High-count gaps include
`StateMachineRequestExtensions` (74/89), `SendCallbackExtensions` (56/60),
and `CachedMessageProducer` (58/81); these counts locate review areas, not
permission to add tests solely for coverage.

Six isolated broker-fixture runs finished with `findings: []`. Their exact
finding-file SHA-256 values are recorded because the 33 receipts do not
cryptographically bind the separate fixture files:

| Fixture | Findings SHA-256 |
| --- | --- |
| Azure Storage Azurite `vicione-4f84b3c9b8d1` | `486f5488939396d69f6d7f6fcdeb8bc0e0f9ecf6543aa274995ca42731e076c8` |
| LocalStack `vicione-16f53d938978` | `998a7a907191b453ae919f480de43e9115797e0278f4be81c0c8089139cdbe29` |
| Azurite and Event Hubs `vicione-80bd3a6e4798` | `c03520285916939703b884c914903f7ec4a959bbb535c20435d10d484359bcc6` |
| PostgreSQL, SQL Server and Service Bus `vicione-2623c3046cab` | `e253f60add974fd394d5aa0d97052f4ccb7a18e4874945747c36f5fe57d30e5f` |
| RabbitMQ `vicione-62fe74b6c231` | `381445e13ac5aef6eb5527c82a9f9fa0fb914dadecf0a4e2bf2cd0f9381aec62` |
| ActiveMQ and Artemis `vicione-e3d649a1dd47` | `d534024a3999f423554ecd82998f8aceff90df8cec338b9daa6b8368e65f525c` |

All ten broker-log hashes in the finding files match their collected log
files. The fixture evidence is internally consistent but is not bound to
the aggregate by the receipt schema. These measurements establish execution
and observed coverage, not the quality of every test or unobserved provider
behavior.

An independent read-only Red Team audit reconstructed the numerical results
from the 33 XML reports and found no discrepancy. It verified 33 exact commit
and source/test tree bindings, 487/487 binary/PDB, log and XML hashes, 66/66
runner and settings hashes, zero-warning builds, every passing MTP summary,
the 32-assembly inventory, and the Top-25 CRAP rows. It confirmed all six
empty fixture-finding files and ten matching broker logs. Its one P2 evidence
finding is the fixture-to-receipt link described above; no other concrete
P1/P2 issue was found.
