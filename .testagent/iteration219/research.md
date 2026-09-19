# Iteration 219 research

## Scope

Lead read ten state-machine execution and factory activity sources (1,236 initial lines):

- synchronous and asynchronous action activities;
- synchronous and asynchronous delegate-backed activity factories;
- container-backed and fault-only container factories;
- synchronous and asynchronous faulted actions;
- execute-on-fault and slim data-context adapters.

On admission, cumulative lead-read progress becomes 811/4,118 sources (19.694%).

## Initial findings

- Several public constructors retain required delegates or activities without deterministic null
  validation, while neighboring generic forms already validate selected delegates.
- Every visitor, probe, execution, fault and continuation boundary needs exact parameter precedence
  and collaborator identity rather than incidental downstream failures.
- Factory callbacks need proof that null results, synchronous failures, faulted tasks and canceled
  tasks retain causal identity and never invoke the next pipeline stage accidentally.
- Synchronous and asynchronous actions need ordered action-before-continuation behavior, exact
  task/failure/cancellation propagation and no duplicate invocation under concurrency.
- Faulted actions and container factories must discriminate matching from nonmatching exception
  contexts without resolving or invoking fault handlers on the bypass path.
- Execute-on-fault and slim adapters must preserve their wrapped activity, original fault context,
  data context, continuation and visitor/probe semantics exactly.
- Public surface, generic constraints, nullability, parameter names, diagnostics and `Async` naming
  must reconcile bidirectionally with direct contract evidence.
