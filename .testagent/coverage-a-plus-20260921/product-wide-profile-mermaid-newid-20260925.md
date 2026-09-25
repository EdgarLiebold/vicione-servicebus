# Cumulative product coverage profile after Mermaid encoding and NewId format tests

## Tested changes

- `StateMachineMermaidGenerator` now maps Mermaid syntax characters in `SyntaxEntity` and leaves the existing Unicode/control-character fallback in `EscapeLabel`. The Visualizer product and test projects built in Release with zero warnings and errors. An initial coverage run used a stale Visualizer DLL copied into the test directory and is excluded. A dependent rebuild made the product and test-directory DLLs byteidentical; the corrected suite passed **29/29** tests with Microsoft CodeCoverage. Report: `artifacts/coverage-a-plus-20260925-mermaid/visualizer.cobertura.xml` (SHA-256 `1ce34fbd8e5d4e16738a9b55154b3e1001bf797b6009413b86ed3b3a34031a98`). Existing exact-output tests check graph syntax, syntax entities, controls, Unicode scalars, and unpaired surrogates.
- New `NewIdGuidInteropTests` compare B/D/N/P standard and sequential forms, uppercase/lowercase variants, null/empty defaults, and invalid format shapes with independent fixed and framework `Guid` values. The Abstractions test project built in Release with zero warnings and errors; **810/810** tests passed with Microsoft CodeCoverage. Report: `artifacts/coverage-a-plus-20260925-newid/abstractions.cobertura.xml` (SHA-256 `68a21c7a3389dfe092d34ea465f15effc18029128b1797664100baca5cc43cb4`).
- Read-only adversarial Red Team review passed the Mermaid refactor. It found a self-referential sequential-format oracle in the first NewId test version; the final test uses a fixed independently derived Guid, and the Red Team closed that finding.
- The Release artifact identity gate scanned **183 artifacts** (83 DLLs, 35 PDBs, 65 packages) and found **0 issues**. Result: `artifacts/identity-mermaid-newid-20260925-release-artifact-gate.json`. Its scope excludes older ignored Debug and Engineering outputs.
- The complete Microsoft Testing Platform Unit/Architecture gate passed **10,429/10,429**, zero failures or skips (`/private/tmp/vicione-servicebus-mermaid-newid-unit-gate.log`, SHA-256 `7c1823897aae24904073152f449a31640c0bf738924702400cef96b111009386`).

## Cumulative product result

| Measure | Current | Previous job-attempt profile |
| --- | ---: | ---: |
| Line coverage | 84,303 / 93,540 = **90.1251%** | 84,302 / 93,542 = 90.1221% |
| Conservative branch observation | 30,261 / 36,676 = **82.5090%** | 30,253 / 36,674 = 82.4917% |
| Methods with CRAP > 30 | **9 / 25,994** | 11 / 25,992 |

The previous Visualizer report measured 68/68 lines, 45/45 branches, and 6 method identities in `StateMachineMermaidGenerator.cs`. The fresh report measures 66/66 lines, 47/47 branches, and 8 method identities. `EscapeLabel` improved from complexity 35 and CRAP **35.00** to complexity 4 and CRAP **4.00**; `SyntaxEntity` has complexity 23, 15/15 covered lines, and CRAP **23.00**.

The unchanged `NewId.cs` file had 143/248 lines and 78/104 conservative branches in the 54-report baseline. Union with the new Abstractions report raises it to 146/248 lines and 84/104 branches without changing its 41 method identities. `ToString(string, IFormatProvider)` improved from 18/21 lines and CRAP **32.62** to 21/21 lines and CRAP **30.00**. The newly tested format errors, defaults, and suffix/case variants distinguish product behavior, not just method execution.

## Provenance and limits

This overlays the fresh Visualizer report for the changed source file and unions the fresh Abstractions observations for the unchanged `NewId.cs` file with the preceding 54-report, 32-product-assembly profile and later EF inbox, transport factory, and job-attempt overlays. Other provider reports remain inherited. Cobertura does not provide stable branch identities across reports; the conservative profile keeps the largest observed covered count at each branch location. This mixed-commit aggregate is not a fresh whole-repository coverage run. The Microsoft `code-testing-agent`, `test-gap-analysis`, `run-tests`, and `coverage-analysis` skills guided test design, execution, and risk measurement.

Global A+ remains open: branch observation is **82.5090%**, and **9 methods** still exceed CRAP 30. Further work must exercise or simplify real product behavior with strong assertions.
