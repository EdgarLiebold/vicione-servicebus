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

An independent read-only audit reconstructed all figures directly from the
33 Cobertura XML documents and found no discrepancy. It checked all 33
source/test tree and runner/settings bindings; 355 binary/PDB hashes, 99
log hashes and 33 XML hashes (487/487 artifact files); and each complete MTP
test summary. The 32 assembly and 2,775 observed source-file identities
agree. The Top-25 CRAP rows match exactly. The four fixture finding files
each contain `findings: []`, and all nine brokerlog hashes agree with their
recorded values. Their own SHA-256 values are:

| Fixture | Findings SHA-256 |
| --- | --- |
| Shared six-broker `vicione-28bce5cebbe0` | `1562f8300de891977f1383ec040f81dbfcf7354ab2fd558c05ce2ca5b491ac96` |
| RabbitMQ `vicione-0b39d2dbceca` | `c17dbf6647b131b7ca746bee5b6a06b4ffe06b52e6df9557c1631693de4d3806` |
| Service Bus `vicione-377f590cead0` | `530c4dd23cce779fa3104011f1744df08bb091fe53baedd57cafabbd96a74461` |
| SQL Server `vicione-0220616f2f9c` | `a6940e4f17b06d501e0d3b23ac397e83890c4f6a6e898b37dd627434012440e1` |

The receipts do not themselves bind those four separate fixture files
cryptographically. The independent audit verified their internal hashes and
the run logs; the table records their exact bytes for later review.

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
