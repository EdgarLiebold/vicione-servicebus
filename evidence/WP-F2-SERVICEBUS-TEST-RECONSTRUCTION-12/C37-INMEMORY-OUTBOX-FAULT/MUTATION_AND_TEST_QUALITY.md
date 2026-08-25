# C37 InMemory Outbox Fault — Mutation and Test Quality

## Ordinary test

The replacement is an ordinary xUnit/MTP test. It has one passive requirement carrier,
`REQ-VSB-INMEMORY-OUTBOX-FAULT / discard-response-before-fault`, and no receipt, interceptor,
runtime coverage collector, wall-clock oracle, sleep or polling loop.

The subject uses a unique endpoint name and bounded waits only as failure containment. The request
fault, not the timeout, is the behavioral completion signal. The final response check reads the
current harness snapshot with an already-canceled observation token.

## One-cause product mutation

Only the exception branch of `InMemoryOutboxFilter` was changed from
`DiscardPendingActions()` to `ExecutePendingActions(_concurrentMessageDelivery)`. The focused test
failed at `Assert.Empty`: exactly one deferred `OutboxResponse` had escaped. This demonstrates that
the assertion distinguishes the required discard behavior from the opposite implementation.

## Restoration control

An initial narrow reverse edit matched the same execution call in the success branch and briefly
left success discarding while failure executed. The focused test stayed red and prevented a false
acceptance. Restoration was then performed against the complete try/catch structure.

The final product file SHA-256 is
`8c5ec3bdc809809357457ea6049b9e276bedda960e76cb12dd01d73fc0c24c33`, byte-identical to the recorded
baseline. A forced non-incremental Release build followed restoration before the focused test was
accepted. Future repeated-call-site mutations require the same three controls: restore the complete
structural region, prove byte identity, then rebuild without incremental reuse.

## Final subject hashes

- Native test: `6351fe98690e500f5deae6c5eeb9901e1585e01962b4859a31640e0bc1f7c4ee`
- Core projection: `f05bdf273c327db0c6aab325cd326b36ab372687d739c50b0e9d3aad09ab3927`
