# A+ remediation iteration 2 validation

Date: 2026-09-06

## Scope

This iteration resolves deep-review findings DR-009 and DR-010:

- both SQL providers now materialize non-null `Uri` values under one explicit behavioral contract; and
- Core temporary names and Azure Service Bus subscription names use one deterministic collision-resistant shortening policy.

## Contract decisions

The SQL write path accepts nullable URI values and writes `DBNull` for null. When a non-nullable URI is materialized, however, language null, `DBNull`, empty or whitespace text, malformed text, and non-string values are invalid data boundaries and fail explicitly. Both relative and absolute URI instances round-trip because the corresponding write path accepts both forms.

Long entity names are shortened with SHA-256 and a 13-character Base32 suffix. The 65-bit suffix replaces the former 30-bit suffix, uses the complete provider budget, retains a readable prefix, is stable for identical UTF-8 input, and differs for distinct inputs. A 10,000-name same-prefix set is collision-free in the acceptance test. Budgets smaller than one prefix character, one separator, and the suffix are rejected during topology construction.

## TDD and mutation evidence

Against the prior secured commit, the new SQL tests produced 10 failures out of 14 cases, the Core name tests produced three failures out of three cases, and the Azure topology class produced four failures while its six existing tests remained green. After implementation and completion of the related parameter matrix, the focused profiles passed 14/14, 3/3, and 11/11 respectively.

Five one-cause mutation groups were then killed and restored byte-for-byte:

1. PostgreSQL rejected relative URI values.
2. SQL Server rejected relative URI values.
3. The shared hash suffix was reduced from 13 to six characters.
4. The Azure public parameter guard was removed.
5. The Core minimum-length guard was removed.

## Repository validation

| Gate | Result |
|---|---|
| `ViciOne.ServiceBus.Tests.Unit.slnx` Release build, warnings as errors | PASS — 0 warnings, 0 errors |
| Complete Unit/Architecture profile | PASS — 3,751 passed, 0 failed, 0 skipped across 21 assemblies |
| `ViciOne.ServiceBus.Engineering.slnx` Release build, warnings as errors | PASS — 0 warnings, 0 errors |
| Engineering whitespace verification | PASS |
| Engineering style verification at warning severity | PASS |

An intermediate repeat of the full profile found one pre-existing race in `MessageObservationListTests`: its handler completion source could complete before the consume observer had published the same context into the snapshot list. The test was synchronized on the public observation signal, then passed ten isolated repetitions and the final full profile. The file's only bidirectional async-naming exception was corrected from `Empty` to `EmptyAsync` at the same time.

This is internal engineering evidence, not an independent external or Red Team acceptance.
