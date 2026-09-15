# Internal nested retry ownership counterreview

Status: the original ordinary in-memory nested observer defect is resolved in
the reviewed scope. Four adjacent findings remain open: two P1 and two P2.
This is an internal read-only lead counterreview, not independent external
acceptance or repository-wide correctness proof.

## Frozen source and execution separation

The reviewer reads the complete current RetryFilter test file: 33 methods,
76 statically declared cases, 1,645 lines. All seven added requirement tuples
are inspected as a delta. The reviewer runs no builds, tests or mutations,
edits no files, reads no protected review/results contents, and spawns no agents.
Executed results below belong to the author, not the reviewer.

| Frozen input | SHA-256 |
|---|---|
| RetryLifecycleFaults.cs | `89326d7d2e945c884d814f3d0aa0a1ea67416f523db94246fc82bd93f4c39d39` |
| RetryFilter.cs | `f35da80e74682bd507bebb4db9f63ab2e1f5ac502e269132d9e083c226dfc7f8` |
| RetryFilterTests.cs | `2ca4fa3ad01bf7a2dbe7f6e5955af665ff5e0dc9afaf578f0afb3016c42560d6` |
| CoreRequirements.json | `7adcf4dc5461a432bde37819246e2dfe9a51e9c998f4ee14234ed335c3cd4b2d` |

Author evidence: the first five-phase nested observer matrix has 15 causal
failures against unchanged checkpoint 2968483f0, then passes 50/50 after its
scoped correction. Extended projection/reuse/callback cases execute 64 cases
with three causal failures. Actual typed dispatch and independent delay
cancellation extend that to 76 cases with five causal failures. The corrected
76-case selection passes 76/76 with zero skips after a zero-warning/error build.
Artifact paths and exact oracles are recorded in [VALIDATION.md](VALIDATION.md).
That green selection does not exercise the four counterexamples below.

## Accepted local axes

- Ordinary nested in-memory retry filters preserve exact escaped observer
  failures across creation, post-fault, pre-retry, completion and terminal phases.
- Root/current leases retain lifecycle ownership across independent context
  projections and typed command dispatch, including replacement retry contexts.
- Sequential reuse after creation/completion faults does not suppress a later
  legitimate business retry using the same context and exact exception instance.
- Preparation owns delay/callback failures and preserves original source/policy
  cancellation-token identity while releasing injected-clock timers.
- Policy fault callbacks and nested observer notifications are awaited and owned.
- Observer snapshot fan-out is retained; leases are idempotently released.
- Meaningful assertions cover exact exceptions, effects, events, token identities,
  disposal, pending barriers and timer cleanup. No assertion-free or missing-await
  false-confidence test is found in this scope.
- The coordinator is internal; its handwritten comments and names describe its
  functionality. No public feature or API is removed by this correction.

## Accepted open findings

### NN-01 — P1: redelivery composition lacks lifecycle ownership

Owners: `Middleware/RedeliveryRetryFilter.cs` and
`Middleware/ActivityRedeliveryRetryFilter.cs`.

Neither filter leases/checks lifecycle ownership or marks its own observer
failures. With outer redelivery and inner in-memory retry, business invocation
one can fail, invocation two can commit, and inner completion observation can
fail. The outer filter then schedules broker redelivery for the observer error,
potentially replaying the committed effect. In the reverse composition, a
redelivery observer failure can consume an outer in-memory business retry budget.

Required causal evidence: both directions for ordinary and activity redelivery,
exact observer exception, exact attempts/effects, zero redelivery scheduling for
lifecycle failure, and exactly-once acquired-context disposal. Leases must span
downstream execution; adding only a lookup is insufficient because ownership
would otherwise be cleared before the outer catch.

### NN-02 — P1: cleanup failure can replay committed business work

Owner: `Middleware/RetryFilter.cs`, implicit policy-context disposal.

If inner business work succeeds and an acquired custom policy context throws
once during disposal, the outer retry can classify cleanup as business failure,
replay the committed work, and return success after cleanup subsequently succeeds.
Disposal is currently outside lifecycle marking.

