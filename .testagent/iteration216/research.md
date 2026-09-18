# Iteration 216 research

## Scope

The lead read the complete twelve-file state-machine behavior construction and fault-dispatch
packet (966 initial lines):

- behavior factories and builder contract: `Behavior`, `IBehaviorBuilder`,
  `ActivityBehaviorBuilder`, and `CatchBehaviorBuilder`;
- ordinary, terminal, empty, faulted, data-adapter and execute-on-fault behavior nodes; and
- runtime exception-type dispatch in `ExceptionTypeCache`.

No earlier admission evidence names these twelve paths. On admission, cumulative lead-read progress
becomes 784/4,118 sources (19.039%).

## Findings

- Builders and terminal nodes accepted null activities, producing position-dependent delayed
  failures. Builder materialization and concurrent addition also lacked one atomic freeze boundary.
- Empty, faulted, data-adapter, terminal and execute-on-fault nodes did not consistently own their
  visitor, probe, execution, fault-context or constructor dependencies.
- `CatchBehaviorBuilder` carried a private duplicate terminal implementation instead of composing
  the public `LastCatchBehavior`, allowing their contracts to diverge.
- Ordinary failures that occur after a caller token changes must still reach fault dispatch with
  their exact identity; treating the later cancellation as dominant would erase the causal error.
- Runtime exception dispatch used a process-lifetime strong dictionary whose closed generic values
  also referenced arbitrary exception types, permanently rooting collectible plug-in assemblies.
- Direct causal evidence was absent for cache partitioning, builder freezing and order, exact
  Empty-versus-Faulted terminal selection, adapter identity, fault routing, visitor/probe behavior,
  returned-task identity, and deterministic null diagnostics.

The three delegated review/test packets are disjoint: builders and terminals; cached/static and
adapter behaviors; and activity execution, execute-on-fault, and exception dispatch.
