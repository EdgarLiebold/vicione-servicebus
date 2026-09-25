# Cumulative product coverage profile after d35c33649

## Tested change

- `TypedDurableSender<TBus>` now separates endpoint/context creation from serialization and durable admission. It preserves validation, cancellation, payload proof, and admission order. Two new product tests prove that an endpoint without transport capability and an endpoint returning a noncanonical context both fail before durable admission, with the original cancellation token and exact endpoint interaction counts.
- The Core Release test assembly passed **6,426/6,426** tests with Microsoft CodeCoverage. Report: `artifacts/coverage-a-plus-20260925-d35c33649/core.cobertura.xml` (SHA-256 `c2b60e4449f521bdbd8d2370ceea55f9051c79def1f5f97fb81f9a5a67b94cbb`). The former `SendAsync` state machine was 63/70 covered lines, complexity 36, CRAP 37.296. The current `SendAsync` state machine is 21/21, complexity and CRAP 4; `CreateSendContextAsync` is 23/23, complexity and CRAP 12; `CreateSerializedSend` is 28/29, complexity 18, CRAP 18.013.
- A read-only adversarial Red Team review found no concrete product regression. It also reviewed three test-fixture changes that keep the same former-identity assertions while preventing the compiler from embedding that name as contiguous binary text.

## Cumulative product result

| Measure | Current | Previous `3df7a5cd7` |
| --- | ---: | ---: |
| Line coverage | 84,296 / 93,538 = **90.1195%** | 84,287 / 93,535 = 90.1128% |
| Conservative branch observation | 30,252 / 36,674 = **82.4890%** | 30,251 / 36,676 = 82.4817% |
| Methods with CRAP > 30 | **14 / 26,003** | 15 / 26,001 |

The prior cumulative 54-report profile observed 74/81 lines and 28/42 conservative branches in `TypedDurableSender.cs`. The fresh Core report observes 83/84 lines and 29/40 branches in the current file. This replaces only the changed file's contribution. One old high-CRAP method is replaced by three methods below 30; two methods are added overall.

## Gates and limits

- The serial Release solution build completed with **0 warnings and 0 errors** (`/private/tmp/vicione-servicebus-d35c33649-build.log`, SHA-256 `cfa1a5ccd3dcf88e1ec82e9a35d6e574c89383032815146b596e134955118370`). The complete Microsoft Testing Platform Unit/Architecture gate passed **10,419/10,419**, zero failures or skips (`/private/tmp/vicione-servicebus-d35c33649-gate.log`, SHA-256 `bf74d2dfb08510c4f314786c99191677604765b3de78e50e0702895b8f3c02a8`).
- After the test-fixture edits, focused Release builds had **0 warnings and 0 errors**. The affected Architecture tests passed **445/445** (`/private/tmp/vicione-servicebus-identity-architecture-test-final.log`, SHA-256 `eec81ef8814a7f2547c2dbd6bb4bebbc3730e4b98a00ab759f826c61e95ccc7d`); the EF-Core tests passed **276/276** (`/private/tmp/vicione-servicebus-identity-ef-test-final.log`, SHA-256 `c38841420419319b480018b99dcf1db89d8710b4f3c009c796dad9fd1122fbbc`). Both had zero failures or skips.
- After the fixture rebuild, the Release artifact identity gate scanned **183 artifacts** (83 DLLs, 35 PDBs, 65 packages) with **0 findings**. The unrestricted artifact gate still reports older ignored Debug and Engineering outputs; its failed result is retained at `artifacts/identity-d35c33649-artifact-gate.json`. The Release result is `artifacts/identity-d35c33649-release-artifact-gate.json`.
- This cumulative overlay combines fresh Core coverage for the changed file with inherited reports for other assemblies. It is not a fresh whole-repository coverage run. Cobertura has no stable branch identifiers across reports, so the branch value remains a conservative observation. The Microsoft `code-testing-agent`, `coverage-analysis`, and `run-tests` skills guided the test and coverage work.

Global A+ remains open: branch observation is **82.4890%**, and **14 methods** exceed CRAP 30. The missing payload-admission-proof line in `CreateSerializedSend` and other uncovered branches require meaningful product assertions before they count as progress.
