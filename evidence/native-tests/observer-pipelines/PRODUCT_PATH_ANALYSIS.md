# Observer and message-flow product-path analysis

## Scope

The five inherited fixtures are migration evidence, not the specification. The analysis read their
complete contents and the complete connected product paths for connection management, send,
publish, receive, consume-observer, message-observer, mediator, and harness timeline behavior.

The resulting native tests are source-owned:

- `ViciOne.ServiceBus.Abstractions.Tests/Util/ConnectableTests.cs` owns connection lifecycle and
  callback fan-out;
- `ViciOne.ServiceBus.Tests/Observers` owns bus and endpoint observers;
- `ViciOne.ServiceBus.Tests/Mediator/Contexts` owns mediator send observers;
- `ViciOne.ServiceBus.Tests/Testing/DiagnosticOutputTests.cs` owns the harness timeline.

## Product paths and contracts

`Connectable<T>` owns a cached internal point-in-time connection snapshot. Public callers receive a
defensive array copy, while internal dispatch, count, and predicate operations reuse the internal
snapshot. Connect/disconnect handles are independent and idempotent. Asynchronous fan-out invokes
every connection from the same snapshot, converts synchronous callback failures and null returned
tasks into faulted tasks, awaits all started work, and exposes every failure through the returned
task. Synchronous iteration and predicates reject missing callbacks before inspecting the set.

Transport sends notify `PreSend` before serialization/dispatch, then exactly one of `PostSend` or
`SendFault`. A bus observer and an endpoint observer see the same context. Publish is intentionally
adapted through the send transport but is exposed only as `IPublishObserver`; it does not leak a
publish as a send observation. Request-client publication and response sends retain their distinct
observer categories.

Receive observation has three separate boundaries. `PreReceive` and `PostReceive` describe the
transport delivery. `PostConsume`/`ConsumeFault` describe an individual consumer or handler.
`ReceiveFault` describes a receive-pipeline failure outside the successful consumer. A failure that
is handled by the endpoint error pipe therefore produces `ReceiveFault` and then `PostReceive`; it
must not be mislabeled as `ConsumeFault`. Tests correlate callbacks by the exact `ReceiveContext`,
so parallel bus deliveries cannot contaminate a target assertion.

An `IObserver<ConsumeContext<T>>` is a consume branch, not a filter over sibling consumers. Its
configuration pipe runs before `OnNext`. An `OnNext` exception is passed unchanged to `OnError`, is
reported as that branch's consume fault, and does not suppress an independent handler branch.

Mediator sends use the same pre/post/fault contract. A request-response sequence is nested: request
pre, response pre/post, request post. The tests bind this order and the exact fault instance.

The message-flow test uses a deterministic fake clock and an explicit topology. It asserts every
publish, send, and consume count and every consume endpoint. It does not mutate the global endpoint
convention, wait for inactivity, depend on wall-clock duration, or infer success from a substring
that could belong to another operation.

## Inherited gaps closed

The inherited tests were largely one-callback awaits; many had no assertion. They did not prove
callback order, context/message/exception identity, absence of the mutually exclusive callback,
bus-versus-endpoint propagation, disconnect behavior, publish-versus-send isolation, handler versus
consumer naming, receive-versus-consume fault semantics, observer-branch isolation, mediator fault
behavior, or connection fan-out failure behavior. All of those are now executable contracts.

No meaningful feature is removed. The only product correction is the `Connectable<T>` hardening
described above; transport, observer, mediator, and timeline behavior is preserved and more exactly
specified.
