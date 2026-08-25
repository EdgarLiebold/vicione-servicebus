# Pipe-context failure precedence — product-path analysis

## Scope

This cohort replaces the five inherited cases in `PipeContextFailure_Specs.cs`. The complete
`PipeContextSupervisor<TContext>` send path, its context factory and handle contracts, active-context
agent, base agent/supervisor lifecycle, extension methods and transport specialization were read
before the replacement was designed.

## Operation and cleanup ownership

`PipeContextSupervisor<TContext>.Send` has one externally meaningful operation result. A successful
operation remains successful even when later context cleanup fails; otherwise a caller may retry a
message that was already delivered. A failed operation preserves the exact originating exception
instance. Fault notification, stop and disposal are cleanup stages and cannot replace that result.

On failure the stable lifecycle is operation, fault notification, stop, disposal. On success it is
operation, stop, disposal. Every later stage is attempted even if an earlier cleanup stage fails.
The caller's cancellation token reaches the stop context unchanged. A fault while asynchronously
acquiring the context follows the failure lifecycle and never invokes the operation.

## Cache boundary

The existing source-owner tests independently prove that a fault invalidates the cached underlying
context and that explicit invalidation causes recreation. This cohort composes with those tests; it
does not duplicate their factory/cache assertions.

## A+ disposition

- Preserve one unambiguous operation result and exact exception identity.
- Keep cleanup failures diagnostic and execute every cleanup stage.
- Prevent cleanup after successful delivery from creating a false retry signal.
- Use deterministic in-memory fakes and exact event order; no timing, broker or log-message oracle.
- Replace the incident-specific product comments with concise transport-independent invariants.
