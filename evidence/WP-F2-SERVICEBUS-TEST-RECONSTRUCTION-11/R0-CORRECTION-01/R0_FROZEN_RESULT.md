# R0 — frozen research result, corrected

Correction of the R0 checkpoint under Lead directive `DIR-A0071-R0-CORRECTION-01`. The research is
retained and not repeated. The superseded artefacts under `R0/` keep their bytes and their bound
hashes; every corrected artefact lives here at a new path, as the immutability rule requires.

Baseline commit `ae73c6da748e3bc3257dffa4971ee8680e086207`, tree `e5897e7632be4f491e01d51221ee59081d4d2aa0`.

## Census, stated as separate values

| Value | Count |
|---|---:|
| physical manifest lines | 3377 |
| header lines | 1 |
| file-read records | 3376 |
| distinct paths | 3329 |
| intentional cross-cohort reread records | 47 |
| unknown-shape lines | 0 |
| paths outside the tracked baseline | 0 |
| hash mismatches | 0 |
| missing mandatory paths | 0 |

The earlier report stated the 3377 physical lines as "files read". That was wrong and is
replaced by the values above. Raw cohort findings are not rewritten; the reconciler handles the one
header explicitly and rejects unknown headers and columns.

## The ledger

| Row kind | Rows |
|---|---:|
| `OBLIGATION` | 3358 |
| `INFRASTRUCTURE_FILE` | 149 |
| `ASSURANCE_PROMISE` | 116 |
| `SHARED_CAPABILITY` | 41 |

2869 obligations are anchor-backed and 489 newly derived; the counts stay separate so the bound
3114 remains checkable. Every row carries `dispositionState`; **no R0 row is terminal**.

## The frozen obligation set

3663 entries of `obligationId` plus `variantKey`, 0 duplicates, no empty field, no trailing
whitespace, across 41 executable target projects each in exactly one profile.

| Profile | Entries |
|---|---:|
| `UnitArchitecture` | 2762 |
| `LocalIntegration` | 773 |
| `External` | 128 |

## Corrected artefacts

| Artefact | SHA-256 |
|---|---|
| `R0-CORRECTION-01/SOURCE_OWNER_MAP.json` | `add7806336e81d8f941c836144f481c287fa70922554a774dd5db5fb80a0c24f` |
| `R0-CORRECTION-01/COMBINED_SEMANTIC_LEDGER.jsonl` | `f738444459328ba8d8a9be1eb82b6c8decba2c2de51e777d8105357f366952c1` |
| `R0-CORRECTION-01/OBLIGATION_SET_FROZEN.tsv` | `684dc7ea1bc50557f9f1db32baa4430cac0f02f569a5d477003ac6bfe75f10a7` |
| `R0-CORRECTION-01/CENSUS_RECONCILIATION.json` | `9be77861131a4d0be2e41aef8917549a2dbfc110fa0805decba3fe6bc7876c31` |
| `R0-CORRECTION-01/PROFILE_PROJECT_REALIGNMENT.json` | `c11fe0cee4abf7585159c912c3390e5ce7f72bcdc4fcf629d1213131c987b4ee` |
| `R0-CORRECTION-01/LEAD_DISPOSITIONS_APPLIED.md` | `f29c423b7a93e9ae2cb57a7c73965f8cd9977081132bafeb083692e5b36a36b8` |
| `.testagent/plan.md` | `b1a25a42b4975e41dbbc8978056adcbca23181452944a45fb72c6e9a25710aef` |
| `.testagent/research.md` | `8604fa7711ce89d652394526047884a3cb1bdd66cfc4ebfa2c83e031a7cdbfd6` |
| `.testagent/status.md` | `ecff06a0c7e526d0470ad370ec4edf842366fa13e05036602e6617d05882fa4f` |

## Unchanged and still bound at their original path

`R0/BASELINE_TRACKED_FILE_MANIFEST.tsv`, `R0/ANCHOR_IDENTITY_TO_OBLIGATION.tsv`, the four probe
documents, both security dispositions, `R0/PO_DECISIONS_R0.md`, `R0/INTEGRATOR_DECISIONS.md`,
`R0/COHORT_READING_RULES.md`, `R0/FIND_UNTESTED_SOURCES.md`, `R0/SKILL_BINDING.md`,
`R0/GIT_BASELINE_STATE.json` and all twelve cohort directories.
