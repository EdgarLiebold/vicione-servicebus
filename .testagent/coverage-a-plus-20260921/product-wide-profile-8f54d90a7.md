# T59 complete product-wide measurement — `8f54d90a7`

The connected multi-response Saga request packet is frozen at
`8f54d90a790d0cf0dd52b647fff3b07a28819596`. All 33 accepted coverage
receipts use source tree `24a2a9fb7d53e43572029be79355f12e18902849`
and test tree `209b8c2bbf5e87f2c3d8913ac082138c8fd67aab`. The strict
`tools/ci/aggregate_coverage_receipts.py` run completed without `--partial`;
the result is `artifacts/t59-aggregate.json`.

| Measure | T58 | T59 | Change |
| --- | ---: | ---: | ---: |
| Passed tests | 13,257 | 13,287 | +30 |
| Covered physical lines | 85,958/93,754 | 86,006/93,754 | +48 |
| Line rate | 91.68462% | 91.73582% | +0.05120 points |
| Conservatively covered branches | 31,057/36,845 | 31,084/36,845 | +27 |
| Conservative branch rate | 84.29095% | 84.36423% | +0.07328 points |
| Method identities | 26,071 | 26,071 | 0 |
| Methods with CRAP > 30 | 0 | 0 | 0 |

The receipts represent all required normal, local-integration, no-AVX2 and
scalar-fallback profiles over 32 product assemblies. Every accepted profile
passed without failure or skip, and each fresh build reported zero warnings and
errors. The highest CRAP score remains 30, on already fully covered methods;
there are no methods above the project's threshold.

An independent read-only audit checked 487 binary/log/XML hashes, 66
runner/settings bindings and all 33 test summaries. It reconstructed the
physical-line, conservative-branch, method and CRAP counts directly from the
Cobertura XML; all numbers agree with the strict aggregator. Four accepted
fixture groups have empty findings after teardown. Their nine broker logs
match the recorded SHA-256 hashes.

The first Azure Table local-integration attempt (`artifacts/t59-profile-24`)
failed an existing JobService observation test: no `IJobFaulted` was observed
before the observation sequence ended. The same profile log also records Azure
Table ETag 412 conflicts in concurrently running Future tests; it does not
establish those conflicts as the cause of the JobService failure. That run has no valid
receipt and is not part of the accepted aggregate. The new isolated fixture
passed the same complete 40-test project at
`artifacts/t59-profile-24-r1/receipt.json`; both fixture teardown finding
groups were empty. This sporadic failure is retained as a follow-up signal,
not presented as a fixed product defect.

T59 added 30 behavior cases across two Quartz integration files, with the
existing one-response cases retained. The tests check stored request IDs
distinct from Saga/body IDs; accepted response URNs and response address;
first/second/third replies, service fault and real Quartz timeout; callback
owner override; missing and wrong IDs; two simultaneous Saga owners with
separate triggers; and old one-/two-/three-response request generations.
Read-only Red Team identified two assertion gaps, which were repaired and
re-reviewed. Two isolated mutations of `Completed2` and `Completed3` request
correlation each failed the matching integration variant and were restored to
the original product-source SHA-256
`b34154afbdcca7bef514d35fd589519f2ae14c7b9d2bdb1f940cb87f5856eeae`.
The complete affected Quartz project passed 318/318 including Requirement
projection. No product source changed in this packet.

The conservative branch count combines independent Cobertura locations and
does not claim an exact branch identity union across profiles. Global Line and
Branch A+ remain open: 7,748 physical lines and 5,761 conservative branch
outcomes are still uncovered. The next packet should again group connected
product behavior and use focused feedback before one complete exact-commit
measurement.
