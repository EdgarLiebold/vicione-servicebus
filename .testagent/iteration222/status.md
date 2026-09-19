# Iteration 222 status

Research continues; no source or test admission yet. The lead has personally read
the ten source files listed in `research.md` (1,136 initial lines). Three Sol xhigh
agents independently reread disjoint source subsets (3/3/4 files) without editing
or running builds. Cumulative lead-read source count remains 831/4,118 until
iteration 221's open admission findings are dispositioned; the ten candidates
would yield 841/4,118 (20.422%), not a total-goal completion percentage.

Read-only source audit findings to resolve or explicitly disposition:

- `RequestActivity` validates neither `next` nor pre-cancellation before dispatch;
  `RequestActivityImpl` generates the ID and resolves an addressed endpoint before
  observing cancellation, and omits the token from endpoint resolution. A null
  continuation can be discovered after send/persistence. Sending/persisting before
  timeout scheduling may leave partial state when the scheduler fails; establish
  the supported recovery contract before changing order.
- `RequestStateMessagePipe` reads mutable saga routing/expiry at pipe execution
  after the caller has selected an endpoint; divergent route metadata under
  mutation across that await is an ownership risk, not yet a demonstrated bug.
- Typed `RequestCompletedActivity` invokes a response factory before cancellation
  and accepts a null result for a nonnullable typed payload. `RequestFaultedActivity`
  validates/reads its fault before a canceled transport observes the token.
  Started/simple Completed similarly construct payload before transport. Public
  lifecycle argument boundaries are inconsistent with the guarded iteration-221
  activities.

Contract-trace resolution: the typed completion factory result is declared
nonnullable; `IRequestCompleted.Payload` is nonnullable and its payload type
announces `TResponse`. The initializer and serializer can nevertheless carry
a nested null, but `ForwardedRequestOutcome` rejects it on the receive side.
Rejecting a null at its production boundary is therefore a concrete correctness
fix, pending compliant regression evidence. The typed `RequestCompletedActivity`
now explicitly rejects a factory-returned null task or null response before
publication. Strict Sagas and Core test-project Release builds each passed
with 0 warnings/errors. The unfiltered Core suite passed 6,217/6,217 with
0 skipped on the corrected source. Whitespace verification passed; the
corrected source SHA-256 is
`683b6c8aef9574a708e9536f4125a58fccb5f8c169da534e0ec7b383f0a4f01f`.
This source-only correction is not yet
mutation- or §4.3-qualified. Conversely, built-in endpoint
resolution and send check pre-canceled tokens; the iteration-221 Complete/Fault
pre-cancel warning depends on a token-ignoring injected collaborator and is a
hardening/contract-precedence question, not a confirmed built-in send defect.
- `RespondActivity`, both `SendActivity` variants, both `ScheduleActivity`
  variants and `UnscheduleActivity` can reach collaborator work before observing
  pre-cancellation. Message factories and addressed-endpoint resolution receive
  the default token in several paths. Constructors defer required-dependency
  failures, and Schedule's untyped nullable schedule handling is inconsistent.
- Schedule/Unschedule may race on one saga's mutable token across awaits.
  Upstream protection differs by repository, as traced below.

Partial upstream read: `InMemorySagaConsumeContextFactory` acquires
`SagaInstance.MarkInUseAsync` for loaded state, `SagaInstance` uses an exclusive
semaphore, `SendSagaPipe` awaits the complete policy + repository action and then
disposes the consume context, and `InMemorySagaConsumeContext.Dispose` releases
that lease. This supports per-saga serialization for the normal **InMemory**
repository path, not for arbitrary/custom/EF repositories or direct public
activity execution. Further EF source tracing distinguishes its normal
pessimistic path, which holds a row lock + transaction across awaited activity
execution, from its supported optimistic path, which loads without a row lock
and may run the external scheduler twice before either save/optimistic conflict.
A losing save cannot undo its accepted external schedule. Custom repositories
declare no serialization guarantee, and EF's ambient-transaction path delegates
lock behavior to the caller. Thus the same-saga schedule/unschedule ownership
risk is concrete in supported optimistic/custom paths; exact recovery and
token-owner semantics still need proof before product correction.

The §4.3 full-owning-test-project personal-read/ordering gate is open. Do not
turn these source hypotheses into new test design/edit or A+ acceptance until that
gate and the historical order deviation are explicitly addressed. The active
goal continues with read-only inventory and semantic reading meanwhile.

Further disjoint **agent-only, read-only** source audit (17 additional activity
files; this does not increase the lead-personal source-read/admission count):

- Action/async/factory/faulted-action flows reject required/null delegate results
  and propagate the original context. They do not force pre-cancellation before
  user code; without a corresponding source contract this is only a conditional
  API question. Untyped async-factory execution is interface-explicit while its
  typed counterpart is publicly callable on the concrete type.
- `TransitionActivity` raises before-enter asynchronously, then writes the target
  without rereading state. A nested transition in a lifecycle handler can be
  overwritten or result in an Enter notification inconsistent with the current
  state; a failing after-leave/enter leaves the in-memory target assigned. The
  lead has also personally read this 181-line source, but it is not admitted.
  Data-converter covariance requires an independently compatible continuation;
  it fails closed for an otherwise valid derived-message/base-activity pairing.
- `CompositeEventActivity` marks completion before awaiting its nested raise;
  failure/cancellation can cause same-instance retry to be suppressed by
  RaiseOnce. Concurrent status read-modify-write is conditional on same-saga
  serialization. Condition activities may invoke delegates on a canceled
  context and the retry delay selects system time. These are audit findings,
  not yet regression-proven fixes or broad transport acceptance.

The lead's hash-bound partial personal read of the owning Core test project is
tracked separately in `../core-test-project-full-read/partial-ledger.md` (80/702
at this checkpoint). Historical ordering is still not retrospectively cured.

Later source-only checkpoint: `RequestActivity` now rejects null execution
contexts/continuations and observes an already-canceled context before address
provider/message-factory work on all three execution overloads. Its shared
`RequestActivityImpl.SendRequestAsync` rechecks cancellation before generating a
request ID, covering cancellation during an awaited message factory. A separate
agent edited only these two source files; the lead personally reread their full
post-edit versions. No tests were edited. The agent's initial change also
forwarded cancellation into endpoint resolution. A fresh Core run then failed
the existing `StateMachineFaultedRequestScheduleActivitiesDeepContractTests`
faulted-request test reproducibly (full 6,216/6,217; focused 0/1): its strict
endpoint proxy explicitly expects `CancellationToken.None` at resolution. This
shared `RequestActivityImpl` path serves faulted requests too. The lead removed
**only** that token-forwarding change, preserving the new early cancellation
checks without changing the pinned endpoint-resolution contract. Propagation at
endpoint resolution remains an explicit pending API/test decision after §4.3,
not an accepted fix. A separate strict Core Release build on the corrected
source passed with zero warnings/errors; the unfiltered MTP Core rerun passed
6,217/6,217 with no skips. The first `dotnet test` invocation included an
unsupported build-server flag and ran zero tests; it is not counted as evidence.
Focused causal mutation evidence for this source change remains outstanding.

Agents declined changes to `TransitionActivity` and `CompositeEventActivity`:
an immediate state re-read or moving the completion write after nested RaiseAsync
would pick an undefined nested-transition precedence or break same-saga
reentrancy. Current documents/tests pin ordinary lifecycle order but not a
winner for nested lifecycle transitions. These risks need broader contract
derivation and, if behavior changes, PO-level semantic disposition; no quick
line-reorder is represented as a correction. The active goal continues.
