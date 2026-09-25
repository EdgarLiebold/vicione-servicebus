# Amazon SQS consumer topology connection, 25.09.2026

## Product behavior under test

Commit `1cdac5ea6` checks the three gates on an Amazon SQS consumer
subscription: the endpoint's topology setting, the connection option, and
the message topology's own consent. Each can suppress `Subscribe` without
preventing the consumer pipe from connecting once. The ordinary overload
without an options parameter must request topology configuration.

## Tests and adversarial review

The four explicit-option combinations and the ordinary-overload case passed
5/5; the complete SQS suite passed 284/284. Assertions distinguish every
gate, count message-topology lookups and subscriptions, and prove that the
original pipe and connect handle pass through unchanged. A first read-only
Red Team pass found that the ordinary overload was missing; its follow-up
returned PASS after that case was added, with no open P1/P2 in this slice.

## Exact-commit receipt

`artifacts/coverage-receipt-amazonsqs-1cdac5ea6/receipt.json` verifies 284
tests, 11 unchanged binaries, and 1,423 tracked product sources at commit
`1cdac5ea6`. The partial aggregate is
`artifacts/coverage-sqs-1cdac5ea6.json`.

Within this **SQS unit receipt**, the SQS assembly measures 75.11% line and
78.70% branch coverage, with two methods above CRAP 30. The previous SQS
receipt at `df008b502` measured 74.96% line, 78.31% branch, and three such
methods. `AmazonSqsReceiveEndpointBuilder.ConnectConsumePipe` rose from 0/5
to 5/5 covered method lines and fell from CRAP 42 to 6. The remaining SQS
risk members are `ConnectionContextSupervisor.CreateSendTransportAsync` and
the async setup path in `ConnectionContextFactory`. This is a subset
measurement; a fresh complete product-wide profile and the strict A+ gate
remain open.
