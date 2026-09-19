# Iteration 221 research

## Scope

Lead personally read ten state-machine request timeout/forwarding, publish, send, respond,
faulted request, schedule and unschedule activity sources (1,311 initial lines):

- `CancelRequestTimeoutActivity.cs`, `CompleteRequestActivity.cs`, `FaultRequestActivity.cs`;
- `FaultedPublishActivity.cs`, `FaultedRespondActivity.cs`, `FaultedSendActivity.cs`,
  `PublishActivity.cs`;
- `FaultedRequestActivity.cs`, `FaultedScheduleActivity.cs`, `FaultedUnscheduleActivity.cs`.

On admission, cumulative lead-read progress becomes 831/4,118 sources (20.180%).

## Initial findings

- Most externally reachable constructor, visitor, probe and runtime boundaries in the selected
  activity families lack deterministic required-argument checks and precise current documentation.
- Completion/fault forwarding must prove expiration boundary, payload and contract snapshot
  identity, endpoint routing, send-pipe metadata, cancellation and continuation ordering.
- Publish/send/respond and request fault paths need matching/nonmatching exception and message
  covariance, factory cardinality, destination/address precedence, failure/cancellation and
  exact context/continuation identity across both typed and untyped shapes.
- Scheduling/unscheduling must prove token ownership, previous-token cancellation boundaries,
  idempotence, scheduler absence/failure, cancellation and state mutation timing.
- Public API shape, exact generic constraints/names, comments, nullability and `Async` names must
  reconcile with bidirectional test/requirement evidence.
