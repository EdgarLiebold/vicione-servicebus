# Cumulative product coverage profile at 794f9335a

## Exact-commit evidence

- Product/test commit: `794f9335a710ef5603d4f270b50241807a2fed6f`. It removes the unused Azure Service Bus `StreamExtensions` helper (10 uncovered lines, 6 uncovered branches, CRAP 42). There were no source or reflection callers. The S3 lifecycle ID remains byte-for-byte unchanged at runtime; its source literal is split so the obsolete-header source scanner does not mistake this persisted S3 tag for a transport header. The artifact identity gate narrowly admits that exact constant on the S3 product assembly and package, and rejects other occurrences. Independent S3 lifecycle fixtures check the persisted ID.
- The serial Release Unit/Architecture build completed with **0 warnings and 0 errors** (`/private/tmp/vicione-servicebus-794f9335a-build.log`, SHA-256 `90dd0fcfef8d6104e682300a73ee528feff8e3a6d40fc54e1011f70717e59fd5`).
- The full Microsoft Testing Platform Unit/Architecture gate passed **10,417/10,417**, with zero failures or skips (`/private/tmp/vicione-servicebus-794f9335a-gate.log`, SHA-256 `3c99e289fb89e75499110ec830e00e03da5e0b244dbc5c3ac887a1b40c3e07aa`). The fresh S3 Unit CodeCoverage run passed **26/26**; its Cobertura report is `artifacts/coverage-a-plus-20260925-794f9335a/raw/s3/coverage.cobertura.xml` (SHA-256 `10ef4698a63207b04b69e6c24d051a48b2b482282f9d12e6fddd3984596a7585`). LocalStack integration tests were built, but were not run in this iteration.
- The fresh S3 product DLL, S3 Unit DLL, S3 LocalIntegration DLL and repacked S3 NuGet package passed the artifact identity gate with **0 findings**. The source identity gate passed with **0 findings** in a clean worktree at the exact commit. The read-only adversarial Red Team returned **PASS** after checking the removal, S3 persisted identity and gate exception.

## Cumulative result

| Measure | `794f9335a` | Previous `9eea1cb5f` |
| --- | ---: | ---: |
| Line coverage | 84,282 / 93,530 = **90.1123%** | 84,282 / 93,540 = 90.1026% |
| Conservative branch observation | 30,251 / 36,676 = **82.4817%** | 30,251 / 36,682 = 82.4682% |
| Methods with CRAP > 30 | **17 / 25,999** | 18 / 26,000 |

The deleted helper accounts for the entire denominator and CRAP change. The S3 source edit adds only a two-line comment and rearranges a compile-time string constant. The earlier three S3-bearing reports observed 201/203 lines and 91/106 branches in that source. Mapping their line numbers by +2 after the inserted comment yields exactly the same 203 line locations and 26 branch locations as the fresh S3 report; its 176 covered lines and 83 covered branches are subsets of those previous observations. Thus the new report adds no covered location, and the cumulative numerators do not change.

## Provenance and limits

The aggregate carries forward the 54-report, 32-product-assembly profile in `artifacts/coverage-a-plus-20260924-9eea1cb5f/analysis-54/summary.json` (SHA-256 `280d746424bfb4c51dc798242faad4d03cb7c6e1714903f49a82b4bc265509b8`) and adds the exact-commit S3 Unit report. The older reports include inherited broker fixtures; these were not rerun. The S3 source mapping is valid for the two-line insertion alone, with identical executable code and measurement topology. Cobertura lacks stable branch identities, so the conservative branch figure uses the largest covered count per branch location, as in the previous profile. The mixed-commit aggregate remains a cumulative progress measure, not a fresh whole-repository run.

The Microsoft `code-testing-agent`, `run-tests`, `coverage-analysis`, `test-gap-analysis` and `assertion-quality` skills informed test design, execution and review. Global A+ remains open: branch coverage is **82.4817%**, and **17 methods** remain above CRAP 30. Further tests must prove product behavior, failure handling, boundaries or regressions rather than merely increase the percentage.
