# In-memory delay and scheduled-publish product-path analysis

## Scope read before change

The complete path was read from `IInMemoryDelayProvider` through `InMemoryDelayProvider`,
`InMemoryBusInstance`, host/fabric ownership, dependency-injection registration,
`MessageQueue.DeliverWithDelay`, the real scheduler path and both inherited fixtures. All product
call sites and the already accepted minimal-envelope redelivery carrier were included. Git history
shows that the implementation was imported unchanged from MassTransit v8.5.10; the ViciOne rename
added no behavioral rationale.

## Inherited design and defects

The inherited provider used one unbounded channel, one permanently running reader task, a mutable
wall-clock offset and a fresh cancellation timer for every wait for the next deadline. Equal
deadlines were forced into a `SortedList` with a comparer that returned `1` in both comparison
directions and therefore violated the comparer contract. `Advance` completed when its command was
written, not when logical time had actually advanced. A canceled far-future delay remained in the
sorted list until its deadline, and disposal/cancellation behavior depended on channel timing. The
four direct tests used real elapsed-time windows and did not test ordering, cancellation, disposal
or resources completely.

The scheduled-publish path additionally requires delay registration to happen before `Deliver`
returns. A previously removed `Task.Run` boundary violated that rule and allowed `Advance` to win
the race. The final implementation retains direct asynchronous invocation; a compiled-IL assurance
test makes this structural prerequisite deterministic rather than relying on scheduler luck.

## A+ result

- The standard .NET `TimeProvider` is injected; production defaults to `TimeProvider.System`.
- One provider owns exactly one `ITimer`; no background task, channel or timer-per-delay remains.
- A `SortedSet` orders by `(DateTimeOffset deadline, sequence)`, so equal deadlines are valid and
  deterministic without violating comparison rules.
- `Advance` is synchronous: on return the logical clock has moved, all due tasks are completed and
  the one timer is rearmed.
- Relative and absolute delays use `TimeSpan` and `DateTimeOffset`; the redundant integer and
  ambiguous `DateTime` overloads are removed. API compatibility was not an acceptance goal.
- Cancellation preserves the caller token, removes the entry immediately and rearms the next
  deadline. Disposal is idempotent, cancels pending operations and releases the timer.
- Very long deadlines are rearmed in the maximum interval supported by .NET timers rather than
  overflowing the timer implementation.
- Scheduled publish is tested through the real DI bus, scheduler, fabric and consumer path without
  a sleep or wall-clock verdict.

This path is intentionally independent of the broader scheduling/transport-clock normalization in
`TODO.md`; no unrelated persisted or wire timestamp was redefined.
