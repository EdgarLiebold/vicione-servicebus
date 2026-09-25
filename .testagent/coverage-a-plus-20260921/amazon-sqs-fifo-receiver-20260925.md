# Amazon SQS FIFO receiver, 25.09.2026

## Product behavior under test

Commit `1ff3e6ee6` adds provider-facing tests for the actual FIFO receiver.
One group dispatches numeric sequence numbers in order, including a value
beyond `ulong.MaxValue`. Interleaved groups each retain their own sequence
order. A blocked dispatch in one Murmur3 executor partition does not prevent
another partition from dispatching. Missing or invalid group and sequence
attributes stop the receiver before dispatch and log the corresponding
`InvalidDataException`.

## Verification and adversarial review

The focused FIFO tests passed 8/8, and the complete Amazon SQS suite passed
269/269 after the Red Team correction. The first Red Team pass found that the
interleaved-group test alone did not establish independent progress. A new
blocking test now proves the other group completes before the blocked group
is released. A second pass found a failure-path teardown hang; both gates
are released in `finally`, and queue disposal is protected by `finally`.
The final read-only review found no false positive in the passing path.

## Exact-commit receipt

`artifacts/coverage-receipt-amazonsqs-1ff3e6ee6/receipt.json` verifies 269
tests, 11 unchanged binaries, and 1,423 tracked product sources at commit
`1ff3e6ee6`. The repository aggregator accepted its single receipt in
partial mode at `artifacts/coverage-sqs-1ff3e6ee6.json`.

Within this **SQS unit receipt**, the SQS assembly measures 72.97% line and
75.65% branch coverage, with five methods above CRAP 30. The preceding
receipt at `1871e3bf2` measured 71.85% line, 73.70% branch, and eight such
methods. `GetMessageGroupId` and `GetSequenceNumber` each reached 5/5 covered
lines and CRAP 6. This is a subset measurement; a new complete product-wide
profile and the strict A+ gate remain open.
