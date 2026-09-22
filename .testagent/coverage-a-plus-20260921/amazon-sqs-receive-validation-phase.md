# Amazon SQS receive-endpoint validation

## Product defects and regression oracles

`QueueReceiveSettings` exposes mutable values that can bypass the endpoint
configurator's bounds. Before the correction, direct `VisibilityTimeout = -1`
passed endpoint validation, while `43201` produced a misleading
`MaxVisibilityTimeout` failure. Direct `MaxVisibilityTimeout = 13 hours` also
passed. Direct `MaxVisibilityTimeoutRenewal = 0`, `1`, or `59` passed even though
the configurator raises such values to the 60-second floor. A zero renewal can
stop renewal, and a one-second renewal can create excessive provider traffic.
The new tests reproduced all seven invalid cases before the production fix.

Endpoint validation now checks the AWS visibility range, positive maximum
duration at most 12 hours, and the effective renewal floor under each setting's
own diagnostic key. The public `ReceiveSettings` comment now distinguishes
configurator clamping from direct mutation. A separate regression test checks
the original diagnostic order in a mixed failure/warning case and verifies
that an already obtained validation enumerable reads mutable settings when it
is enumerated. Raw SNS delivery tests include a Boolean object: converting it
to text would make it parseable, so the test protects the string-only boundary.
The public changelog records the corrected behavior.

## Current-source validation

- The Release SQS test-project build had zero warnings and errors. The final
  complete SQS Unit run passed 200/200 with Microsoft CodeCoverage and
  `tools/ci/coverage.settings.xml`. Its Cobertura report SHA-256 is
  `01dd3bfcfc151db892db195357df9a0b3c71aed9f3904ae874a81897a4c5e1f0`.
- The full Unit/Architecture gate passed 10,018/10,018, zero failures and
  skips. Its log SHA-256 is
  `d21260070d6f7c8fb2cb26ac85fa5e8c437ecf1fa4c2de9ecadcddcb8338be8f`.
- The old generated `Validate` body had 24/36 covered lines and CRAP about
  124.37 in the preceding focused SQS report. The new body and four cohesive
  validation helpers are different method bodies, so those coverage fractions
  are not a controlled before/after percentage comparison. In the final
  report, generated `Validate` is 12/12 lines, complexity/CRAP 12;
  `ValidateReceiveLimits` 11/11 and CRAP 18; `ValidateQueueSettings` 4/5 and
  CRAP 4.128; `ValidateVisibilitySettings` 16/16 and CRAP 22; and
  `ValidateSubscriptionSettings` 12/12 and CRAP 14. Every new validation
  method is below CRAP 30. The one uncovered queue-name failure branch is
  separate from the newly corrected visibility behavior.
- The report and gate log are in
  `artifacts/coverage-a-plus-20260922-4488b29fe/sqs-validation-phase/`.
  This is a focused profile, not a new product-wide A+ measurement.

## Adversarial review

The independent read-only review found the three direct-mutation gaps and
identified a weak non-string test value. After the corrections it returned
PASS: no further concrete defect in the product/test diff. It separately
checked boundaries, the order and deferred enumeration contract, all four
helper state machines, the Boolean-object type oracle, and the Requirements
projection. The final tests and coverage run followed that review.

## Exact source/test commit

Pending commit and isolated exact-commit verification.
