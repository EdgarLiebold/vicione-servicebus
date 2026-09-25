# Amazon SQS send transport provider dispatch, 25.09.2026

## Product behavior under test

Commit `0ced761dc` creates a send transport from a queue, relative topic, or
absolute `type=topic` address and sends a serialized application message
through it. The resulting client context must declare the matching entity and
invoke exactly SQS `SendMessageAsync` for queues or SNS `PublishAsync` for
topics. The destination name and provider request body are asserted.

## Tests and adversarial review

The focused variants passed 3/3; the complete Amazon SQS suite passed
295/295. A targeted counterexample temporarily replaced the Topic branch's
`TopicSendTransportContext` with `QueueSendTransportContext`: both topic
variants failed at the provider-call assertion, while the queue case passed.
The product source was then restored byte-for-byte (`git diff --exit-code`).
Read-only Red Team confirmed the previous routing P2 is closed, then found
an assertion-failure cleanup gap. The transport is now bound by `await using`
immediately after creation; the recording client also disposes every resolved
queue/topic info. Final Red Team review returned PASS with no concrete P1/P2.

## Exact-commit receipt

`artifacts/coverage-receipt-amazonsqs-0ced761dc/receipt.json` verifies 295
tests, 11 unchanged binaries, and 1,423 tracked product sources at commit
`0ced761dc`. The partial aggregate is
`artifacts/coverage-sqs-0ced761dc.json`.

Within this **SQS unit receipt**, the SQS assembly measures 78.23% line and
79.54% branch coverage, with zero SQS methods above CRAP 30. This is a
subset measurement; a fresh complete product-wide profile and the strict A+
gate remain open.
