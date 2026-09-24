# Bus composition startup validation

## Baseline and change

The last complete 36-report product profile at exact source/test commit
`4e7b54c22` measured `BusCompositionStartupValidator<TBus>.StartAsync` at
110/115 covered lines, complexity 42, and CRAP 42.14. The uncovered duplicate
feature-owner branch was a real startup contract, so the new test registers
Payload Admission twice through public DI APIs and requires exactly one
configuration failure with the correct feature, cause, and remedy. Its
requirement variant is recorded in `CoreRequirements.json`.

After the behavior test passed, `StartAsync` was split at the existing feature
ownership and reliable messaging validation boundaries. The relative order of
transport, limits, feature, durable sender, and bus-resolution checks and all
failure messages are unchanged. No test was added merely to exercise a line.

The independent read-only adversarial review compared old and new control
flow and returned PASS. It confirmed the two registration owners are real,
the new test requires the static startup diagnostic, and requirement mapping
matches the test. Its only note was method-body indentation, now corrected.

## Focused verification and measurement

- Release startup-validator class: 10/10 passed before and after extraction.
- Complete current-byte Release Unit/Architecture solution: 10,263/10,263
  passed, zero failures and skips.
- Fresh Microsoft CodeCoverage report:
  `artifacts/coverage-a-plus-20260924-29b792236/bus-composition/coverage.cobertura.xml`.

| Target method | Covered lines | Complexity | CRAP | Observed branch rate |
| --- | ---: | ---: | ---: | ---: |
| `StartAsync` | 47/49 | 20 | 20.0272 | 75% |
| `AddFeatureOwnershipFailures` | 14/14 | 20 | 20 | 100% |
| `AddReliableMessagingFailures` | 54/55 | 2 | 2 | 100% |

The two missed `StartAsync` lines belong to the Endpoint QoS exception path;
the missed reliable-messaging line is the successful catalog materialization.
These values come from a focused class run, so they establish the target-method
result only. A new complete product-wide profile is required before changing
global coverage or CRAP totals. The A+ goal remains open.

The Microsoft `code-testing-agent`, `run-tests`, `coverage-analysis`,
`crap-score`, `test-gap-analysis`, and `assertion-quality` skill workflows guided
the test design, execution, and measurement.
