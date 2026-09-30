# T160 product-wide ServiceBus profile — `096e8ecfa`

The strict aggregate `artifacts/t160-aggregate.json` (SHA-256
`1e81a768e6512bc11e9a02fbadca5b8d29bddb0d81482a12dea3479d7f895a99`)
accepted 33 verified receipts without `--partial`. All receipts bind the
same source and test trees at commit
`096e8ecfa89718a66630098a3118d55181d2a16d`. All 32 product
assemblies, all Unit and LocalIntegration projects, the no-AVX2 mode and
the scalar mode are present. Selected test executions: 14,139 passed,
zero failed and zero skipped.

| Measure | T157 | T160 | Change |
| --- | ---: | ---: | ---: |
| Passed test executions | 14,138 | 14,139 | +1 |
| Covered physical lines | 87,578/94,786 | 87,584/94,786 | +6/0 |
| Line rate | 92.39550% | 92.40183% | +0.00633 points |
| Conservatively covered branches | 31,823/37,382 | 31,831/37,384 | +8/+2 |
| Conservative branch rate | 85.12921% | 85.14605% | +0.01684 points |
| Method identities | 26,194 | 26,194 | 0 |
| Methods with CRAP > 30 | 0 | 0 | 0 |

The only product source change since T157 is the T159 EF transactional
outbox rejection of `Guid.Empty` as a message ID. Its red-first SQLite test
failed against the old code, passed after the correction, and is included
in the exact-commit EF receipt (401/401). The complete EF local integration
receipt passed 96/96 with PostgreSQL and SQL Server. This profile contains
no test added only to change the coverage percentage.

All twelve provider fixture runs ended with empty teardown findings. The
finding records sit outside the receipt hash chain; each hash below binds
the separate fixture observation to this report. The broker log hashes
embedded in the finding records were independently checked against their
files. Profile 29 used the controlled ActiveMQ outage and restored its
broker.

| Profile | Run suffix | Brokers | Findings SHA-256 |
| ---: | --- | --- | --- |
| 21 | `a4e3cb681f52` | Azurite | `cefa11d8237d2887501b3e6902b1a1dc9a1de790d2d17bc2572e764ec5045822` |
| 22 | `82af440c54bb` | LocalStack | `ec1d53505c8a55dadeedcdea148d5163ad4c448ed59b9d8741bcd95a1f278e4a` |
| 23 | `b9982a41cf81` | LocalStack | `965a68c69d3f4249b608bb732e6a5ff1622528311551ccd4f722f478f0e27f2b` |
| 24 | `0f259ae879fb` | Azurite | `5644d23f87eac9c6ea976355ed89a1a0beb27cb7de8aff6dd553f39a81a456de` |
| 25 | `822ac255d101` | RabbitMQ | `640f9ae5688ea2d73762185657c2627979a0f26c553789c3dd6df370c848d2dc` |
| 26 | `07be63b736aa` | PostgreSQL, SQL Server | `d51cd5c054474ad86b37efc2b22013b9f9974ca252a22138af4561a15928feea` |
| 27 | `5c99deca0f4a` | LocalStack | `3cc89360cad02f21b8ae1560f5ae664a327325c0c18d3cea1508c5d3b5379b73` |
| 28 | `620c3e26a852` | Azurite, Event Hubs | `005948a55f6c60fc55baf8ac90e8c26661fd2c7cad32b00f743514cbd2135c92` |
| 29 | `bb8cab1a4968` | ActiveMQ, Artemis | `9eac450586f91a91602aab54b270b7d4647169ab84b69b39553f29b8a3edf20d` |
| 30 | `3cdb8be1eae4` | Azure Service Bus | `988242feccb1ff4980675688c9e269815f62c9143b4084cd0b2973d1c539a16c` |
| 31 | `5c663ef96545` | PostgreSQL | `fb8dd1e874e9e55cd021e872d2e284abdf88c00ed2d820a554daeba757820cb7` |
| 32 | `1b0c0b2de365` | SQL Server | `bcc416e4f6d88098a039715cf2b4e38b7ed56361596b562530ad6f208241bcb9` |

The first sandbox attempt was excluded: NuGet's security advisory query
could not reach nuget.org and restore failed with `NU1900` treated as an
error. A network-approved rerun completed normally. A separate initial
Azure Storage probe supplied an incorrect minimum of 50 for its 25-test
project; MTP exited 9 despite all 25 tests passing. That probe was also
excluded. The selected receipt uses the previous verified minimum and
passed 25/25. Neither failed attempt appears in the aggregate.

The branch figure uses the repository's conservative Cobertura merge; it
does not infer shared branch identities across independent reports. The
Microsoft `coverage-analysis` skill's PowerShell scripts remain unavailable
because `pwsh` is absent; the repository's hash-checked Python receipt
runner and strict aggregator calculate these method-level CRAP values.
Local emulators do not prove real cloud IAM, lifecycle deletion, broker
failover or every deployment configuration. The metric gate is green at
this commit; zero remaining product defects cannot be proven by this run.

Independent read-only Red Team review reran the strict aggregate and
independently recomputed the Cobertura totals and method CRAP values. It
verified all 33 receipts, 355 binary hashes, 99 log hashes, 33 coverage
hashes, all twelve fixture finding hashes and their 15 broker log hashes.
Its verdict is PASS with no concrete P1/P2 measurement finding; the
highest method CRAP is exactly 30. The separate fixture finding records
remain outside the receipt hash chain, as noted above.
