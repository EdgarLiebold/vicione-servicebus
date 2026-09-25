# Cumulative product coverage profile after job-attempt state-machine extraction

## Tested change

- `JobAttemptStateMachine` now registers events in `ConfigureEvents` and then registers its schedule and state behavior in `ConfigureStateBehavior`. The registration order and public state-machine properties are unchanged.
- The changed JobService and Core test projects built in Release with zero warnings and errors. Their DLL copies match by SHA-256. The full Core suite passed **6,427/6,427** tests with Microsoft CodeCoverage. Report: `artifacts/coverage-a-plus-20260925-job-attempt/core.cobertura.xml` (SHA-256 `ff99508cb7767d3b2742386fc22b1c74850d24dff6e22e4daf6bd974412b5dc1`). Its existing tests exercise startup, liveness escalation, status outcomes, cancellation, fault identity, late acknowledgement, and finalization.
- A read-only adversarial Red Team review found no concrete change in event, schedule, and state registration order or in visible API and behavior.
- The Release artifact identity gate scanned **183 artifacts** (83 DLLs, 35 PDBs, 65 packages) and found **0 issues**. Result: `artifacts/identity-job-attempt-20260925-release-artifact-gate.json`. Its scope excludes older ignored Debug and Engineering outputs.
- The complete Microsoft Testing Platform Unit/Architecture gate passed **10,420/10,420**, zero failures or skips (`/private/tmp/vicione-servicebus-job-attempt-unit-gate.log`, SHA-256 `fbbb8fcfe3bbc251f113036adc4352892a6993ff78fdcaf8b7efbd6534301d15`).

## Cumulative product result

| Measure | Current | Previous transport factory profile |
| --- | ---: | ---: |
| Line coverage | 84,302 / 93,542 = **90.1221%** | 84,313 / 93,553 = 90.1232% |
| Conservative branch observation | 30,253 / 36,674 = **82.4917%** | 30,253 / 36,674 = 82.4917% |
| Methods with CRAP > 30 | **11 / 25,992** | 12 / 26,005 |

Across the previous 54 reports, this source file contributed 237/237 executable lines, 104/118 conservative branches, and 72 method identities from inherited builds. The fresh Core report measures 226/226 lines, 104/118 branches, and 59 method identities. The decreased line and method denominators reflect a fresh compiler output replacing historical generated lambda identities; all new executable lines are covered. The constructor improved from complexity 36 and CRAP **36.00** to complexity 1 and CRAP **1.00**. `ConfigureEvents` and `ConfigureStateBehavior` each have complexity 18, complete method-line coverage, and CRAP **18.00**.

## Provenance and limits

This is a cumulative overlay of fresh Core coverage for the changed file on the previous 54-report, 32-product-assembly profile plus later EF inbox and transport factory overlays. Other provider reports remain inherited. Cobertura does not provide stable branch identities across reports; this profile keeps the largest observed covered count at each branch location. The mixed-commit aggregate is not a fresh whole-repository coverage run. The Microsoft `run-tests` and `coverage-analysis` skills guided execution and risk measurement; existing hard behavior tests were retained without adding coverage-only tests.

Global A+ remains open: branch observation is **82.4917%**, and **11 methods** still exceed CRAP 30. Further work must exercise or simplify real product behavior with strong assertions.
