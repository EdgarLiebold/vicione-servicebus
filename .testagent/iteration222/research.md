# Iteration 222 research

## Scope

Lead personally read ten connected state-machine request, response, scheduling and send sources
(1,136 initial lines):

- `RequestActivity.cs`, `RequestActivityImpl.cs`, `RequestCompletedActivity.cs`,
  `RequestFaultedActivity.cs`, `RequestStartedActivity.cs`, `RequestStateMessagePipe.cs`;
- `RespondActivity.cs`, `ScheduleActivity.cs`, `SendActivity.cs`, `UnscheduleActivity.cs`.

These are not yet admitted in the cumulative source manifest. After iteration 221 admits, this
packet would bring the count to 841/4,118 (20.422%).

## Initial findings

- Constructor, visitor, probe and runtime null boundaries are uneven; public documentation and
  generic parameter descriptions include inherited placeholder wording.
- `RequestActivityImpl` persists the new request ID after sending but before scheduling the
  timeout; the partial-state failure boundary needs direct evidence and explicit disposition.
- `RequestActivityImpl.SendRequestAsync` presently resolves addressed endpoints without the
  behavior-context cancellation token (`GetSendEndpointAsync(serviceAddress)`), while the
  subsequent send receives `context.CancellationToken`. Iteration 221 tests observed this
  split; verify the desired contract and cover both token and pre-cancel before correction.
- Both `RequestActivity` variants omit a pre-cancel guard and pass no context token into
  `ContextMessageFactory.UseAsync`; a canceled context may still run its address provider,
  factory and transport. The typed `RequestCompletedActivity` can await a response factory
  and then publish after cancellation; Started/simple Completed/Faulted similarly need a
  pre-cancel no-effects contract. Test exact old RequestId retention and no continuation.
- `RequestStateMessagePipe` projects response/fault addresses, correlation, conversation, TTL and
  serializer; its one-second minimum TTL for expired results must remain distinct from generic
  forwarding semantics.
- Candidate mixed-snapshot defect: Complete/Fault outcome contract names are snapshotted before
  asynchronous endpoint resolution, but the pipe reads mutable saga response/source/fault
  routes, request/conversation IDs and expiration only at SendAsync. Gate resolution, mutate
  every saga field, and verify whether one outcome must retain its original metadata boundary;
  existing snapshot tests change only the contract names.
- Request started/completed/faulted event activities need exact payload identity/contract names,
  timestamps, cancellation and continuation ordering, including factory-failure/null results.
- Schedule, send, respond and unschedule families require token ownership, address/provider
  timing, scheduler absence/failure, previous-token handling, typed and untyped routing,
  lifecycle ordering and context-free await evidence.
- Independent read-only source audit found normal Schedule (both variants), Unschedule, Send
  (both variants) and Respond lack pre-cancellation guards; their factory calls default to
  `CancellationToken.None`, and normal Send also resolves endpoints with that default token.
  Verify no factory/transport/next call and no persisted token change for pre-canceled
  contexts; separately prove exact endpoint-resolution token and old/new schedule ordering.
- Clarify the `IRequestStarted` documentation saying late responses "must be discarded"
  against `PO-2026-08-24-01`, which gives expired request responses/faults a one-second
  forwarding window. The former may describe client acceptance rather than forwarding;
  do not change the contract text until that owner boundary has been traced.