Required causal evidence: one invocation/effect, exact cleanup failure escaping,
and one disposal per acquired context. Simultaneous primary and cleanup failures
also require a documented preservation/aggregation rule; ordinary using semantics
can currently replace the primary failure with the cleanup exception. A correction
must preserve both failure identities rather than silently hiding either one.

### NN-03 — P2: policy infrastructure failures remain unowned

Owner: `Middleware/RetryFilter.cs`, factory, both CanRetry boundaries, IsHandled,
and invalid-output admission diagnostics.

An inner policy factory/classifier can throw and let an outer handled policy
restart downstream work. Null policy/context/decision diagnostics can similarly
consume the outer business budget. These infrastructure failures are distinct
from the ordinary business exception under evaluation.

Required causal evidence: exact infrastructure failure, one factory/classifier
invocation, no additional business execution, and cleanup of acquired state.
Only infrastructure failures may be marked; ordinary classification outcomes
must retain legitimate retry and exception-filter behavior.

### NN-04 — P2: terminal business ownership outlives an operation

Owner: `Middleware/RetryFilter.cs`, terminal RetryContext publication and
PropagateNestedRetryFailureAsync. The reuse theory covers create/complete only.

First reach terminal business failure and throw once from its terminal observer.
Reuse the same context; on the next operation throw that exact observer exception
as a legitimate business failure, then succeed. The old RetryContext payload can
be interpreted as current nested terminal ownership, suppressing the later retry:
one attempt/zero effects instead of two attempts/one effect.

This defect predates the new lifecycle marker. The marker correctly clears its
exception set, but the separate business ownership signal is not operation-scoped.
Extend the reuse matrix to terminal and associate terminal ownership with the
active operation. Preserve caller-owned payloads and public post-failure
diagnostics; do not broadly delete RetryContext payloads.

## Required continuation and limits

The author accepts all four findings as open before the intermediate checkpoint.
They are source-established counterexamples, not reviewer-executed test results.
Implement causal regressions first, then coherent lifecycle ownership across
filter families, infrastructure admission/classification and cleanup. Define
combined-failure behavior explicitly. Prove terminal reuse without erasing
caller-owned diagnostics. Execute meaningful separate controlled counterchanges,
repeat the scoped review, then rerun final coverage/CRAP and repository gates.

Additional assurance limits include multi-observer pending/failure integration
and direct fault-callback failure variants. No full-provider coverage, 100%
correctness, completed Iteration119, or final A+ acceptance is claimed.

## Complete reviewer read scope

```text
src/ViciOne.ServiceBus/RetryPolicies/RetryLifecycleFaults.cs
src/ViciOne.ServiceBus/Middleware/RetryFilter.cs
tests/ViciOne.ServiceBus.Tests/Middleware/RetryFilterTests.cs
tests/Testing/ViciOne.ServiceBus.Tests.InternalAccess/Retry/RetryFilterTestFactory.cs
src/ViciOne.ServiceBus/Middleware/RedeliveryRetryFilter.cs
src/ViciOne.ServiceBus/Middleware/ActivityRedeliveryRetryFilter.cs
src/ViciOne.ServiceBus/Configuration/Redelivery/RedeliveryRetryPipeSpecification.cs
src/ViciOne.ServiceBus/Configuration/Redelivery/ExecuteContextRedeliveryPipeSpecification.cs
src/ViciOne.ServiceBus/Configuration/Redelivery/CompensateContextRedeliveryPipeSpecification.cs
src/ViciOne.ServiceBus.Abstractions/Observers/Observables/RetryObservable.cs
src/ViciOne.ServiceBus.Abstractions/Util/Connectable.cs
tests/ViciOne.ServiceBus.Tests/Testing/ObservableTimeProvider.cs
src/ViciOne.ServiceBus.Abstractions/Middleware/IRetryPolicy.cs
src/ViciOne.ServiceBus.Abstractions/Middleware/RetryPolicyContext.cs
src/ViciOne.ServiceBus.Abstractions/Middleware/RetryContext.cs
```
