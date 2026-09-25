# Cumulative product coverage profile after transport bus factory review

## Tested change

- `TransportRegistrationBusFactory.CreateBus` delegates its existing bus construction block to `BuildConfiguredBus` inside the same exception boundary. A DI test induces both a bus-instance specification failure and a creation-fault observer failure, then asserts that the original exception instance remains the reported cause and each relevant callback runs once.
- The changed product and test projects built in Release with zero warnings and errors. The focused DI regression passed, and the full Core suite passed **6,427/6,427** tests with Microsoft CodeCoverage. Report: `artifacts/coverage-a-plus-20260925-transport-factory/core.cobertura.xml` (SHA-256 `45a25ed10fc03db0bef34618c9565232a8cc02a5b5115e745468d2d9dc3f463c`).
- A read-only adversarial Red Team review found no concrete regression in construction order, configuration, error identity, or test assertions.
- The complete Microsoft Testing Platform Unit/Architecture gate passed **10,420/10,420**, zero failures or skips (`/private/tmp/vicione-servicebus-transport-factory-unit-gate.log`, SHA-256 `3b030b8364d4d6064cfd60ddba387c6c64828de38e902d3c06acdce41f5076e8`).
- The Release artifact identity gate scanned **183 artifacts** (83 DLLs, 35 PDBs, 65 packages) and found **0 issues**. Result: `artifacts/identity-transport-factory-20260925-release-artifact-gate.json`. Its scope excludes older ignored Debug and Engineering outputs.

## Cumulative product result

| Measure | Current | Previous EF inbox profile |
| --- | ---: | ---: |
| Line coverage | 84,313 / 93,553 = **90.1232%** | 84,309 / 93,552 = 90.1199% |
| Conservative branch observation | 30,253 / 36,674 = **82.4917%** | 30,252 / 36,674 = 82.4890% |
| Methods with CRAP > 30 | **12 / 26,005** | 13 / 26,004 |

Across the previous 54 reports, this factory file contributed 104/135 executable lines and 50/72 conservative branches. The fresh Core report measures 108/136 lines and 51/72 branches. `CreateBus` improved from complexity 30, 46/57 lines, CRAP **36.468** to complexity 20, 34/41 lines, CRAP **21.99**. The extracted `BuildConfiguredBus` has complexity 10, 17/17 lines, CRAP **10.00**.

## Provenance and limits

This is a cumulative overlay of fresh Core coverage for the changed file on the preceding 54-report, 32-product-assembly profile plus the later EF inbox overlay. Other provider reports remain inherited. Cobertura does not provide stable branch identities across reports; this profile keeps the largest observed covered count at each branch location. The mixed-commit aggregate is not a fresh whole-repository coverage run. The Microsoft `code-testing-agent`, `test-gap-analysis`, `run-tests`, and `coverage-analysis` skills guided test selection, execution, and risk measurement.

Global A+ remains open: branch observation is **82.4917%**, and **12 methods** still exceed CRAP 30. Further work must exercise or simplify real product behavior with strong assertions.
