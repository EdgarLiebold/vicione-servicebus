# Azure Service Bus entity options and zero idle-deletion contract slice

## Scope and product corrections

- A configured subscription now has its topic, subscription, routing, message lifetime,
  delivery, session, and metadata settings checked at the Azure SDK options boundary.
  Forwarding and session-required cases are separate valid configurations.
- The omitted-settings case checks the actual 366-day product message lifetime and Azure
  SDK defaults. An explicit null message lifetime and zero idle duration are distinct cases.
- A regression test exposed that all three configurators accepted zero idle deletion in
  `Validate()` but then passed zero to Azure SDK setters, which reject intervals below five
  minutes. Queue, topic, and subscription configurators now leave the SDK default unchanged
  for zero. A second adversarial review found the same bug in URI-derived send settings and
  `QueueSendSettings`; those paths now map zero to `TimeSpan.MaxValue`, the SDK's disabled
  idle-deletion value. Tests inspect the resulting broker declarations after URI roundtrips.
- No comments were changed by script. The 36-report product-wide baseline remains
  `ac363722c` pending a new full profile.

## Verification

- Focused Microsoft CodeCoverage: 4/4 passed, zero failures and skips. Cobertura:
  `artifacts/coverage-a-plus-20260923-ac363722c/asb-subscription-options-focused.cobertura.xml`,
  SHA-256 `9b5f1e95addb19279e698617d667625bb5e4549162ff88d5a2634f36f2568f4a`.
- Complete Azure Service Bus provider CodeCoverage: 229/229 passed, zero failures and skips.
  Cobertura: `artifacts/coverage-a-plus-20260923-ac363722c/asb-subscription-options-provider.cobertura.xml`,
  SHA-256 `077fc41e589f12d5d01f8de015f8338eb76d471e4815f9a9f2eed0a3fa407e87`.
- Complete final-byte Unit/Architecture solution: 10,196/10,196 passed, zero failures
  and skips.
- Adversarial read-only review: initial FAIL identified an invalid session-plus-forwarding
  test and a missing real default. The next FAIL identified the public URI/send zero paths.
  Both rounds were corrected. Final re-review: PASS.

## Targeted method result

| Method | Previous product-wide CRAP | Current provider lines | Reported branches | Current CRAP |
| --- | ---: | ---: | ---: | ---: |
| `ServiceBusSubscriptionConfigurator.GetCreateSubscriptionOptions` | 57.01 | 24/24 | 100% | 24 |
| `ServiceBusTopicConfigurator.GetCreateTopicOptions` | pending refreshed baseline | 22/22 | 100% | 22 |
| `ServiceBusQueueConfigurator.GetCreateQueueOptions` | pending refreshed baseline | 23/32 | 71.875% | above 30 |

The queue projection and product-wide aggregate still need further A+ work.
