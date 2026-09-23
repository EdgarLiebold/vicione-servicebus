# RabbitMQ topology diagnostics contract slice

## Scope and behavior

- One attributed test constructs two exchanges, two exchange-to-exchange bindings with
  opposite directions and different routing keys, plus a queue and queue binding through
  the real `BrokerTopologyBuilder`. It invokes `LogResult` through the public logging context.
- The captured structured logs must contain exactly two exchange declarations and two
  exchange bindings, with the correct templates, information level, names, types, flags,
  endpoints, and routing keys. The queue declarations and bindings are outside this method's
  documented diagnostic contract and cannot generate extra records. The test also exercises
  an absent logging context and restores the previous `AsyncLocal` context in `finally`.
- Product source was unchanged. The 36-report product-wide baseline remains `ac363722c`.

## Verification

- Focused Microsoft CodeCoverage: 1/1 passed, zero failures and skips. Cobertura:
  `artifacts/coverage-a-plus-20260923-ac363722c/rabbitmq-topology-logging-focused.cobertura.xml`,
  SHA-256 `017c02f2990894c2699987d22d5f31c34569809928d9a1d0745e1a6028dc1545`.
- Complete RabbitMQ provider CodeCoverage: 387/387 passed, zero failures and skips. Cobertura:
  `artifacts/coverage-a-plus-20260923-ac363722c/rabbitmq-topology-logging-provider.cobertura.xml`,
  SHA-256 `7d8563ed88440fd97e8126d672a0647be457beee7d696aeb16cdfadad9eac747`.
- Complete final-byte Unit/Architecture solution: 10,192/10,192 passed, zero failures
  and skips.
- Adversarial read-only review initially found that a single binding could not detect an
  implementation that logs only the first binding. After the distinct second binding and
  exact structured triple assertions were added, its re-review returned PASS.

## Focused method result

| Method | Previous product-wide CRAP | Complete provider lines | Reported branches | Current CRAP |
| --- | ---: | ---: | ---: | ---: |
| `TopologyLayoutExtensions.LogResult` | 72 | 7/7 | 100% | 8 |

This targeted result does not replace a refreshed product-wide aggregate. Global A+ remains open.
