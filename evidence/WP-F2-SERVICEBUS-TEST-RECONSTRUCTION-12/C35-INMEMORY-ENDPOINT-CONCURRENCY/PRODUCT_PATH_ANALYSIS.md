# C35 product-path analysis — InMemory endpoint concurrency

## Preserved capability

`OBL-R0-CORE-D-0465` requires a configured InMemory receive endpoint to admit one hundred
deliveries concurrently and to complete every delivery after release. The inherited fixture's
thread-pool wording is not the product contract; observable concurrent handler admission is.

## Complete relevant path

1. `EndpointConfiguration` stores endpoint `PrefetchCount` and `ConcurrentMessageLimit` in the
   transport configuration.
2. `InMemoryReceiveEndpointConfiguration` applies endpoint specifications and creates the receive
   context.
3. `TransportInMemoryReceiveEndpointContext` projects that immutable endpoint configuration to the
   transport boundary.
4. `InMemoryReceiveTransport.ReceiveTransportAgent` constructs `TaskExecutor` with
   `ConcurrentMessageLimit ?? PrefetchCount`.
5. The fabric queue has one dispatcher, while `TaskExecutor.Push` returns after admission; this lets
   the dispatcher continue admitting messages until the configured executor limit is occupied.
6. Each admitted handler remains in the consume pipeline until its observable release barrier opens.

The accepted `TaskExecutorTests` prove the executor in isolation and the middleware tests prove a
pipe filter. Neither proves the endpoint configuration reaches the InMemory transport. The new
tests therefore use a real harness input endpoint and real messages.

## A+ extension beyond the inherited test

The inherited case proves high concurrency but does not distinguish an upper bound from an
unbounded implementation. A second case configures prefetch four and concurrency three. Exactly
three handlers must enter while held; the fourth must remain queued until a slot is released. This
separates transport capacity from handler concurrency and protects the actual selection expression.

No product defect was found in this owner. No product source was permanently changed.
