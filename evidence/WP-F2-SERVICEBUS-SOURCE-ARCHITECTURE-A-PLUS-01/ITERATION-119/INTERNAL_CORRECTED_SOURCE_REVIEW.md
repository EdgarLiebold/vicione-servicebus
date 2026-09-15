# Iteration 119 corrected-source internal counterreview

Status: one P1 composition finding remains; not final A+ acceptance.

The same separate read-only internal reviewer completely reads the corrected
source/test delta against checkpoint `97c1b364bf37ed48387373f5feee3e23f065fd27`.
This is not an external product-team verdict or independent external execution.

## Complete-read scope

Five production files, 836 lines:

- `Middleware/ActivityRedeliveryRetryFilter.cs`
- `Middleware/RetryFilter.cs`
- `RetryPolicies/ConsumeContextRetryContext.cs`
- `RetryPolicies/ConsumeContextRetryPolicy.cs`
- `RetryPolicies/ExponentialRetryPolicy.cs`

Six test/helper files, 2,432 lines:

- `Product/GreenfieldApiArchitectureTests.cs` in the architecture project
- `Retry/RetryFilterTestFactory.cs` in InternalAccess
- `Middleware/RetryFilterTests.cs` in Core tests
- `Monitoring/ServiceBusTelemetryTests.cs` in Core tests
- `RetryPolicies/ConsumeContextRetryPolicyTests.cs` in Core tests
- `RetryPolicies/ExponentialRetryPolicyTests.cs` in Core tests

Every test in those five test files is accounted for: 63 methods and 80 statically
declared cases. The nine Core requirement-mapping additions are read as a delta.
The two existing Iteration-119 evidence reports are read completely. All supplied
frozen production hashes match. No changed C# test/helper remains unread.

Additional complete edge reads cover ProductAssemblyFacts, RetryObservable,
Connectable, BaseRetryContext, BaseRetryPolicyContext, and
ConsumeContextRetryPolicyContext. No reviewer builds, tests, or mutations run;
the executed evidence remains author evidence. Broader unchanged provider suites,
final coverage, and complete acceptance gates remain outside this bounded review.
No files or protected review/results contents are changed or inspected.

## Locally resolved axes

No remaining causal finding is established in the supplied-token cancellation
fix, consume ownership transfer/error disposal, four consume null-task guards,
or tick-precision exponential bounds/convergence. The namespace/arity visibility
closure and explicit Core assembly anchor are corrected. DI/telemetry absence
checks inspect both actual foundation assemblies.

New assertions meaningfully check exact exceptions, committed effects, tokens,
and disposal. Pending fixtures use entry/release barriers and release/drain
cleanup. No assertion-free or missing-await false-positive test is found in the
delta. These local results do not cover the missing nested lifecycle-fault matrix.

## P1: nested retry filters reclassify escaped lifecycle faults

The owning RetryFilter observes creation/completion outside its business catch.
An outer filter still catches the inner filter's escaped observer exception from
`next.SendAsync` and classifies it as a business failure.

Concrete counterexample: outer Immediate(1), inner Immediate(1), initial business
invocation fails, second invocation commits one effect, and inner completion
observation throws a handled observer failure. No terminal business RetryContext
payload exists. Outer retry starts a third invocation, which succeeds as the
inner initial attempt without completion observation. The operation can return
success with two committed effects and a swallowed observer error.

Required oracle: exact observer failure escapes; two business invocations and
one committed effect; both acquired policy contexts disposed exactly once.

| Inner observation phase | Remaining nested issue |
|---|---|
| Creation | Outer policy can restart failed creation and later execute business work. |
| Post-fault | Outer classification can restart downstream work after observation failed. |
| Pre-retry | Outer classification can restart work despite failed pre-retry observation. |
| Completion | Committed effects can replay and the observer failure can disappear. |
| Terminal | Existing business payload avoids multiplication, but outer notification presents a business exception context while an observer exception escapes. |

These phases are one lifecycle-failure ownership defect, not five independent
findings. The reviewer recommends internal, exact-exception-identity ownership,
recognition before outer business classification/notification, propagation
through root/current projections and typed dispatch, and operation-scoped lifetime.
Do not blanket-disable business retry, invent a terminal business RetryContext,
swallow observer errors, or expand public API. Preserve awaited fan-out,
cancellation normalization, independent business budgets, and disposal.

The author accepts the finding for synchronous, faulted-task, and pending nested
causal regressions, owning-boundary correction, controlled counterchanges, and
another corrected-source review before final acceptance. The read-only freeze ends.
