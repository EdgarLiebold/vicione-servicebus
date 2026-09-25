# Cumulative product coverage profile after EF-Core reliable inbox extraction

## Tested change

- `EntityFrameworkReliableInboxContextFactory.SendAsync` now delegates the existing save, consumer pipe, commit, and failure handling block to `SendWithLeaseAsync`. Lease acquisition and its transaction lifetime remain in the caller. A quarantined duplicate is sent through the real EF-Core inbox factory and must leave the consumer untouched and retain its status, attempt count, failure details, and empty lease fields.
- The focused EF-Core Release suite passed **276/276** tests with Microsoft CodeCoverage, zero failures or skips. Report: `artifacts/coverage-a-plus-20260925-ef-inbox/ef.cobertura.xml` (SHA-256 `4f15b53a942057504b71a453eeb113602c91feedda4ad7441e55a120afc75445`). After the attempted whole-solution build, the test project was rebuilt so its product DLL hash again matched the product output (`10a15a3048ff7ecb1a1f2f1fe7c458aee76ca8156ef78ff348990e25a3ab0d20`); the focused suite passed again **276/276** (`/private/tmp/vicione-servicebus-ef-inbox-tests-final.log`, SHA-256 `1915b5f76bf153ced42f23e050322002488dc21b8b657e524d82546c134ccf2b`).
- A read-only adversarial Red Team review found no concrete change in transaction disposal, cancellation, exception filters, rollback, abort, or retry behavior. Its suggestion to assert retained failure and lease fields was applied before the final coverage run.
- The complete Microsoft Testing Platform Unit/Architecture gate passed **10,419/10,419**, zero failures or skips (`/private/tmp/vicione-servicebus-ef-inbox-full-gate.log`, SHA-256 `890308bd5d346947494354add15417912efa843ffedf43397f6df03716cc5edf`). The changed product and test projects built in Release with zero warnings and errors. A separate serial whole-solution build was stopped after slow progress, and a parallel retry did not finish; this iteration therefore does not claim a completed whole-solution build.
- The Release artifact identity gate scanned **183 artifacts** (83 DLLs, 35 PDBs, 65 packages) and found **0 issues**. Result: `artifacts/identity-ef-inbox-20260925-release-artifact-gate.json`. Its scope excludes older ignored Debug and Engineering outputs.

## Cumulative product result

| Measure | Current | Previous `d35c33649` |
| --- | ---: | ---: |
| Line coverage | 84,309 / 93,552 = **90.1199%** | 84,296 / 93,538 = 90.1195% |
| Conservative branch observation | 30,252 / 36,674 = **82.4890%** | 30,252 / 36,674 = 82.4890% |
| Methods with CRAP > 30 | **13 / 26,004** | 14 / 26,003 |

In the preceding 54-report profile, the changed EF-Core source file contributed 174/215 executable lines and 70/100 conservative branches. The fresh EF-Core report measures 187/229 lines and 70/100 branches. The former `SendAsync` state machine was 91/101 lines, complexity 36, CRAP 37.258. The current `SendAsync` is 68/70 lines, complexity 18, CRAP 18.008; `SendWithLeaseAsync` is 36/45 lines, complexity 18, CRAP 20.592. The duplicate test strengthens the terminal-state contract; it does not add a new conservative branch observation because that branch was already exercised elsewhere.

## Provenance and limits

This is a cumulative overlay of fresh EF-Core coverage for the changed file on the previous 54-report, 32-product-assembly profile. Other provider reports remain inherited. Cobertura does not provide stable branch identities across reports; this profile keeps the largest observed covered count at each branch location. The mixed-commit aggregate is not a fresh whole-repository coverage run. The Microsoft `code-testing-agent`, `coverage-analysis`, and `run-tests` skills guided test selection, execution, and risk measurement.

Global A+ remains open: branch observation is **82.4890%**, and **13 methods** still exceed CRAP 30. Further work must exercise or simplify real product behavior with strong assertions.
