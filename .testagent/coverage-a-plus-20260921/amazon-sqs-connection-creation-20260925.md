# Amazon SQS connection creation, 25.09.2026

## Product behavior under test

Commit `1e737afcd` checks that connection creation uses the configured
provider connection, transfers it to the returned context handle, and frees
it exactly once when that handle stops. Provider failure remains the exact
inner cause of `AmazonSqsConnectionException`. Provider cancellation passes
through without wrapping. A stop transition between the retry wrapper and
the factory callback prevents the provider connection from opening.

## Tests and adversarial review

The focused cases passed 4/4; the complete Amazon SQS suite passed 292/292.
Read-only Red Team found two weak initial oracles. The stopping test had only
reached the retry wrapper's early cancellation check; it now controls the
two reads of the supervisor's stopping token and reaches the factory guard.
The success test had disposed the context directly; it now observes the
supervisor registration and releases the connection by stopping the returned
handle. The corrected tests passed focused reruns. Final read-only review
returned PASS with no concrete P1/P2 finding in this slice.

## Exact-commit receipt

`artifacts/coverage-receipt-amazonsqs-1e737afcd/receipt.json` verifies 292
tests, 11 unchanged binaries, and 1,423 tracked product sources at commit
`1e737afcd`. The partial aggregate is
`artifacts/coverage-sqs-1e737afcd.json`.

Within this **SQS unit receipt**, the SQS assembly measures 77.39% line and
79.28% branch coverage, with no SQS methods above CRAP 30. The async
connection-creation callback in `ConnectionContextFactory` rose from 0/11
to 11/11 covered lines and fell from CRAP 42 to 6. The callback's observed
branch coverage is 66.67%. This is a subset measurement; a fresh complete
product-wide profile and the strict A+ gate remain open. The provider-dispatch
gap identified in the preceding send-transport selection slice is also open.
