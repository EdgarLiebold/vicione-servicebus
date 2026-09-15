# Internal operation-ownership counterreview

Status: NN-01 through NN-04 and external-caller isolation NN-05 are locally corrected.
Four adjacent causal boundaries remain open below. This is a separate internal
read-only lead review, not independent external acceptance or final Iteration119.
The reviewer runs no builds/tests/mutations, edits no files, spawns no agents and
inspects no protected review/results contents. Source freeze is released after
reverifying all nine source/test input hashes unchanged.

## Accepted local axes

All41 methods/111 declared cases are personally read by the reviewer: new8/35 and
existing33/76. No Critical/High assertion anti-pattern is found. Exact exception
identities, effects/attempts/disposals, lifecycle sequences, controlled timers and
entered/release barriers are meaningful; local collaborators are not product dummies.
The 35/35,76/76 and3,744/3,744 green results are author executions, not the reviewer's.

Normal five-phase observer ownership, root/current/replacement/typed projections,
factory/classification failure ownership, exactly-once cleanup and ordered exact
primary/cleanup aggregation are accepted in this scope. Retained diagnostics no
longer determine later external-operation admission; unrelated caller-owned
payloads are preserved. Both redelivery families use the common algorithm.
Changed comments, Async/type/file/folder names and internal visibility yield no
additional naming or structure finding. This is not absolute feature/correctness proof.

## Accepted open causal findings

### NN-06 — P1: scheduling and acknowledgment lose operation cancellation

RedeliveryRetryExecution.ScheduleAsync supplies no token to ScheduleRedeliveryAsync;
both ordinary/activity acknowledgment callbacks omit available cancellation arguments.
The public scheduling contract explicitly accepts operation cancellation. Actual
scheduler-backed implementations forward that supplied default token, including
endpoint lookup. A valid scheduling payload awaiting only its argument cannot be
released by source or selected policy cancellation. A cancellation occurring after
PostFault can reach scheduling without a fresh check. The catch-all incorrectly
wraps requested cancellation as TransportException.

Required red-first evidence: source/policy cancellation before/during scheduling
and acknowledgment, original terminal-token identity, no extra business work or
scheduling, acquired cleanup and drained pending work. Link both independent tokens,
check before each stage, forward to actual public calls, and normalize requested
cancellation before wrapping genuine transport failure. A failed acknowledgment
after successful scheduling must never schedule again.

### NN-07 — P2: direct terminal fault callbacks receive an uncancellable token

Both generic terminal/nested RetryFaultedAsync calls and common redelivery terminal
calls use the default token. Their public contracts explicitly promise notification
cancellation through that argument. A valid custom pending callback waiting only
on its argument retains processing and acquired policy state indefinitely after
source/policy cancellation. Observer APIs already carry context cancellation and
need no API expansion.

Required causal evidence: initial/after-retry/nested policy callbacks and both
redelivery families, source/decision cancellation, exact normalized token, no outer
business restart, exactly-once cleanup and finally-drained work.

### NN-08 — P2: actual custom-decision acquisition/publication remains unguarded

PublishTerminal reads RetryContext.Exception outside infrastructure ownership.
A valid-context custom decision whose Exception getter throws exact E once can
escape unowned and let an outer retry reinvoke inner business work. All terminal
publication callers share this hole. Separately, the custom Context getter can
return a valid value during EnsureDecision and null during later actual acquisition;
Enter(null) then throws outside the guarded read.

Required causal evidence: exact getter failure or contract diagnostic, one factory,
no additional business work, cleanup and typed/projection preservation. Guard
diagnostic publication and actual context admission in their owning boundaries.

### NN-09 — P2: independently invoked children collide under an active parent

The new external-caller NN-05 fixture proves a separate caller's async context,
not independently initiated children within an active parent. Current Enter joins
every active ambient identity and retains exact failures while any parent lease
exists. Inside a parent business stage, catch child observer E, then invoke another
child business-retry pipe on the same context; that child legitimately throws exact
E once but incorrectly sees earlier lifecycle ownership. Parallel child invocations
under that parent have the same collision.

