# Amazon SQS move transport, 25.09.2026

## Product behavior and fix

Commit `5549a5b4b` adds a local check after move-specific headers are added
and before `ClientContext.SendMessageAsync`. Amazon SQS permits at most ten
message attributes per message ([AWS message quotas](https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/quotas-messages.html)).
A dead-letter move with nine custom attributes plus its reason is valid. Ten
custom attributes plus the required reason cannot be preserved in one SQS
message; the move now fails locally with the destination and count instead
of submitting an invalid provider request. It leaves the source attributes
intact. This limit remains a product constraint for that input.

## Tests and adversarial review

Provider-boundary tests capture body, FIFO identifiers, and string and binary
attribute values at the instant of send. They distinguish the admitted
receive-context body from the SDK message body, reject stale transport
headers, and require the new move header. A provider send failure must
redeclare destination topology on retry; a third successful move proves
the setup remains cached after success. The dead-letter limit test checks
the valid 9+1 and invalid 10+1 boundaries with the actual reason adapter.
The 10+1 test failed before the product fix because no exception was thrown;
this red run was observed but not archived. After the fix, the focused suite
passed 6/6 and the complete SQS suite passed 274/274. Independent read-only
Red Team found and had corrected three P2 test-oracle gaps: a mutable request
reference, identical context and SDK bodies, and no post-success cache check.
Its final review returned PASS with no open P1/P2 in this slice.

## Exact-commit receipt

`artifacts/coverage-receipt-amazonsqs-5549a5b4b/receipt.json` verifies 274
tests, 11 unchanged binaries, and 1,423 tracked product sources at commit
`5549a5b4b`. The partial aggregate is
`artifacts/coverage-sqs-5549a5b4b.json`.

Within this **SQS unit receipt**, the SQS assembly measures 73.64% line and
76.98% branch coverage, with four methods above CRAP 30. The previous SQS
receipt at `1ff3e6ee6` measured 72.97% line, 75.65% branch, and five such
methods. `SqsMoveTransport.MoveAsync` rose from 19/32 to 34/35 covered method
lines and fell from CRAP 33.16 to 18.01. The remaining SQS CRAP>30 members
are `CreateSendTransportAsync`, `AnyAutoDelete`, `ConnectConsumePipe`, and
the async setup path in `ConnectionContextFactory`. This is a subset
measurement; a fresh complete product-wide profile and the strict A+ gate
remain open.
