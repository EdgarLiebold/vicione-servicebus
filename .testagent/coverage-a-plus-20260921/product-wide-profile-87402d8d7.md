# Cumulative product coverage profile at 87402d8d7

## Exact-commit evidence

- Product commit: `87402d8d7` (`StateMachineMermaidGenerator.cs`, `CHANGELOG.md`). The default Unicode branch of Mermaid label encoding moved unchanged to `AppendUnicodeCharacter`; the loop and all reserved-character replacements remain in `EscapeLabel`. This lowers the fully covered method's complexity from 35 to 25; the new helper has complexity 10. No behavior-only coverage test was added because existing tests already assert the exact generated document for reserved characters, controls, valid Unicode pairs and unpaired surrogates.
- The focused Visualizer project passed **29/29** tests. Fresh Microsoft CodeCoverage report: `artifacts/sdk/bin/ViciOne.ServiceBus.StateMachineVisualizer.Tests/release/TestResults/artifacts/coverage-a-plus-20260925-mermaid/mermaid.cobertura.xml` (SHA-256 `2f17e2fa12ad414d113d71838832d0c9e68b2219b2ed8916726c1f8e35d5776a`). Its `EscapeLabel` is 27/27 lines, CRAP 25; `AppendUnicodeCharacter` is 14/14 lines, CRAP 10.
- The serial Release Unit/Architecture build completed with **0 warnings, 0 errors** (`/private/tmp/vicione-servicebus-87402d8d7-build.log`, SHA-256 `3fc4ceb12a90c567933a7f2ae9adb2a015e2ddcd25c0d15ed922347e1addb7d4`). The complete Microsoft Testing Platform Unit/Architecture gate passed **10,417/10,417**, zero failures or skips (`/private/tmp/vicione-servicebus-87402d8d7-gate.log`, SHA-256 `f632ce98a5ba1d30da0a8b170380b412a979913b106af3e117f23c346904cb0d`).
- A read-only adversarial Red Team review returned **PASS**: the index increment for a valid UTF-16 pair, guard order, invariant formatting and all syntax-sensitive replacements are preserved. The regenerated source identity gate returned **PASS with 0 findings**; its initial scan had found one stale generated baseline binding, which was corrected by regenerating the two affected evidence files. The generated change list check passed with 16,393 entries.

## Cumulative result

| Measure | `87402d8d7` | Previous `794f9335a` |
| --- | ---: | ---: |
| Line coverage | 84,285 / 93,533 = **90.1126%** | 84,282 / 93,530 = 90.1123% |
| Conservative branch observation | 30,251 / 36,676 = **82.4817%** | 30,251 / 36,676 = 82.4817% |
| Methods with CRAP > 30 | **16 / 26,000** | 17 / 25,999 |

The prior 54-report profile contains the modified Visualizer source in exactly one report. That source moves from 68/68 to 71/71 measured lines, stays at 45/45 branches and gains one method. All 3 new measured lines are covered. No other product source changed.

## Provenance and limits

The aggregate overlays the fresh Visualizer report on the preceding 54-report, 32-product-assembly cumulative profile. Other provider reports, including broker fixtures, remain inherited and were not rerun for this iteration. Cobertura lacks stable branch identities; the conservative figure uses the maximum covered count per branch location as in the previous profile. This mixed-commit aggregate tracks progress; it is not a fresh whole-repository coverage run. The complete Unit/Architecture test gate and source identity gate were rerun at the product commit. The Microsoft `coverage-analysis`, `code-testing-agent` and `run-tests` skills informed the method selection, test sufficiency review and execution.

Global A+ remains open: conservative branch coverage is **82.4817%**, and **16 methods** still exceed CRAP 30. Subsequent work must remain tied to real product contracts and meaningful failure or boundary behavior.
