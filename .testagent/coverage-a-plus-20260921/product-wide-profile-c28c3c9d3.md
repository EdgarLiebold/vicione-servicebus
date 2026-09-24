# Cumulative product coverage profile at c28c3c9d3

## Exact-commit evidence

- Product/test commit: `c28c3c9d384f9202f1df3e9a3d9eb73f924c6c5e`.
  Against `6d131442a`, no `src` file changed. Two Core tests exercise the
  value-initializer and typed-pipe overloads of both recurring schedulers.
  The test, requirement mapping, and changelog were committed together.
- The serial Release Unit/Architecture build completed with zero warnings and
  errors (`build.log` SHA-256
  `6e2ab5b0621534efbd1536b8a6c55991f313de666fde3460759ea7d5a79c5fd1`).
  The Core test DLL and all nine product DLLs in its fresh report embed the
  full product/test revision.
- The serial Unit/Architecture gate passed **10,376/10,376** with zero
  failures and skips (`gate.log` SHA-256
  `e9b098a537f087da8401d44b60bd39d148b15e4b0ac4c0f4bfb6419ee2f0e32e`).
- The fresh Core Microsoft CodeCoverage run passed **6,402/6,402** with zero
  failures and skips (`core-coverage.log` SHA-256
  `28613fc8e95f9801761bd0797aa33dd27b5ce8466a83b1ba7d4843bf29057203`;
  Cobertura SHA-256
  `ae305fd81105fc05aeb4e770932c739a8e2d895de1255c04b8986eb59ec4818a`).
- Read-only adversarial Red Team reviewed both new scheduler tests, mapping,
  and changelog. It found a surviving handle mutant in the typed-pipe path.
  The test was strengthened to assert exact payload, schedule, and destination;
  the changelog wording was corrected. Red Team re-review returned **PASS**.

## Cumulative result

| Measure | `c28c3c9d3` | Previous `6d131442a` |
| --- | ---: | ---: |
| Line coverage | 84,169 / 93,509 = 90.0117% | 84,124 / 93,509 = 89.9635% |
| Conservative branch observation | 30,192 / 36,670 = 82.3343% | 30,182 / 36,670 = 82.3071% |
| Methods with CRAP > 30 | 25 / 25,995 | 25 / 25,995 |

For the two recurring scheduler source files, the merged method-line
observations rise from 88/216 to 110/216 (publish) and 111/235 to 134/235
(endpoint). The new tests verify initialized payload fields, command type and
destination, schedule identity, cancellation, exact typed handles, and the
deferred typed pipe's payload and correlation propagation through each adapter.

## Merge and limits

`artifacts/coverage-a-plus-20260924-c28c3c9d3/raw/` contains 49 parseable
reports for 32 product assemblies: 48 byte-verified inherited reports and
one fresh Core report. Twelve broker fixture records are inherited; no broker
fixture ran in this iteration. All product source files are unchanged from
the previous profile. The JobSaga, Serialization, Retry, RequestRate, and QoS
source overlays remain in force; their stale-report counts are respectively
16, 41, 41, 45, and 41. Cobertura has no stable branch identities, so the
conservative result takes the largest covered count per branch location. The
capped-sum estimate is not used as the quality gate.

The merge is recorded in `analysis-49/summary.json` (SHA-256
`c7f6d6cc85eb55a0d7ff7645e38cbd881c9e96e795f3cbfc2b1924c7ecd4cac7`),
`analysis-49/methods.json` (`c65c9563349112da0133438d3d78372c5db0e0c156005e4a3e8570a661a3b802`),
`provenance.json` (`59ee7cf29bd941c027160f888861d0d28688ca5989f6086a361387d83b52e40b`),
and `overlay-policy.json` (`6f75c913f04299589895cfbf6d5edf2cdafacadd84d3b15156d3968c712aed8d`).

The Microsoft `code-testing-agent`, `coverage-analysis`, and `run-tests`
skills informed test design, commands, and measurement. Global A+ remains
open: branch coverage is below A+, and 25 methods remain above CRAP 30.
