# Existing SNS subscription attribute read

## Product finding and correction

The adversarial review found that `CreateQueueSubscriptionAsync` accepted a
non-success `GetSubscriptionAttributesAsync` response on an existing SNS
subscription. It skipped attribute reconciliation, recorded the subscription
ARN, and attempted to update the SQS queue policy. This could report stale
filter or raw-delivery settings as configured.

`ExistingSubscriptionAttributeReadFailure_DoesNotAcceptStaleSubscriptionAsync`
was observed failing against the old behavior during development: a returned
503 response reached the explicitly forbidden SQS `SetQueueAttributesAsync`
call. That red-run console output was not archived. The product method
now calls the existing `EnsureSuccessfulResponse()` immediately after the
attribute read. The test requires the exact provider transport exception,
the unsuccessful status, one attribute read, no later SNS/SQS operation, and
no recorded subscription ARN. The read-only Red Team re-review passed.

The existing update-failure regression is now a three-row theory for changed
`FilterPolicy`, `FilterPolicyScope`, and `RawMessageDelivery`. Each row checks
the exact subscription ARN, attribute name, desired value, original exception
identity, and absence of a recorded ARN. The tests do not merely invoke the
comparison helper.

## Validation at source/test commit `61f7e4705`

- Complete Release build: zero warnings, zero errors.
- Unit/Architecture solution: 10,222 passed, zero failed/skipped.
- Amazon SQS unit project with Microsoft CodeCoverage: 203 passed, zero
  failed/skipped; Cobertura XML parses successfully.
- The two local Task-returning test helpers were given `Async` names after an
  initial Architecture gate reported their naming violation. The subsequent
  exact-commit gate above passed.

The archived build, solution-test, and focused coverage-test logs have SHA-256
hashes `708b0e745837397c08e99882912a6594f1df2ca5f0a5a723989f1014795f71d9`,
`050c8bc3c38c141957ea0fefe03de04bbb19a801a6160d2da7381c9452c68ea3`,
and `e97c847e76e0112cd0a3d18174f40853d65f76187c29c6d5b982b48eb5e7c345`
respectively. The targeted Cobertura report hash is
`e00b3846da9f1b839900eddb3c3b3344f2bf3e2fbb3ec8b6006e442beac6773a`.
These ignored files are under `artifacts/verification-sns-subscription-20260923/`
and `artifacts/coverage-sns-subscription-20260923-61f7e4705/`.

The targeted report shows `SubscriptionAttributesEqual` moving from 6/11
to 10/11 lines and from 33.33% to 88.89% branch observation in the Amazon SQS
unit cohort. Its reported complexity remains 18; the CRAP formula therefore
changes from 48.43 to about 18.24 for this cohort. The complete 36-report
product-wide profile still belongs to `5da15ce2e`; this targeted report is
not a replacement for an exact-commit product-wide A+ measurement. The equal
existing-value and missing existing-attribute cases remain candidate product
contracts for the next SNS iteration.
