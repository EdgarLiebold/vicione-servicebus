# Amazon SQS queue-policy permission review, 25.09.2026

## Product defect and correction

Commits `f99fc6622` and `1f1544dc9` correct `QueueInfo.UpdatePolicyAsync`.
The old precheck treated any statement with a matching queue resource and
SourceArn as permission, even when its effect, action or principal could not
allow SNS to send. It could also add a topic to an existing Allow carrying
unrelated conditions. That changed permissions without granting the intended
topic access.

The precheck now recognizes only a matching SNS SendMessage Allow. Existing
unrestricted grants remain untouched. New grants stay separate from foreign
conditions. An existing dedicated ArnLike or ArnEquals grant can safely add
another exact topic value without consuming another policy statement. A
potentially matching explicit Deny produces a diagnostic before any AWS write;
the Deny check covers action/resource/SourceArn wildcards and a raw JSON
`Principal: "*"`. An ArnNotEquals condition excluding the target topic does
not block its grant. The [IAM evaluation rules](https://docs.aws.amazon.com/IAM/latest/UserGuide/reference_policies_evaluation-logic_policy-eval-denyallow.html)
give explicit Deny precedence over Allow; [SQS policy quotas](https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/quotas-policies.html)
limit statement count, motivating safe reuse.

## Behavioral tests and adversarial review

- Sixteen `QueuePolicyPermissionTests` executions cover unrelated Allow/Deny
  statements, exact and wildcard Deny, negative SourceArn conditions, raw
  wildcard principal JSON, unrestricted grants, additional conditions,
  ArnEquals idempotence, and multi-topic ArnLike/ArnEquals reuse.
- The initial focused run failed 3/5 tests on the old product code. The
  raw wildcard-principal counterexample also failed before its fix: it
  attempted `SetQueueAttributesAsync` despite an effective Deny.
- Tests assert exact new statement effect, action, resource, service principal,
  condition operator and ARN values; rejected paths forbid broker writes.
  The full SQS suite passed 257/257. Final adversarial read-only review
  reported PASS with no remaining concrete P1/P2 in this scope.

## Exact-commit receipt

`artifacts/coverage-receipt-amazonsqs-1f1544dc9/receipt.json` verifies
257 tests, 11 unchanged binaries, and 1,423 tracked product source files
on `1f1544dc9`. The repository aggregator accepted the single receipt in
partial mode at `artifacts/coverage-sqs-1f1544dc9.json`.

Within this **SQS unit receipt**, `ViciOne.ServiceBus.AmazonSqs` reports
70.47% line and 72.85% branch coverage, with 10 methods above CRAP 30.
The preceding receipt on `0487b4f0b` reported 69.59% line, 70.55% branch
and 12 methods above CRAP 30. This is a subset measurement, not a
product-wide A+ result. The strict product gate still needs same-commit
unit, provider, no-AVX2 and scalar receipts across the repository.
