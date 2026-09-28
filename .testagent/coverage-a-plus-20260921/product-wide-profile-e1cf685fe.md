# T63 complete product-wide measurement — `e1cf685fe`

The strict 33-receipt aggregate at source/test commit
`e1cf685fe577ea7c18fca57dc3f5e64c406b0899` is
`artifacts/t63-aggregate.json`. It includes every required normal and local
integration project, no-AVX2 and scalar fallback, across all 32 product
assemblies. All 13,318 tests passed without failures or skips. The four
canonical broker fixtures have empty finding lists. The first profile-00
attempt stopped during restore because sandbox networking blocked NuGet audit
metadata (`NU1900`); the accepted `t63-profile-00-r1` used the normal network
path and passed all 6,897 Core tests. The failed restore produced no receipt.

| Measure | T59 | T63 frozen commit | Change |
| --- | ---: | ---: | ---: |
| Passed tests | 13,287 | 13,318 | +31 |
| Physical lines | 86,006/93,754 | 86,113/93,862 | +107/+108 |
| Line rate | 91.73582% | 91.74426% | +0.00844 points |
| Conservative branches | 31,084/36,845 | 31,110/36,869 | +26/+24 |
| Conservative branch rate | 84.36423% | 84.37983% | +0.01560 points |
| Method identities | 26,071 | 26,084 | +13 |
| Methods with CRAP > 30 | 0 | 1 | +1 |

The one CRAP regression is
`EventHubProducer.BatchSendPipe.SendAsync` (`MoveNext`): complexity 36,
48/52 lines, CRAP 36.5899. Coverage cannot lower it below 30 because its
complexity alone is 36. This is an accepted diagnostic measurement, not an
A+ release claim.

The method was subsequently split into pending-context creation, admission,
provider submission, partial-outcome handling and progress marking. This
changed the source tree after the frozen commit, so the figures above are not
figures for the later refactored tree. A fresh focused Microsoft CodeCoverage
report (`/private/tmp/vsb-t63-refactor-outcome-coverage-r1.xml`) measures the
new `SendAsync` at complexity 6, 26/29 lines, CRAP 6.0399; its largest new
helper, `HandlePartialFailureAsync`, at complexity 14, 24/27 lines, CRAP
14.2689. All seven measured batch-pipe methods are below 30. The focused
Outcome suite passes 12/12 under coverage and the sender suite passes 15/15.
Read-only adversarial review found no semantic regression in the extraction.
These focused figures do not replace a complete exact-commit aggregate for
the refactored tree.

The next measurement interval groups six to eight connected packets unless a
cross-assembly contract change, fresh critical CRAP hotspot or required
milestone demands an earlier full audit. Every packet still requires
red-first behavior evidence where a defect is fixed, affected-project tests,
adjacent real integration controls where relevant, and adversarial review.
