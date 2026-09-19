# Iteration 220 research

## Scope

Lead read ten state-machine control-flow, conversion, retry, selector and transition sources (1,266
initial lines):

- fault catch, ordinary condition and exception-condition activities;
- composite-event tracking and data-context conversion;
- untyped and message-specific retry activities;
- ordinary and faulted activity selectors;
- hierarchical transition execution and forwarded-request outcome ownership.

On admission, cumulative lead-read progress becomes 821/4,118 sources (19.937%).

## Initial findings

- Most condition, conversion, retry, selector and composite constructors retain required owners
  without deterministic null validation; visitor, probe and pipeline boundaries are similarly
  inconsistent.
- Condition families require exact true/false, matching/nonmatching message and exception routing,
  async failure/cancellation and continuation-order evidence across all shapes.
- Composite status updates require exact RaiseOnce, equality, persistence-before-raise, failure,
  cancellation, concurrency and event/context ownership contracts.
- Data conversion must distinguish missing body, wrong message context and incompatible next
  behavior without reading unsafe message state while preserving exact typed execution/fault tasks.
- Retry must prove policy/cancellation ownership, attempt ordering, typed gating, exception
  unwrapping with original stack/identity and absence of extra continuations on terminal failure.
- Selectors must append exactly one correct container activity, preserve returned binder identity
  and reject missing binders at their own boundary.
- Hierarchical transitions need direct proof for self, sibling, parent, child and disjoint paths,
  exact leave/before-enter/set/after-leave/enter ordering, cancellation and partial-failure state.
- Forwarded outcomes currently expose the caller's mutable payload-type array; snapshot ownership
  and null/empty/null-entry validation require review.
- Public surface, generic names/constraints, current comments, nullability and `Async` naming must
  reconcile bidirectionally with direct contract evidence.
