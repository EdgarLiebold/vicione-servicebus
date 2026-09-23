# RabbitMQ endpoint query parser

## Change and behavior evidence

The URI constructor previously combined scheme resolution, host selection, and all query
option policies in one method. Its previous product-wide profile reported CRAP 67 even
with 98/99 measured lines covered. The query policies now live in small parsing steps
for lifetime, exchange type, Boolean flags, and entity names. Final values are assigned
once to the readonly address. The full-address host options remain accepted only on full
transport addresses; temporary/durable/autodelete conflicts, delayed-exchange precedence,
case-insensitive singleton options, ordered binding deduplication, and canonical short
rendering retain their existing behavior.

The unreachable `virtualHost ?? "/"` fallback was removed. The static
`ParseHostPathAndEntityName` helper assigns `hostPath` on every path and uses `"/"` for a
root or single-segment address.

## Product tests and adversarial review

The RabbitMQ source-owned suite now checks combined host and endpoint settings, both
unsupported full-address scheme variants, the reverse temporary/autodelete conflict,
representative duplicate guards for lifetime, exchange type, and entity name options,
and stable ordered deduplication of repeated exchange bindings. Assertions inspect
host values, endpoint topology, exact rendered addresses, exception types/messages,
and binding order; the cases distinguish realistic parser regressions.

The first read-only adversarial review found gaps for the reverse lifetime conflict,
duplicate guards outside the Boolean helper, and repeated binding deduplication.
Those cases were added with a matching requirement projection. Final read-only review
returned PASS, including the null-fallback simplification, with no concrete blocker.
The review did not execute tests.

## Verification and measurement

- Source/test commit: `c4b3faeda`.
- RabbitMQ Unit suite: 395/395 passed, zero failures and skips.
- Broker-backed RabbitMQ local integration: 31/31 passed, zero failures and skips.
- The RabbitMQ fixture findings list is empty for that broker run.
- Complete fresh-build Unit/Architecture gate: 10,211/10,211 passed, zero failures
  and skips.
- Focused report: `artifacts/coverage-a-plus-20260923-a087aaa64/rabbitmq-parser-final-v2.cobertura.xml`.
  SHA-256: `7713c2da3fc652a4ad63bf6db1111f024c6a7fc74163912f36cabf1a31360741`.
- The tracked `src`/`tests` diff is empty at `c4b3faeda` (empty SHA-256
  `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855`).

| Focused target | Lines | Reported branches | Complexity / CRAP |
| --- | ---: | ---: | ---: |
| URI constructor | 47/47 | 6/6 | 6 |
| `ParsedOptions.Apply` | 13/13 | 2/2 | 2 |
| `ApplyLifetime` | 15/15 | 2/2 | 2 |
| `ApplyExchangeType` | 5/5 | 2/2 | 2 |
| `ApplyBoolean` | 5/5 | 2/2 | 2 |
| `ApplyName` | 11/11 | 4/4 | 4 |

The Microsoft `code-testing-agent` skill was used before editing tests; `run-tests`
guided MTP execution, and `coverage-analysis`, `test-gap-analysis`, and
`assertion-quality` guided the risk, gap, and oracle checks. The focused report is not
a product-wide A+ measurement. The historical exact-commit profile is in
`product-wide-profile-c4b3faeda.md`; the current source/test state, including the
subsequent Azure Service Bus test correction, is in `product-wide-profile-90819758b.md`.
