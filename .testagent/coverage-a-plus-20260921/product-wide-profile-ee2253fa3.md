# T114 complete product-wide measurement — `ee2253fa3`

The strict aggregate `artifacts/t114-aggregate.json` accepted 33 fresh
receipts from the exact source/test commit
`ee2253fa3da617ce67dbe091db9f168382ca0d41`, without `--partial`.
All 32 product assemblies, required unit and local integration projects,
and the no-AVX2 and scalar-fallback profiles are present. All 13,783 test
executions passed. The source tree is
`d3a7a99f48b87ce8d655619a00ac74b41f460780`; the test tree is
`5c946820dd510a078b0e3a1320adcf4944dc2d3f`.

| Measure | T97 | T114 | Change |
| --- | ---: | ---: | ---: |
| Passed test executions | 13,604 | 13,783 | +179 |
| Covered physical lines | 86,639/93,963 | 87,071/94,328 | +432/+365 |
| Line rate | 92.20544% | 92.30663% | +0.10119 points |
| Conservatively covered branches | 31,312/36,927 | 31,583/37,199 | +271/+272 |
| Conservative branch rate | 84.79432% | 84.90282% | +0.10850 points |
| Method identities | 26,094 | 26,142 | +48 |
| Methods with CRAP > 30 | 0 | 5 | +5 |

The five measured CRAP hotspots are real open work, not an A+ result:

| CRAP | Method | Source area |
| ---: | --- | --- |
| 52.00 | `EnsureSessionPending` | EF transactional outbox |
| 42.08 | `SendAsync` | InMemory reliable inbox |
| 36.07 | `EnsureStagedRecordsPending` | EF scoped outbox |
| 32.05 | `SendAsync` | EF reliable inbox |
| 31.09 | `AddSendAsync` async lambda | EF scoped outbox |

The first method has full measured line coverage (31/31), but complexity
52 alone keeps CRAP above 30. The third method has 26/27 lines and
complexity 36. The two reliable-inbox `SendAsync` methods each have 81/84
measured lines. The EF scoped outbox lambda has 48/57. The next coherent
packet will inspect their behavior, write only
tests with a meaningful failure or outcome oracle, and simplify product
branches where the contract permits it. A coverage-only test is not a fix.

Twelve broker fixture runs provided the local profiles: Azurite twice,
LocalStack three times, RabbitMQ, PostgreSQL with SQL Server, Azurite with
Event Hubs, ActiveMQ with Artemis and the canonical controlled ActiveMQ
outage, Azure Service Bus, PostgreSQL, and SQL Server. All current fixture
runs completed with empty teardown findings. Their fixture logs and findings
are separate from the receipt hash chain, as in T97; the strict aggregate
verifies the local test runs and coverage reports, not broker-log bytes.
The following SHA-256 values bind the twelve current
`artifacts/run-output/vicione-{run}/fixture-findings.json` files to this
tracked report. Each has `findings: []`; profile 29 declares the canonical
`allowedBrokerOutage: activemq`. These fixture files remain separate from
the strict coverage receipt chain.

| Profile | Run suffix | Findings SHA-256 |
| ---: | --- | --- |
| 21 | `db9e95f572a9` | `b91ebf15a7d6a64daf41d7ee2b172ac892e9270c8507283961249262f243d7d3` |
| 22 | `7f3a0aa7bf43` | `93c1cd69fbe8102b3932c42368b87963473c46b72a212bcfddad0028fdfab3b3` |
| 23 | `904623eab278` | `57573d227a84be2a50380e14c6534f659703a8f34e2e0021c45aedf9852af9ea` |
| 24 | `802d5ff52e50` | `2a81e8c1a6c8b7a2c4f6aed0904f907e244e24ef98c40e3de10bf4eb48897c3b` |
| 25 | `613d07d01f57` | `e3a703bcb8a321541b78cc0e041049284afac1b2071bfdca7d7ddef75e9c69f0` |
| 26 | `85e67bb8640f` | `a7fb8d6c45ee8beab00ba560ace8cb223590235eda4adb6af9ca11d9e954ad35` |
| 27 | `983c64ed2bdd` | `108affbbf318c0dffb458fb9158434363a3e9033dee57b423fc093f74af16d7a` |
| 28 | `18d4e70d41b5` | `0abcb4aba138c0efa9003300fafb20fee99d927cefb82543a143f836e3934283` |
| 29 | `49a08ac6c4d4` | `027b29c60911fb44dca0ee4dc9078684fcf24e08c2e131edbabdf41354a91b8f` |
| 30 | `e9d87650ef10` | `8c41673938be108505ec773bcf367beae66d13a5d8402f9fc9a4426c304bdef2` |
| 31 | `cdc81a1faac0` | `8565b4a38c8ec245342930f782f4bd7012cc0a0387d974e1f7ceefaed1bb1694` |
| 32 | `fd60ee887295` | `12da6ae13c7b3c1811f9ff9866e4eafe73239013e5ca6cad6e37df0762fec115` |

The aggregate observed 2,776 tracked C# source files. This is a count of
source observations, not a count of untested methods or files. Cobertura
does not carry stable cross-report branch identities, so the branch number
uses the repository's conservative merge. The Microsoft
`coverage-analysis` skill's PowerShell helper could not run because `pwsh`
is unavailable here; the repository's strict, hash-checked Python receipt
aggregator produced this profile. An ad-hoc parallel-launch wrapper raised
a tuple-unpacking error after its child processes completed; individual
receipts and the aggregate were independently checked and passed.

Independent read-only Red Team review verified 33 project/mode receipts on
the exact commit, all 487 referenced binary/PDB, log and XML hashes,
13,783 passed executions without failures or skips, and all twelve fixture
findings and their 15 broker-log hashes. It found two documentation P2s in
the first draft: the third hotspot's measured line count and missing
fixture-findings SHA-256 values. Both were corrected above; re-review is
**PASS** with no remaining concrete P1/P2 discrepancy.

Global Line, Branch and CRAP A+ remain open. The next full 33-profile
measurement follows the larger-packet cadence, with an earlier run for a
shared runtime or coverage infrastructure change that invalidates local
evidence. During a packet, focused behavior tests run first; the complete
affected project runs on a frozen packet commit.
