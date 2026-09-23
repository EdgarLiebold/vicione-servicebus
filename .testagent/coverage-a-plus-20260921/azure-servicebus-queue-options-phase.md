# Azure Service Bus queue options projection slice

## Scope and behavior

- Two requirement-attributed tests project real `ServiceBusQueueConfigurator` settings to
  Azure SDK queue options. The configured test distinguishes path, expiry, duplicate
  detection, batching, dead-lettering, forwarding, lock, capacity, session, and metadata
  values. It uses separate Standard and Premium configurations because queue partitioning
  and a large maximum message size do not form a valid shared tier scenario.
- The default test checks the transport's durable-queue defaults and verifies that unset
  optional settings and blank forwarding paths do not overwrite Azure SDK defaults.
- The original `GetCreateQueueOptions` method had complexity 32, so complete coverage
  alone could only lower CRAP to 32. Its property assignments were preserved in order
  and split into three named groups. The focused tests check each group's configured and
  default behavior; product option values were not changed.
- The 36-report product-wide baseline remains `ac363722c` pending a new full profile.

## Verification

- Focused Microsoft CodeCoverage: 2/2 passed, zero failures and skips. Cobertura:
  `artifacts/coverage-a-plus-20260923-ac363722c/asb-queue-options-focused.cobertura.xml`,
  SHA-256 `e7d97bf75c8bbc3d32bb3c00d1e1a1b81379789a8f6523171bb0ce01a7dce319`.
- Complete Azure Service Bus provider CodeCoverage: 231/231 passed, zero failures and
  skips. Cobertura: `artifacts/coverage-a-plus-20260923-ac363722c/asb-queue-options-provider.cobertura.xml`,
  SHA-256 `5b1ce10f51386900d154faf8e6d215290551a137a3c5b0a524970319b4a23ee1`.
- Complete final-byte Unit/Architecture solution: 10,198/10,198 passed, zero failures
  and skips.
- Adversarial read-only review first rejected a combined Standard/Premium option set.
  After splitting the configurations, its re-review returned PASS with no remaining
  concrete behavior mutant in this slice.

## Targeted method result

| Method | Current-byte CRAP before split | Provider lines | Reported branches | Current CRAP |
| --- | ---: | ---: | ---: | ---: |
| `GetCreateQueueOptions` | 54.78 | 5/5 | 100% | 1 |
| `ApplyLifetimeOptions` | — | 11/11 | 100% | 12 |
| `ApplyRoutingAndDeliveryOptions` | — | 11/11 | 100% | 10 |
| `ApplyCapacityAndIdentityOptions` | — | 11/11 | 100% | 10 |

The previous product-wide value and the full product-wide aggregate still require a
fresh consolidated profile. Global A+ remains open.
