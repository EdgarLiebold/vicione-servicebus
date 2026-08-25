# C35 mutation and test-quality review

## Ordinary tests

`InMemoryReceiveEndpointConcurrencyTests` contains two ordinary xUnit facts with one passive
requirement carrier each:

- a limit of 100 admits all 100 held deliveries before any completes;
- prefetch 4 with concurrency 3 keeps delivery 4 out until release, then completes all 4.

The tests use `TaskCompletionSource` barriers, atomic counters and exact state assertions. They use
no sleep, delay, stopwatch, wall-clock assertion, polling, skip, retry, random value or test-order
dependency. Timeout is only the fail-safe bound. Cleanup always releases held handlers before the
harness stops.

## One-cause mutation

The exact product expression

```csharp
new TaskExecutor(context.ConcurrentMessageLimit ?? context.PrefetchCount)
```

was temporarily changed to

```csharp
new TaskExecutor(context.PrefetchCount)
```

The Release build remained green. The two-test class run then produced exactly one failure:

- high-concurrency case: PASS;
- cap case: FAIL, expected maximum `3`, actual maximum `4`.

This proves the new cap case rejects the intended false-green implementation for its own cause. The
single source line was restored by patch, the Release project was rebuilt with zero warnings and
errors, and the two-test class passed 2/2.

Final source guards prove the unconditional-prefetch mutation absent and the required null-coalescing
selection present.
