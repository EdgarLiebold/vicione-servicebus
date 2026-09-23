# Azure Service Bus connection-string endpoint parser

## Product finding and correction

The public `ServiceBusHostConfigurator.ParseEndpoint` method indexed character
zero for an empty or null string. A connection-string segment with an empty key
also indexed beyond that key, while a whitespace-only key could pass silently.
The method now rejects null with `ArgumentNullException`, returns no endpoint
for empty input as its return contract states, and rejects empty or whitespace
keys with `FormatException`.

The first adversarial read-only review found that an otherwise valid Endpoint
caused an early return before a later malformed segment could be checked. Two
new test rows reproduced this failure (2/11 red). The parser now remembers the
first endpoint, validates every remaining segment, then returns the remembered
result. Its first-endpoint selection and invalid-URI-to-null behavior remain
unchanged. The final read-only review returned PASS with no concrete blocker.
Neither review ran tests.

## Behavioral test evidence

The source-owned xUnit/MTP tests distinguish these regressions:

| Product contract | Test and assertion |
| --- | --- |
| Empty text has no endpoint | `ParseEndpoint_EmptyConnectionStringHasNoEndpoint` asserts an exact null result. |
| Null is a caller error | `ParseEndpoint_RejectsANullConnectionString` asserts `ArgumentNullException` and parameter name `connectionString`. |
| Blank keys fail regardless of segment position | `ParseEndpoint_RejectsAnEmptyConnectionStringKey` checks five leading and trailing forms with `FormatException` and the exact message. |

The first focused run before the product correction had 5/9 failures, exposing
the index and blank-key faults. After the first correction it passed 9/9.
The adversarial trailing-key rows then failed 2/11 before the early-return
correction. The final focused run passed 11/11. The complete Azure Service Bus
Unit suite passed 238/238, zero failures and skips. The exact-commit Unit and
Architecture gate passed 10,218/10,218, zero failures and skips; the local
Azure Service Bus emulator suite passed 28/28 on its own fresh fixture.

The Microsoft `code-testing-agent` skill was applied before changing tests;
`run-tests` selected the xUnit/MTP commands, and `coverage-analysis` guided
the risk check. The new tests have exact result, exception, parameter, and
message oracles and do not exist solely to exercise unexecuted lines.

## Coverage and remaining risk

- Source/test commit: `17501736000f950b70c176cc815a7a70aa36f8bf`.
- Focused Azure Service Bus Unit report:
  `artifacts/coverage-a-plus-20260923-asb-endpoint-parser/azure-servicebus-unit-final.cobertura.xml`,
  SHA-256 `a747ab2b766279d30e9fc37d69750310eebf8a92dac48deb9850722dc952b31f`.
- The current full 36-report profile is in `product-wide-profile-175017360.md`.

| `ParseEndpoint` | Previous exact profile `90819758b` | Current exact profile `175017360` |
| --- | ---: | ---: |
| Measured lines | 28/37 | 35/40 |
| Complexity | 36 | 32 |
| CRAP | 54.65 | 34.00 |

The parser remains above the CRAP 30 work threshold. Its remaining untested
branches require real connection-string contracts and should not be filled
with assertions made only for coverage.
