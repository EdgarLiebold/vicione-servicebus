# Azure Service Bus topology diagnostics contract slice

## Scope and behavior

- One attributed test constructs two topics and two consumer subscriptions using the real
  `BrokerTopologyBuilder`, then invokes `LogResult` through the public logging context.
- The logger captures structured entries. The test requires exactly one Information entry per
  topic and subscription, the correct message templates and fields, and the exact subscription
  to topic pairs. It also exercises an absent logging context and restores the previous
  `AsyncLocal` context in `finally`.
- Product source was unchanged. The 36-report product-wide baseline remains `ac363722c`.

## Verification

- Focused Microsoft CodeCoverage: 1/1 passed, zero failures and skips. Cobertura:
  `artifacts/coverage-a-plus-20260923-ac363722c/asb-topology-logging-focused.cobertura.xml`,
  SHA-256 `12911fa4f892fe54d945a688bba6f264e609cb86b309b747f04abc921fd522f3`.
- Complete current-byte Azure Service Bus provider CodeCoverage: 225/225 passed, zero failures
  and skips. Cobertura:
  `artifacts/coverage-a-plus-20260923-ac363722c/asb-topology-logging-provider.cobertura.xml`,
  SHA-256 `c05c0abe8500e441fdce7f5d5a02546c8474c81320cd27f372329318dda1b393`.
- Complete final-byte Unit/Architecture solution: 10,191/10,191 passed, zero failures and skips.
- Adversarial read-only review: PASS. It found no concrete surviving behavior mutant for missing,
  duplicated, misassigned, or unstructured topic/subscription logging.

## Focused method result

| Method | Previous product-wide CRAP | Complete provider lines | Reported branches | Current CRAP |
| --- | ---: | ---: | ---: | ---: |
| `ServiceBusBrokerTopologyExtensions.LogResult` | 72 | 6/6 | 100% | 8 |

This targeted result does not replace a refreshed product-wide aggregate. Global A+ remains open.
