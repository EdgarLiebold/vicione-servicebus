# Azure Service Bus queue entity-path contract slice

## Scope and product behavior

- One attributed integration-style unit test uses real unstarted Azure SDK message and session
  processors. It requires `QueueClientContext.EntityPath` to reject access before initialization
  and then expose the path of the selected SDK processor.
- The injected SDK message and session paths are `sdk-message-queue` and `sdk-session-queue`.
  They differ from both the queue settings path `processor-input` and the input address path
  `different-input`. This distinguishes the SDK processor identity from either configuration
  source, without opening a network connection.
- Product source was unchanged. The 36-report product-wide baseline remains `ac363722c`.

## Verification

- Focused Microsoft CodeCoverage: 1/1 passed, zero failures and skips. Cobertura:
  `artifacts/coverage-a-plus-20260923-ac363722c/asb-entity-path-focused-final.cobertura.xml`,
  SHA-256 `b484c13ca5dd7aaaaa42d5bf6ef6c6da58e042cc4560be2cb181788d68137e20`.
- Complete current-byte Azure Service Bus provider CodeCoverage: 224/224 passed, zero failures
  and skips. Cobertura:
  `artifacts/coverage-a-plus-20260923-ac363722c/asb-entity-path-provider-final.cobertura.xml`,
  SHA-256 `0b0be75e4c7fc629da019a9ef989da8410e1f6900579c294087d043cf0697782`.
- Complete final-byte Unit/Architecture solution: 10,190/10,190 passed, zero failures and skips.
- Read-only adversarial review initially returned FAIL: the first test used the same path for
  settings and SDK processors, so a settings-based getter mutant survived. Distinct injected
  SDK paths closed that gap. The final review returned PASS.

## Focused method result

| Getter | Previous product-wide CRAP | Complete provider lines | Reported branches | Current CRAP |
| --- | ---: | ---: | ---: | ---: |
| `QueueClientContext.EntityPath` | 72 | 2/2 | 8/8 | 8 |

This is a targeted method result, not a refreshed product-wide aggregate. Global A+ remains open.
