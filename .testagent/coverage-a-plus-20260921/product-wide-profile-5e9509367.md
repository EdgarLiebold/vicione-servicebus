# T58 complete product-wide measurement — `5e9509367`

The connected registration, scope, retry/rescue and journal-owner packet is
frozen at `5e9509367fab17a95bfe49236517185e07bcf214`. All 33 fresh
coverage receipts were collected from that unchanged source and test tree.
`tools/ci/aggregate_coverage_receipts.py` accepted them without `--partial`;
its output is `artifacts/t58-aggregate.json`.

| Measure | T57 | T58 | Change |
| --- | ---: | ---: | ---: |
| Passed tests | 13,250 | 13,257 | +7 |
| Covered physical lines | 85,902/93,754 | 85,958/93,754 | +56 |
| Line rate | 91.62489% | 91.68462% | +0.05973 points |
| Conservatively covered branches | 31,048/36,845 | 31,057/36,845 | +9 |
| Conservative branch rate | 84.26652% | 84.29095% | +0.02443 points |
| Method identities | 26,071 | 26,071 | 0 |
| Methods with CRAP > 30 | 0 | 0 | 0 |

The 33 receipts represent all required normal, local-integration, no-AVX2
and scalar-fallback profiles across 32 product assemblies. They share source
tree `24a2a9fb7d53e43572029be79355f12e18902849` and test tree
`c460d4a88153d98f6f045fc02a62a8964165707d`. Every test passed without
failure or skip, and every fresh build reported zero warnings and errors.

An independent read-only calculation checked 487 binary/log/XML hashes,
66 runner/settings bindings, all 33 test summaries, and reconstructed the
line, conservative branch, method and CRAP counts directly from the Cobertura
XML. Every number agrees with the strict aggregator. The four canonical
fixture groups have empty findings after teardown. All nine PostgreSQL,
Azurite, LocalStack, ActiveMQ, Artemis, Event Hubs, RabbitMQ, Service Bus
and SQL Server broker logs match their fixture hashes.

Four new integration-test methods yield seven cases: filtered consumer
discovery with its definition and real request/response; open and closed
compensation filters with scope identity and disposal; retry exhaustion before
selected rescue while a healthy neighbor delivers; and journal observers on
either of two active buses with exact owner correlation IDs. Read-only Red
Team review strengthened the last two owner/scope oracles. An inverted
product journal-owner filter failed both new journal variants on the wrong
IDs; original product bytes were restored and the Core suite then passed
6,894/6,894. No product source changed and no product defect is claimed for
this test-only packet. The detailed [T58 acceptance and review log](t58-registration-scope-failure-journeys.md)
records the test-authoring corrections and evidence limits.

The conservative branch figure combines independent Cobertura locations and
does not claim an exact identity union across profiles. Product-wide Line and
Branch A+ remain open: 7,796 physical lines and 5,788 conservative branch
outcomes are still uncovered. CRAP>30 remains zero. The next packet should
again group connected product behaviors and use focused feedback before one
complete exact-commit measurement.
