# Reconcile all configured SNS subscription attributes

## Product behavior

The public topic and queue configurators accept Amazon SNS subscription
attribute dictionaries. On an existing subscription, the old comparison
handled only `FilterPolicy`, `FilterPolicyScope`, and `RawMessageDelivery`.
An explicitly changed `RedrivePolicy` could be skipped while the subscription
ARN and SQS permission were recorded as successful. A new test row was observed
failing before the fix: the path reached the forbidden SQS policy call without
sending `SetSubscriptionAttributesAsync`. This red-run console output was not
archived.

`ChangedSubscriptionAttributes` now compares every explicitly configured
attribute with the existing SNS value. It emits missing or changed values and
leaves matching values alone. The independent SNS subscription redrive setting
does not change the product's separate prohibition on SQS queue redrive policy.

`ExistingSubscriptionAttributeUpdateFailure_IsNotReportedAsSuccessAsync`
checks exact ARN, name, value, exception identity, and absent recorded ARN for
missing or changed filter policy, changed filter scope and raw delivery, and
changed redrive policy. `ExistingSubscription_ReconcilesChangedAttributesBeforeQueuePolicyAsync`
tests both no SNS updates for four matching attributes and two exact SNS
updates with unchanged filter scope and redrive policy. The SQS policy write
must follow all required SNS updates; successful setup records the ARN and
queue permission. The read-only adversarial review returned PASS after finding
and requiring this multiple-update case.

## Exact source/test commit and evidence

Commit `6a3689d33` is pushed to `origin/feature/servicebus-a-plus-api`.
The complete Release build passed with zero warnings/errors. The full
Unit/Architecture solution passed 10,226/10,226, zero failures/skips. The
Amazon SQS unit cohort passed 207/207 with Microsoft CodeCoverage, zero
failures/skips, and a parseable Cobertura report. All three green command
outputs are archived under ignored `artifacts/` paths.

| Artifact | SHA-256 |
| --- | --- |
| `artifacts/verification-sns-attributes-20260923/release-build-6a3689d33.log` | `95fa7f479281f09718d9d2dc43487d8c216aa5b2211be4586f9566f7413ea1ef` |
| `artifacts/verification-sns-attributes-20260923/unit-solution-6a3689d33.log` | `682e598fe90e15b08de55def64842347c8bf8b1857f23a2b4b245b5450867cc7` |
| `artifacts/coverage-sns-attributes-20260923-6a3689d33/test.log` | `fb1e5e2ebd96cea7ea12c3e90df058975aeee92da06c51728508a470312291cd` |
| `artifacts/coverage-sns-attributes-20260923-6a3689d33/coverage.cobertura.xml` | `f264956c1dcccb6222144a9b4025941dc3c9c7d41cc1208b3f95e05f3a72c302` |

In the targeted Amazon SQS unit report, the new comparison iterator has
all measured lines and branches covered at reported complexity 6, yielding
CRAP 6. `CreateQueueSubscriptionAsync` has 90.91% lines and 83.33% branch
observation at reported complexity 12. This is a targeted result. The last
complete 36-report product-wide profile still belongs to `5da15ce2e`; an
exact-commit product-wide profile and the A+ target remain open.
