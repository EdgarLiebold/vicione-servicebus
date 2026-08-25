# C37 InMemory Outbox Fault — Product Path Analysis

## Preserved feature

An in-memory outbox may defer sends, publishes and responses while a consumer is running. It may
release those actions only after successful completion. If the handler faults, every deferred
action must be discarded before the normal consume-fault pipeline publishes `Fault<T>`.

## Source owner

The complete relevant path is:

1. `InMemoryOutboxSpecification` installs the filter.
2. `InMemoryOutboxFilter<TContext, TResult>` creates the outbox consume context.
3. `InMemoryOutboxConsumeContext` routes outbound operations into the deferred outbox endpoint.
4. The filter executes pending actions only after `next.Send` succeeds.
5. Its exception branch discards pending actions and rethrows.
6. The surrounding consume pipeline publishes the request fault.

The product owner is unchanged in the accepted candidate. Its SHA-256 is
`8c5ec3bdc809809357457ea6049b9e276bedda960e76cb12dd01d73fc0c24c33`.

## Replacement boundary

`InMemoryOutboxFaultTests.HandlerFault_DiscardsItsDeferredResponseBeforePublishingTheRequestFault`
uses the real harness endpoint and `UseInMemoryOutbox`. Its handler awaits `RespondAsync` before it
throws. Receiving `RequestFaultException` is the causal completion barrier: the outbox exception
branch and fault publication have completed. The test then proves the exact sent
`Fault<OutboxRequest>` and inspects the already-observed send snapshot for absence of the deferred
response.

This is stronger and faster than the inherited fixture's 300-ms negative observation window. It
does not infer correctness from elapsed time.
