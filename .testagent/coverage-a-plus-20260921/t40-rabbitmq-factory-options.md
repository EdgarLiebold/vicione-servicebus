# T40: RabbitMQ connection factory options

Base: `15e7a1c5c164f4eee46b6e56234bc78678b643b5`.
Focused Microsoft code-testing-agent and run-tests workflow, MTP/xUnit v3.
RabbitMQ.Client 7.2.2 factory/resolver APIs checked against installed SDK XML and
successful compilation. Public host configurator, settings, cluster resolver and
existing address/registration tests read fully. No product source changed.

## Requirements and evidence

All tests are in `RabbitMqConnectionFactoryOptionsTests`.

| Product contract | Test | Cases |
| --- | --- | --- |
| Optional message/frame limits are independent; unspecified limits preserve SDK defaults, explicit frame zero remains accepted | OptionalLimits_ReachClientIndependentlyAndPreserveUnsetDefaults | 5 |
| Certificate identity takes precedence over a supplied dynamic provider, which takes precedence over fixed credentials; factory construction never fetches credentials | Authentication_SelectsCertificateBeforeProviderAndProviderBeforeStaticCredentials | 2 |
| Actual resolver callback returns configured cluster resolver despite decoy SDK endpoints, selects A→B→A and preserves explicit/default ports and TLS | ClusterResolver_OverridesSdkCandidatesAndRetainsSequentialSelection | 1 |

The positive limit values differ from fresh SDK defaults and from each other.
Both properties are asserted in all four optional combinations. Frame zero alone
does not distinguish assignment from an already-zero default; nonzero cases do.
Authentication tests use a strict counted provider and inspect mechanism selection,
provider identity and client credentials. Resolver calls are actually invoked and
fully enumerated. These tests do not prove broker authentication, negotiated frame
limits, broker-side message rejection or network connectivity.

## Verification

- MAIN `artifacts/t40-focused.log`: 8/8, exit 0, no skips.
- MAIN `artifacts/t40-unit.log`: 482/482 including requirement projection, exit 0.
- MAIN `artifacts/t40-format.log`: verify-only formatting, exit 0.
- Read-only adversarial plan and implementation review: no blocker; frame-zero
  oracle limitation explicitly retained above.
- GATE `/private/tmp/servicebus-reply-investigation`, each deliberate mutation:
  - `artifacts/t40-mutant-size.log`: omitted message limit, 3 fail / 5 controls.
  - `artifacts/t40-mutant-frame.log`: omitted frame limit, 2 fail / 6 controls.
  - `artifacts/t40-mutant-provider.log`: ignored dynamic provider, 1 fail / 7 controls.
  - `artifacts/t40-mutant-resolver.log`: SDK resolver replaces configured cluster,
    1 fail / 7 controls.
- All mutations exit 2 and were manually restored. Product and test files compare
  byte-identically MAIN/GATE. `artifacts/t40-restored.log`: 482/482, exit 0, no skips.
- Final independent read-only review: no remaining blocker. Provider-mutant
  failure is proved by identity mismatch. xUnit's failing-value display invokes
  the probe's Name getter; its displayed Calls=1 is not evidence of eager product
  credential access. The passing tests explicitly check zero accesses.

Exact-commit full coverage/CRAP measurement, final audit and authorized push remain
pending. Global A+ remains unproven.
