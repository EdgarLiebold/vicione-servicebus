# Iteration 218 research

## Scope

Lead read the ten remaining unadmitted state-machine activity-binder sources (1,322 initial
lines):

- the binder contract and direct execute/ignore/catch/retry binders;
- synchronous and asynchronous conditional and conditional-exception binders;
- trigger-event, data-event and catch-exception persistent composition binders.

On admission, cumulative lead-read progress becomes 801/4,118 sources (19.451%).

## Initial findings

- Several public constructors and binder methods currently accept missing event, machine,
  activity, policy, activity collection, state, builder, condition or callback dependencies and
  fail only through a later collaborator.
- Callback-based fluent composition needs deterministic rejection when a callback is null or
  returns null, without publishing a partially configured binder.
- Persistent binder chains copy activity arrays, but enumeration ownership, append ordering and
  independence between predecessor and successor chains need direct behavioral proof.
- Conditional wrappers need exact synchronous exception timing, asynchronous task/fault/
  cancellation identity and ordered materialization of then/else behavior snapshots.
- Transition-event classification is repeated across binders and must remain exact for enter,
  before-enter, after-leave and leave while rejecting unrelated events and null state.
- Retry and catch composition need evidence for exact policy/behavior/event ownership, nested bind
  ordering, empty branches and collaborator-failure identity.
- Public surface, generic constraints, nullability metadata, method names and retained comments are
  part of this packet's API contract even when a member contains no executable branch.

No genuinely asynchronous public binder method is introduced by this packet; awaited delegates
remain configuration values rather than task-returning binder operations.