Required red-first evidence: sequential caught-child reuse and deterministic parallel
children under a live parent, alongside all normal nested-filter/typed-dispatch
propagation cases. Distinguish invocation ownership without weakening ordinary
terminal budget propagation or silently imposing a new public contract restriction.

## Exact complete reviewer read scope

```text
src/ViciOne.ServiceBus/RetryPolicies/RetryOperationState.cs
src/ViciOne.ServiceBus/RetryPolicies/RetryPolicyExecution.cs
src/ViciOne.ServiceBus/RetryPolicies/RedeliveryRetryExecution.cs
src/ViciOne.ServiceBus/Middleware/RetryFilter.cs
src/ViciOne.ServiceBus/Middleware/RedeliveryRetryFilter.cs
src/ViciOne.ServiceBus/Middleware/ActivityRedeliveryRetryFilter.cs
tests/ViciOne.ServiceBus.Tests/Middleware/RetryOperationOwnershipTests.cs
tests/ViciOne.ServiceBus.Tests/Middleware/RetryFilterTests.cs
tests/Testing/ViciOne.ServiceBus.Tests.InternalAccess/Retry/RetryFilterTestFactory.cs
src/ViciOne.ServiceBus.Abstractions/Middleware/RetryContext.cs
src/ViciOne.ServiceBus.Abstractions/Middleware/RetryPolicyContext.cs
src/ViciOne.ServiceBus.Abstractions/Middleware/IRetryPolicy.cs
src/ViciOne.ServiceBus.Abstractions/Middleware/BasePipeContext.cs
src/ViciOne.ServiceBus.Abstractions/Middleware/Payloads/ListPayloadCache.cs
src/ViciOne.ServiceBus.Abstractions/Scheduling/MessageRedeliveryContext.cs
src/ViciOne.ServiceBus/Scheduling/Contexts/ScheduleMessageRedeliveryContext.cs
src/ViciOne.ServiceBus/Middleware/DelayedMessageRedeliveryContext.cs
src/Transports/ViciOne.ServiceBus.RabbitMq/RabbitMqTransport/RabbitMqQueueRedeliveryContext.cs
.testagent/iteration119-ownership-packet.md
evidence/WP-F2-SERVICEBUS-SOURCE-ARCHITECTURE-A-PLUS-01/ITERATION-119/OPERATION_OWNERSHIP_PACKET.md
```

All eight new requirement tuples are inspected as a delta, not the whole catalog.
Parent/author reads are distinct; the reviewer does not replace the required personal
source reading. Reviewer hashes below are reverified unchanged by the author too.

| Frozen source/test input | SHA-256 |
|---|---|
| RetryOperationState.cs | `d0b62a7a0fbd7db8f9723983fa071d21d8778fd8e52acd3e2e2f3735032916ff` |
| RetryPolicyExecution.cs | `21dce8b690a48ecf45b8150cdcb8bcbf1c756475c2a7fa083d65f85c1035fdc6` |
| RedeliveryRetryExecution.cs | `3593a3b77301e834dd0cf840673824496b21d2d8f1556176f7c0e3ff938e041f` |
| RetryFilter.cs | `8b9e533890c2a2ff81479cedd05b1f1ccd0f05449d3b1b66d352aea502591730` |
| RedeliveryRetryFilter.cs | `79dfa60f8c677d52ae0f3fee60dc0e88a79e898cf5a7b1f1066b1abda1411071` |
| ActivityRedeliveryRetryFilter.cs | `c2fafd3f503e015b7194cc1e3d2bb30ebad7cca90e584b7c3884ccfe22a1562d` |
| RetryOperationOwnershipTests.cs | `023bd417a5f2f68768a1fa37c7412a1b9c4f2383cf54efa8751b03cbb3d3bdd2` |
| RetryFilterTests.cs | `2ca4fa3ad01bf7a2dbe7f6e5955af665ff5e0dc9afaf578f0afb3016c42560d6` |
| RetryFilterTestFactory.cs | `b392bca34d94036fcc66b62a94691629b08d1b44f9121c304ec609a754e9b76b` |

No entire-product coverage, completed iteration or final A+ acceptance is claimed.
Next work remains the same original goal and cohesive Retry/Rescue owner, with all
four findings accepted before an explicitly intermediate normal-push checkpoint.
