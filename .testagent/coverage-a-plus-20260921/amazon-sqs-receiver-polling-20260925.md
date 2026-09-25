# Amazon SQS receiver startup and polling, 25.09.2026

## Product behavior under test

Commits `bb29b9386` and `1871e3bf2` add a provider-facing test for the
actual `AmazonSqsMessageReceiver` lifecycle. The receiver must resolve its
queue before it becomes ready or issues a poll. A valid provider
`VisibilityTimeout` replaces the configured value; an equal, malformed or
missing value leaves the effective setting at 30 seconds. The first poll
must use the logical queue name, the configured seven-second wait and the
exact AWS maximum of ten messages under this fixture's prefetch and
concurrency settings. Stopping the receiver must cancel the token passed
to the outstanding provider poll.

## Test and adversarial review

`AmazonSqsReceiverPollingTests.Receiver_ResolvesQueueBeforePollingAndUsesValidProviderVisibilityAsync`
passed all four visibility variants. The assertions observe the real
provider lookup and poll boundary, the receiver's readiness, settings,
request arguments and stop token. The full SQS suite passed 261/261.
The first Red Team pass found a P2 test weakness: accepting any poll limit
from one to ten allowed a limit-one mutant. The final test proves the
fixture has capacity for ten and requires exactly ten. A final read-only
review reported PASS with no open P1/P2 in this slice.

## Exact-commit receipt

`artifacts/coverage-receipt-amazonsqs-1871e3bf2/receipt.json` verifies
261 tests, 11 unchanged binaries and 1,423 tracked product source files
on `1871e3bf2`. The repository aggregator accepted the single receipt in
partial mode at `artifacts/coverage-sqs-1871e3bf2.json`.

Within this **SQS unit receipt**, `ViciOne.ServiceBus.AmazonSqs` reports
71.85% line and 73.70% branch coverage, with eight methods above CRAP 30.
The previous SQS receipt on `1f1544dc9` reported 70.47% line, 72.85% branch
and ten methods above CRAP 30. The receiver's `ConsumeAsync` state machine
rose from 0/22 covered method lines and CRAP 156 to 15/22 and CRAP 16.64;
`GetQueueAttributesAsync` rose from 0/8 and CRAP 110 to 8/8 and CRAP 10.
This is a subset measurement, not a product-wide A+ result. The strict
product gate still needs same-commit unit, provider, no-AVX2 and scalar
receipts across the repository.
