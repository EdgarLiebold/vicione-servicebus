# Middleware retry

## Decision boundary

The inherited retry fixture and its seven private support files were read together with
`RetryFilter`, every core retry policy/context, the configuration boundary, nested-payload
propagation and the general task retry executor. The inherited fixtures are evidence for behavior,
not an API-shape contract. Public API compatibility with the former product is explicitly not a
goal; useful retry capabilities and their observable semantics are.

## A+ product contract

- exactly one retry layer owns a handled failure; an outer layer may observe the terminal nested
  failure but never starts a second budget for it;
- this ownership survives a change of concrete pipe-context type during typed dispatch;
- retry execution is iterative and therefore safe for large budgets;
- every configured retry consumes exactly one budget slot and the terminal failure is the exact
  failure raised by the final attempt;
- source and policy cancellation can never be reported as successful completion; source
  cancellation preserves the exact caller token;
- delays use the `TimeProvider` owned by the pipe context or explicitly supplied to the general
  retry executor, never an implicit wall clock;
- retry observers see one exact, ordered lifecycle, including success and terminal nested failure;
- policy contexts are disposed exactly once, explicit cancellation is effective even before the
  first failure, and source cancellation remains linked to already created retry contexts;
- interval schedules are validated immutable snapshots, incremental schedules cannot overflow,
  and a randomized exponential delay is selected once per attempt and remains stable thereafter;
- invalid policies, contexts, collaborators and callback tasks fail at their owning boundary with a
  stable diagnostic.

## Product defects corrected

1. Nested retry ownership was consolidated on the non-generic `RetryContext` payload so typed
   dispatch cannot accidentally create a second retry budget.
2. Recursive retry execution was replaced by bounded iterative loops in `RetryFilter` and the
   general task retry executor.
3. Retry delay now honors the configured `TimeProvider`; tests no longer depend on wall-clock time.
4. Source cancellation now propagates the exact source token, while an already cancelled policy
   cannot leave the filter or executor through a false-success path.
5. Policy cancellation before the first `CanRetry` call now works, and the linked cancellation
   source is disposed with its policy context.
6. The task and result retry overloads now share one execution path, retain the exact terminal
   exception, call `PreRetry` and terminal callbacks in order, and reject null policy results.
7. Interval input is copied and exposed read-only; empty and negative schedules are rejected.
8. Immediate, incremental and exponential policies reject invalid limits; incremental overflow and
   exponential range errors fail during construction.
9. Exponential jitter uses the process-wide thread-safe random source and is cached per attempt.
10. The unused `MessageRetryPolicyExtensions` duplicate was removed. General task retry remains in
    `PipeRetryExtensions`; configured message retry remains in `UseMessageRetry` and `RetryFilter`.
    No product or retained legacy-suite call site used the removed extension.

## Preserved intentional behavior

The exception stored on a retry context remains the failure that created that context. Terminal
callbacks receive the terminal failure separately, and the executor rethrows that exact terminal
failure. This keeps diagnostic context stable across an attempt while preventing the historical
mistake of replacing exhaustion with cancellation or rethrowing the first failure.

The non-generic nested retry payload is intentional. It carries the owning retry across context-type
changes; `RetryContext.ContextType` retains the concrete observer type. This is the generalized
replacement for the inherited fixture's command-specific replacement-context hierarchy.
