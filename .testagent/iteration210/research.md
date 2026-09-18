# Iteration 210 research

## Scope

Lead read eleven previously unadmitted saga middleware sources (1,069 final lines):

- correlation/query ingress and missing-instance redelivery: `CorrelatedSagaFilter`,
  `QuerySagaFilter`, `MissingInstanceRedeliveryPipe`;
- repository lifecycle: `SendSagaPipe`, `SendQuerySagaPipe`, `MissingSagaPipe`;
- context split/merge and rescue: `SagaMergePipe`, `SagaMessageMergePipe`, `SagaSplitFilter`,
  `SagaMessageSplitFilter`, `RescueExceptionSagaConsumeContext`.

On admission, cumulative lead-read progress becomes 736/4,118 sources (17.873%).

## Initial findings

- Query ingress can report consumed before a downstream failure, can misattribute a bypass-path
  downstream failure as a saga fault, and accepts a successful null query result.
- Public filter boundaries, cancellation-cause preservation and observer failure composition require
  explicit contracts; redelivery replay requires bounded/cancelable work and complete probing.
- Missing-saga discard can run twice after its own failure. Query/missing logging and all repository
  cleanup paths can lose resources or replace primary operation failures.
- Query results that become stale before load currently bypass the missing policy entirely.
- Saga/message split/merge does not reliably preserve the out-of-band half after adapters replace
  a context; the message merge can replace the owned saga and completion owner.
- Rescue exception projection lacks failure boundaries and single-publication concurrency.

All genuinely asynchronous members already carry the `Async` suffix; synchronous `Probe`,
construction and query-factory members do not.
