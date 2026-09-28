# T74 complete product-wide measurement — `15afb3423`

The strict aggregate `artifacts/t74-aggregate.json` accepted all 33 fresh
receipts from exact source/test commit
`15afb3423b798435d60488afb8460704deb0907c`, without `--partial`.
All 32 product assemblies, every required unit and local integration project,
and the no-AVX2 and scalar-fallback profiles are present. All 13,389 test
executions passed without failures or skips. The source tree is
`68ffa9d83247dc15268bfbe4147fd8dffca2b96c`; the test tree is
`45e03fd40a26701d07c64e071a6cfefa119458ab`.

| Measure | T63 frozen commit | T74 frozen commit | Change |
| --- | ---: | ---: | ---: |
| Passed tests | 13,318 | 13,389 | +71 |
| Covered physical lines | 86,113/93,862 | 86,296/93,951 | +183/+89 |
| Line rate | 91.74426% | 91.85214% | +0.10787 points |
| Conservatively covered branches | 31,110/36,869 | 31,191/36,923 | +81/+54 |
| Conservative branch rate | 84.37983% | 84.47580% | +0.09597 points |
| Method identities | 26,084 | 26,093 | +9 |
| Methods with CRAP > 30 | 1 | 0 | −1 |

The previous `EventHubProducer.BatchSendPipe.SendAsync` CRAP regression is
absent from this complete exact-commit report after its behavior-preserving
extraction. The highest observed CRAP is 30. The conservative branch figure
cannot be treated as an exact union of branch identities across independent
Cobertura reports. Line and branch A+ work remains open.

Four isolated broker-fixture groups finished with `findings: []`. Their exact
finding-file SHA-256 values are recorded here because the 33 receipts do not
cryptographically bind the separate fixture files:

| Fixture | Findings SHA-256 |
| --- | --- |
| Shared six-broker `vicione-4270a27fe7ab` | `9d76070adbb0c260ecf051fa0c705273d379ebca5ff87f098587d8a0509b1531` |
| RabbitMQ `vicione-6443f126f138` | `e976a2814807eac3468534913e2eaee1331ae8cd35ba1158393ad7b6818530de` |
| Service Bus `vicione-425d62cd4fa9` | `0b587c127302d8faebc0cb194854b6100a47cfd5164411eb8789990668a91183` |
| SQL Server `vicione-a8107d2091c6` | `925b487db3135c7ead86325d092629189e04baf580755cad1da1e8859b993124` |

An independent read-only audit reconstructed all figures from the 33 XML
reports without discrepancy. It verified 33/33 commit and source/test tree
bindings, all 487 binary/PDB, log and XML artifact hashes, 66/66 runner and
settings bindings, every passing MTP summary, zero-warning builds, the
32-assembly inventory, 2,776 observed tracked source files, and the exact
Top-25 CRAP rows. All nine broker-log hashes agree with the four fixture
records. The fixture files remain internally verified but are not
cryptographically linked to the aggregate or its receipts. These measurements
establish execution and observed coverage, not the quality of every test or
provider behavior outside the observed runs.
