# C28 validation

## Requirement closure

| Requirement | Evidence |
|---|---|
| Preserve every useful inherited capability | All five R0 identities have exact executing replacements in `INHERITED_BEHAVIOR_DISPOSITION.json`. |
| Treat inherited tests as a minimum | Three source-derived cases add exact lifecycle order, failure identity, cancellation-token propagation and failed context-acquisition behavior. |
| Fix product behavior at its owner | The existing correct result-precedence behavior is retained; only incident-specific commentary and diagnostics were normalized. No test adapts around a product defect. |
| Mirror source ownership | The replacement is under `tests2/ViciOne.ServiceBus.Tests/Agents`, matching `src/ViciOne.ServiceBus/Agents`. |
| Prove test sensitivity | `MUTATION_VALIDATION.md` records eight independent one-cause product mutations and their exact failures. |

## Final commands and results

| Gate | Result |
|---|---|
| Core source-owner Release build | exit 0; 0 warnings; 0 errors |
| Focused restored C28 executable | 8 total; 8 passed; 0 failed; 0 skipped |
| Unfiltered Core source-owner executable | 754 total; 754 passed; 0 failed; 0 skipped |
| Unit solution Release build | exit 0; 0 warnings; 0 errors |
| Unfiltered UnitArchitecture MTP profile with predeclared floor 1358 | 1358 total; 1358 passed; 0 failed; 0 skipped |
| LocalIntegration Release build and profile | build 0 warnings/errors; 3 total; 3 passed; 0 failed; 0 skipped |
| Remaining inherited Core project Release build after deletion | exit 0; 0 warnings; 0 errors |
| Complete Engineering solution Release build | exit 0; 0 warnings; 0 errors |
| Bounded C28 `dotnet format ... whitespace --verify-no-changes` | exit 0 |
| Requirement/disposition JSON, reference closure and Git diff checks | exit 0 |

## Test-quality review

The full replacement file and connected product path were read. The suite has no skip, delay,
randomness, wall-clock, mock-framework or assertion-free case. It asserts exact execution count,
exception identity, failure observation, cleanup order, cancellation-token propagation and the
negative operation boundary. Eight independent mutations demonstrate that these assertions reject
meaningful regressions.

## Final disposition

PASS. C28 is technically complete in the current working tree. No commit or push is implied by this
report.
