# Iteration 217 research

## Scope

Lead read the seven remaining unadmitted state-machine status-accessor, ignore-filter and
unhandled-callback sources (214 initial lines):

- composite-event accessor contract and implementations: `ICompositeEventStatusAccessor`,
  `IntCompositeEventStatusAccessor`, `StructCompositeEventStatusAccessor`;
- state-event filter contract and implementations: `IStateEventFilter`,
  `AllStateEventFilter`, `SelectedStateEventFilter`;
- unhandled-event callback contract: `StateMachineUnhandledEventCallback`.

On admission, cumulative lead-read progress becomes 791/4,118 sources (19.208%).

## Initial findings

- The concrete accessors reject a missing property descriptor but expose public saga and probe
  boundaries whose current null-failure behavior is delegated to internal collaborators.
- Property compatibility, exact read/write conversion and probe ordering need direct evidence for
  both the integer and strongly typed status representations.
- The all-event filter is intentionally context-independent; the selected filter performs an exact
  compatible behavior-context dispatch but its public constructor currently accepts a missing
  condition and defers failure until invocation.
- The generic filter variance, delegate signature, generic constraints and nullable metadata are
  compile-visible contracts even where the sources contain no executable branch.
- The unhandled callback is invoked by state ownership code and therefore needs direct proof that
  exact context, state, returned task, failure and cancellation identity remain observable.

All members in this packet are synchronous except the callback's task-returning invocation
contract; no member name currently conflicts with the asynchronous naming rule.
