# C29 validation

## Requirement closure

| Requirement | Evidence |
|---|---|
| Preserve every useful inherited capability | All four R0 identities have exact executing replacements in `INHERITED_BEHAVIOR_DISPOSITION.json`. |
| Treat inherited tests as a minimum | Source-derived cases cover virtual time, nested token ownership, exact causes, lifecycle completion, timer disposal, public boundaries, all configuration projections, invalid durations and immutable build snapshots. |
| Fix product defects at their owner | Timeout classification, fault projection, time ownership, resource lifetime and configuration ownership are corrected in product code; no test accommodates an inherited defect. |
| Mirror source ownership | Replacements are under `tests2/ViciOne.ServiceBus.Tests/Middleware/Timeout`, matching the owning middleware and timeout configuration paths. |
| Prove test sensitivity | `MUTATION_VALIDATION.md` records twelve independent one-cause product mutations and their exact failures. |

## Final commands and results

| Gate | Result |
|---|---|
| Core source-owner Release build | exit 0; 0 warnings; 0 errors |
| Focused restored C29 executable | 30 total; 30 passed; 0 failed; 0 skipped |
| Unfiltered Core source-owner executable | 784 total; 784 passed; 0 failed; 0 skipped |
| Unit solution Release build | exit 0; 0 warnings; 0 errors |
| Unfiltered UnitArchitecture MTP profile with predeclared floor 1388 | 1388 total; 1388 passed; 0 failed; 0 skipped |
| LocalIntegration Release build and profile | build 0 warnings/errors; 3 total; 3 passed; 0 failed; 0 skipped |
| Remaining inherited Core project Release build after deletion | exit 0; 0 warnings; 0 errors |
| Complete Engineering solution Release build | exit 0; 0 warnings; 0 errors |
| Bounded C29 `dotnet format ... whitespace --verify-no-changes` | exit 0 |
| Requirement/disposition JSON, reference closure and Git diff checks | exit 0 |

## Independent full-gate correction

The first complete UnitArchitecture run found a race in the previously accepted saga connector test:
it read `ConsumeCount` after saga creation but before the consume observer had acknowledged message
processing. The product was not changed. The test now awaits the explicit consume observation before
reading saga state, passes three isolated repetitions, and the subsequent complete 1388-case run is
green.

## Final disposition

PASS. C29 is technically complete in the current working tree. No commit or push is implied by this
report.
