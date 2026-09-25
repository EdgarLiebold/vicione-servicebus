# Cumulative product coverage profile at 3df7a5cd7

## Exact-commit evidence

- Product commit: `3df7a5cd7` (`SerializedDurableSend.cs`, `CHANGELOG.md`). `Validate` keeps the identity checks, optional UUID checks and capacity access in their original order. The unchanged destination and media-type checks now form `ValidateDestinationAndContentType`. Existing contract tests assert each invalid field's exception and parameter, exact size bounds, valid zero-byte bodies, and storage size. No test was added solely to increase a metric.
- The focused Abstractions project passed **801/801** tests. Fresh Microsoft CodeCoverage report: `artifacts/sdk/bin/ViciOne.ServiceBus.Abstractions.Tests/release/TestResults/artifacts/coverage-a-plus-20260925-durable-contracts/abstractions.cobertura.xml` (SHA-256 `ba420d79b60ef4d60a087853b7812c08dd796a8217e0b418c0ecbc61e6ee20fb`). `Validate` is 12/12 lines, complexity and CRAP 18; `ValidateDestinationAndContentType` is 13/13 lines, complexity and CRAP 16. Their former combined method was 23/23 lines, complexity and CRAP 34.
- The serial Release Unit/Architecture build completed with **0 warnings, 0 errors** (`/private/tmp/vicione-servicebus-3df7a5cd7-build.log`, SHA-256 `d0324ffad4b00219654c7d913d3143f19242d318889c7b0210e22bf702ccde2c`). The complete Microsoft Testing Platform Unit/Architecture gate passed **10,417/10,417**, zero failures or skips (`/private/tmp/vicione-servicebus-3df7a5cd7-gate.log`, SHA-256 `60c95ee3dba6d9718926c70198dc8a7986c4fd3571807e5724aef91e09d04d5f`).
- The generated source identity gate returned **PASS with 0 findings** and no evidence-file diff. The generated change list check passed with 16,394 entries. A read-only adversarial Red Team review returned **product PASS**: validation order, exception types and parameter names, length limits, and side effects are preserved. Its documentation finding was resolved by making clear that existing tests cover individual invalid fields while unchanged multi-field precedence is supported by the diff.

## Cumulative result

| Measure | `3df7a5cd7` | Previous `87402d8d7` |
| --- | ---: | ---: |
| Line coverage | 84,287 / 93,535 = **90.1128%** | 84,285 / 93,533 = 90.1126% |
| Conservative branch observation | 30,251 / 36,676 = **82.4817%** | 30,251 / 36,676 = 82.4817% |
| Methods with CRAP > 30 | **15 / 26,001** | 16 / 26,000 |

The preceding 54-report profile contains the changed Abstractions source in 51 reports. Its cumulative executable lines were 34/34, including ten unchanged auto-property locations, with 32/34 conservative branches. The fresh Abstractions report covers all 26/26 current executable locations it instruments. Combined with the ten unchanged auto-properties, the current source is 36/36 lines; the two newly measured lines are covered. Branch counts stay 32/34. One method is added and the former CRAP-34 method is replaced by two methods below 30.

## Provenance and limits

This is a cumulative overlay of the fresh Abstractions report on the preceding 54-report, 32-product-assembly profile. Other provider reports, including broker fixtures, remain inherited and were not rerun for this iteration. Cobertura lacks stable branch identities; the conservative figure uses the largest covered count per branch location as in the previous profile. The mixed-commit aggregate tracks progress and is not a fresh whole-repository coverage run. The Microsoft `coverage-analysis`, `code-testing-agent` and `run-tests` skills informed method selection, test sufficiency review and execution.

Global A+ remains open: conservative branch coverage is **82.4817%**, and **15 methods** still exceed CRAP 30. Further tests must assert real product behavior, failure handling, boundaries or regressions.
