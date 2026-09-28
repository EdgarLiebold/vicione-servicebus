# T57 complete product-wide measurement — `d0263d89e`

The connected ingress-middleware packet is frozen at
`d0263d89ef457d93271047e950e4f6fd94e76e08`. All 33 exact-commit coverage
receipts were collected from that unchanged source and test tree and accepted by
`tools/ci/aggregate_coverage_receipts.py` without `--partial`. The aggregate is
`artifacts/t57-aggregate.json`.

| Measure | T56 | T57 | Change |
| --- | ---: | ---: | ---: |
| Passed tests | 13,222 | 13,250 | +28 |
| Covered physical lines | 85,889/93,754 | 85,902/93,754 | +13 |
| Line rate | 91.61102% | 91.62489% | +0.01387 points |
| Conservatively covered branches | 31,036/36,845 | 31,048/36,845 | +12 |
| Conservative branch rate | 84.23395% | 84.26652% | +0.03257 points |
| Method identities | 26,071 | 26,071 | 0 |
| Methods with CRAP > 30 | 0 | 0 | 0 |

The 33 receipts contain 32 product assemblies and all required unit, local
integration, no-AVX2 and scalar-fallback profiles. Aggregate source tree:
`24a2a9fb7d53e43572029be79355f12e18902849`; test tree:
`790837089425677dee06a7bb59c37d15f8dde1bf`. No test failed or was
skipped. The conservative branch count is an observation across independent
Cobertura reports; it is not an exact union of branch identities.

An independent read-only audit verified 487/487 binary, log and XML hashes,
66/66 runner/settings bindings, all test results and reconstructed the line,
conservative branch, method and CRAP counts from the XML without a discrepancy.
The 33 profiles represent 31 distinct test projects because Abstractions also
runs CPU variants; the full required project inventory is present.

The local broker runs covered PostgreSQL, Azurite, LocalStack, ActiveMQ,
Artemis, Event Hubs, RabbitMQ, SQL Server and the Azure Service Bus emulator.
Their four successful fixture groups recorded empty findings, and the nine
associated broker-log hashes match their fixture records. An earlier
combined Azure Service Bus start stopped before testing because its SQL Server
dependency exited during startup. An isolated SQL Server run and a fresh Azure
Service Bus run then completed; their receipts are profiles 32 and 30. The
failed startup is retained separately as infrastructure evidence and is not
included in the aggregate.

T57 fixes Consumer, Handler and Instance middleware so a downstream `next`
failure does not fault a consumer whose work has already succeeded. The 28 new
parameterized cases check notification ordering, exact contexts and exceptions,
consumer versus downstream cancellation, process Activity ownership, and
per-delivery Retry/Circuit Breaker behavior. The old product implementation
fails the three downstream-failure cases; the corrected implementation and
6,887/6,887 Core tests pass. The read-only product review found no concrete
remaining packet blocker after its proposed edge cases were added. The eight
test methods passed a manual Microsoft assertion-quality and pseudo-mutation
review; only the downstream-failure probe was actually executed against old
product bytes. No numerical mutation score is claimed.

This measurement improves observed coverage and keeps all measured CRAP values
at or below 30. Global line and branch A+ work remains open. The next packet
should group connected product behaviors and use narrow feedback while building,
with one full33 measurement after its own acceptance.
