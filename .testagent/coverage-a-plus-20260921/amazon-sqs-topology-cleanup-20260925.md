# Amazon SQS topology cleanup, 25.09.2026

## Product behavior and fix

Commit `df008b502` checks all four combinations of auto-delete topic and
queue flags. Repeated successful configuration registers one cleanup agent,
and stopping it deletes exactly the expiring entities using the stop token.
The earlier implementation registered a second agent when topology
declaration failed and retried in a new operation scope. The first attempted
fix kept one agent but could retain the stale client after reconnect.

The final implementation retains one agent per active filter lifecycle,
updates its client context on retry, and unwraps operation-scoped and shared
client contexts before retaining it. The context update and stop snapshot
use the same lock. Once the agent begins stopping, a subsequent lifecycle
may register a new agent. The test cancels and disposes a failing scope,
retries through a different parent and nested shared scope, proves deletion
through the successful parent, and verifies registration after restart.

## Test and adversarial review

Three red counterexamples were observed during development: direct retry
registered two agents; retry through a new `ScopeClientContext` still
registered two; retaining one agent across two different parents caused
deletion through the successful parent to be missed. These red runs were
observed but not archived. After the final fix, focused cleanup tests passed
5/5 and the complete SQS suite passed 279/279. Independent read-only Red
Team reviewed the scope, parent-context, stop-lock, and restart behavior and
returned PASS with no remaining concrete P1/P2 in this slice.

## Exact-commit receipt

`artifacts/coverage-receipt-amazonsqs-df008b502/receipt.json` verifies 279
tests, 11 unchanged binaries, and 1,423 tracked product sources at commit
`df008b502`. The partial aggregate is
`artifacts/coverage-sqs-df008b502.json`.

Within this **SQS unit receipt**, the SQS assembly measures 74.96% line and
78.31% branch coverage, with three methods above CRAP 30. The previous SQS
receipt at `5549a5b4b` measured 73.64% line, 76.98% branch, and four such
methods. `AnyAutoDelete` rose from 0/1 to 1/1 covered method lines and fell
from CRAP 42 to 6. The three remaining SQS risk members are
`ConnectionContextSupervisor.CreateSendTransportAsync`,
`AmazonSqsReceiveEndpointBuilder.ConnectConsumePipe`, and the async setup
path in `ConnectionContextFactory`. This is a subset measurement; a fresh
complete product-wide profile and the strict A+ gate remain open.
