# Circuit-breaker runtime-settings validation

## Baseline and defect

The last complete 36-report profile at exact source/test commit `f7d924f94`
measured compiler-generated `CircuitBreakerSettings.Validate.MoveNext` at
8/15 lines, 14/24 reported branches, complexity 24, and CRAP 82.54. The
public `CircuitBreakerOptions.SetBreakDurations` contract already rejected a
decreasing recovery-delay sequence, but the defensive runtime-settings
validator accepted one. An internal caller that bypassed the public options
could therefore construct a circuit breaker whose later recovery probes used
shorter delays than earlier probes.

Exact source/test commit `c13ebf62e` makes runtime validation reject a
decreasing sequence after checking presence and positivity. Equal adjacent
durations remain valid, matching the public options contract. The validator
continues to aggregate independent failures in the established order.

## Hard-test process

The initial fail-first run executed ten cases against the unchanged product.
Nine passed and `Validate_RejectsDescendingBreakDurations` failed because the
validator returned no error. After the product correction, independent
read-only adversarial review found three surviving semantic mutant families:

- negative throughput and sampling-duration values;
- moving the new ordering diagnostic within the aggregate result;
- checking only the first and last duration rather than the whole sequence.

The tests were hardened with negative values, exact aggregate position, and an
inner inversion. A second review found the exact zero-boundary mutants for
throughput and sampling duration. Dedicated theories now test both zero and a
negative value with exact key, message, and failure disposition. Final
adversarial review returned PASS with no concrete remaining product or test
gap.

The final eight attributed test methods execute fourteen cases. They cover:

- all six independent validation failures and their order;
- accepted ratio limits, positive time limits, and equal durations;
- zero and negative throughput and sampling-duration boundaries;
- NaN, both infinities, and values immediately outside the ratio interval;
- null, empty, zero, negative, equal, and internally decreasing duration
  sequences.

The eight requirement variants and eight test attributes have exact set
equality and unique `(requirementId, variantKey)` pairs. The tests use exact
observable outcomes without timing, shared state, branch-touching assertions,
or duplicated production logic. Final adversarial review found no test
antipattern and accepted the suite as A-grade product tests.

## Verification and measurement

- Final focused Microsoft Testing Platform run: 14/14 passed, zero failures
  and skips.
- Final Core run: 6,343/6,343 passed, zero failures and skips.
- Final complete Unit/Architecture gate: 10,110/10,110 passed, zero failures
  and skips.
- Current-byte Release Core-test build: zero warnings and zero errors. The
  preceding complete Unit/Architecture solution build also completed with zero
  warnings and zero errors; the final added tests changed only the rebuilt
  Core test project and its embedded requirements resource.
- Current-byte focused report:
  `artifacts/coverage-circuit-breaker-settings-20260922/final.cobertura.xml`,
  SHA-256 `094a88c88b22187c635829a47907ae898139d6fec34d42ece661c70182299b1a`.
- A clean detached checkout of exact commit `c13ebf62e` passed locked restore,
  a zero-warning Release Core-test build, 14/14 focused tests, and 6,343/6,343
  Core tests. Exact report:
  `artifacts/coverage-circuit-breaker-settings-20260922/exact-c13ebf62e.cobertura.xml`,
  SHA-256 `7c6e2a7838ba4833d6d2056bc12b6052af0072075ab003148e267ed3568a84a1`.

| Exact-commit target method | Lines | Reported branches | Complexity / CRAP |
| --- | ---: | ---: | ---: |
| constructor | 7/7 | 100% | 1 |
| positivity predicate | 1/1 | 100% | 1 |
| `Validate.MoveNext` | 17/17 | 100% | 26 |

The mandatory Microsoft `code-testing-agent`, `run-tests`,
`coverage-analysis`, `crap-score`, `test-gap-analysis`, `assertion-quality`,
and `grade-tests` workflows governed the test design, execution, measurement,
mutation-style review, assertion review, and quality gate. This focused result
does not replace the product-wide profile; global A+ remains open.
