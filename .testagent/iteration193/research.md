# Iteration 193 research

## Scope

Twenty previously unadmitted state-machine configuration, request/schedule metadata, state runtime
and public contract sources, totaling 1,496 physical lines. After admission, cumulative personal-read
coverage will be 585/4,118 current sources (14.206%).

- Configuration/correlation: composite-event options, event correlation, missing-instance,
  request/schedule configuration and uncorrelated-event validation.
- Runtime metadata: composite status, concurrency mode and concrete state-machine event, request,
  schedule and state implementations.
- Public contracts: event, request/settings, schedule, state, state machine and saga state machine.

The lead read every selected source completely before authorizing changes. Three Sol 5.6 xhigh
agents are auditing strictly non-overlapping areas; centralized requirements, integration,
mutations, coverage/CRAP, manifests, evidence and publication remain with the lead.

## Initial risk inventory

- Concrete request/schedule constructors and entry points expose several required collaborators but
  do not consistently validate them before reflection or delegation.
- State runtime owns hierarchy, ignore/filter, superstate fallback, observer-task, cancellation and
  unhandled-event behavior across parallel typed and untyped paths; direct causal coverage is needed.
- Configuration and declaration-only interfaces require precise shape/constraint evidence rather
  than tests that merely exercise locally assigned delegates.
- Composite status and state/event value semantics require boundary evidence for high bits, equality,
  ordering, recursive hierarchy and lifecycle-event exclusion.
