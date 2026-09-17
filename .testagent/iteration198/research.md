# Iteration 198 research

## Admitted source candidates

The lead read all three current sources in full before delegation:

- `ScheduleDateTimeExtensions.cs`: 431 baseline lines, 20 public schedule overloads.
- `ScheduleTimeSpanExtensions.cs`: 1,071 baseline lines, 40 schedule plus four unschedule overloads.
- `SendByConventionExtensions.cs`: 758 baseline lines, 40 public send overloads.

This is 2,260 baseline physical lines and 104 overloads. All three sources are newly unique, moving
personal-read coverage from 637 to 640 of 4,118 current source files (15.542%). No existing focused
test names these three owners.

## Contract gaps and test axes

- The overload matrices currently reach `source.Add` without owning receiver validation.
- Schedule owners accept a required schedule plus a time/delay provider in provider forms; task and
  delegate message forms also require deterministic registration-time validation.
- Time-span overloads synthesize a `DateTimeOffset` from the behavior context's `TimeProvider` and
  either schedule-owned delay or a supplied delay provider. Tests must prove exact context, single
  invocation, arithmetic and laziness.
- Unschedule overloads must validate receiver/schedule and preserve the exact normal/faulted
  activity plus returned binder identity.
- Convention sends must defer endpoint-convention lookup until activity execution and preserve the
  context-aware callback variants. Those callback parameters are required on the second 20-overload
  family, unlike the nullable simple callback family.
- Direct-message null validation is already downstream-equivalent through `InitializedMessage<T>`;
  the iteration must not add redundant guards solely to move an identical observable boundary.

The mandatory code-testing pipeline, source/test pairing, pseudo-mutation gap analysis and
assertion-quality review require exact reflection censuses, boundary-order assertions, representative
runtime matrices across normal/data/faulted/data-faulted binders and isolated compiled mutations.
