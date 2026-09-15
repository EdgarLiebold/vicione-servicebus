# Iteration 119 retry and rescue validation

Status: focused remediation validated; final iteration acceptance is still open.

## Review and ownership

The author manually reads the complete initial 78-file, 5,430-line provider-neutral
retry/rescue source owner, its comments, direct tests, and affected runtime consumers.
The shared Split specification correction extends that scope through a causal
public rescue-configuration regression. Source, tests, comments, names, and folders
are edited manually, without a generator.

The separate internal counterreview reads the complete changed source and test
files at checkpoint `97c1b364bf37ed48387373f5feee3e23f065fd27`. Its exact scope,
findings, and limits are in [INTERNAL_COUNTERREVIEW.md](INTERNAL_COUNTERREVIEW.md).
It is not an independent external acceptance.

Core remains the independently compiled `src/ViciOne.ServiceBus` project. Contract
and capability assemblies remain siblings under `src`; external integrations
remain grouped under `Persistence`, `Scheduling`, and `Transports`. The source
ownership decision is documented in `docs/api-surface.md`.

## Counterreview requirement-to-evidence map

| Requirement | Concrete evidence |
|---|---|
| Creation observer faults do not start business execution | `CreationObserverFailure_PropagatesWithoutExecutingBusinessWorkAsync`; exact exception, zero operations, one disposal |
| Completion observer faults do not repeat committed effects | `CompletionObserverFailure_DoesNotReplayASuccessfulOperationAsync`; exact exception, two attempts, one successful effect, one disposal |
| Pending observation is awaited without business replay | `PendingObserverFailure_IsAwaitedWithoutStartingOrReplayingBusinessWorkAsync`; entry/release barriers and exact before/after operation/effect/disposal counts |
| Pending pre-retry work observes independent source and policy cancellation | `PendingPreRetryWork_ReceivesSourceOrPolicyCancellationWithoutAnotherOperationAsync`; pending barrier, cancellable callback argument, exact terminal token, one operation and disposal |
| Exponential bounds preserve sub-millisecond values | `FractionalMinimum_IsPreservedAtTickPrecision`; exact tick-defined minimum/maximum bounds |
| Positive exponential increments converge without zero growth | `PositiveTickDelta_ReachesTheBoundedCapWithoutZeroGrowth`; bounded 64-retry budget, final cap range, exact terminal state |
| Missing nested policy state does not leak acquired ownership | `WrappedPolicyOutput_RejectsMissingPolicyOrConsumeContext`; typed/untyped exact exception contract and disposal count |
| Failed initial projections release acquired ownership | `InitialProjectionFailure_DisposesTheAcquiredPolicyContext`; null/throw branches, exact failure identity or message, one disposal |
| Failed representation releases state and cancellation registration | `RepresentationFailure_ReleasesTheContextAndItsCancellationRegistration`; typed/untyped one disposal and zero cancellation callbacks after failure |
| Both consume projections diagnose invalid callback results | `CallbackTask_RejectsNullUnderlyingResultsInBothConsumeProjectionsAsync`; both projections and pre-retry/fault boundaries, exact InvalidOperationException messages |
| Retry implementation visibility is closed over actual Core ownership, activity arity, and child namespaces | `RetryImplementationTypes_AreNotPartOfThePublicApi`; exact Core identity and both separate visibility counterchanges killed |

All nine new method mappings are added manually to the Core requirement projection.
The strengthened missing-context disposal oracle belongs to an existing mapping.

## Executed corrected-source checks

- Serial Unit build: zero warnings and errors, 3m26.27s.
- Three-class focused counterreview suite: 57/57; then 59/59 after representation
  ownership evidence is added; zero skips.
- Corrected complete bidirectional Async guard at the first checkpoint: 1/1.
- First checkpoint full hosts: Core 3,650/3,650 and Abstractions 696/696.
  These predate the counterreview remediation and are not final-source acceptance.

## Separate controlled counterchanges

| Counterchange | Selected cases | Causal failures | Accepted-source restoration |
|---|---:|---:|---|
| Completion observation re-enters business classification | 35 | 3 | RetryFilter SHA-256 `78a85794cd11d54bf64f906cd1a45c9dfb11dc3c6a42ce15c36e6fc0b8b792fd` |
| Creation observation re-enters business classification | 35 | 3 | same RetryFilter SHA-256 |
| Pre-retry callback receives CancellationToken.None | 35 | 2 | same RetryFilter SHA-256 |
| Fractional minimum is truncated to milliseconds | 6 | 1 | ExponentialRetryPolicy SHA-256 `f80d86f398891b14c70b08cf9d62a453e18724915dc86c9559981bd7eccaa242` |
| Exponential increment becomes zero | 6 | 1 | same ExponentialRetryPolicy SHA-256 |
| Consume construction error path omits owned disposal | 18 | 6 | ConsumeContextRetryPolicy SHA-256 `014fc7d15a6e7162ce9dff47189a8a1f679986c603d10a61af8a9cda7874aaff` |
| Consume adapters omit null callback task diagnostics | 18 | 4 | ConsumeContextRetryContext SHA-256 `df88f0e3c8fe883359a061fba77d80a5c5fc498484cc1425b89f9e280d76dbed` |
| Retry child-namespace implementation becomes public | 1 | 1 | AllExceptionFilter SHA-256 `b0a67fa00843bfaed96cb5f0ba250cb13e5b65171d4ad81e7505252cbeeefe61` |
| One-parameter activity redelivery filter becomes public | 1 | 1 | ActivityRedeliveryRetryFilter SHA-256 `7b6457c3947b82669b2c89ff6b4f062f263ab84d5b2d6394ea9b5b5cb8610eda` |

Each change is applied separately, compiled successfully, executed against its
owning suite, and restored byte-for-byte. All selected tests that are not listed
as causal failures pass; no cases skip. Reports are under the correspondingly
named `/private/tmp/vsb-iteration119-mutation-*` directories.

The visibility results above use the corrected explicit Core assembly anchor.
The isolated old child-namespace guard first survives because IBus identifies
Abstractions, not Core. That seventh author-discovered assurance finding is
documented in the counterreview report; the adjacent DI and telemetry absence
guards are strengthened to both real foundation assemblies. Neither survivor
is represented as a killed mutation. The activity mutation retains an internal
constructor while exposing the type, so it remains compilable despite the
internal observer parameter; the failure comes from the export guard, not the
compiler. Activity comments are clarified manually before its corrected repeat.

The zero-growth counterchange is never tested with an unlimited retry budget.
The unsafe historical unlimited-allocation scenario is established statically,
not deliberately executed. These hand-selected counterchanges establish the
effectiveness of specific oracles; they are not a repository-wide mutation score.

## Remaining acceptance

The full strict Engineering build now passes with zero warnings and errors,
4m29.58s, after all mutations are restored. The separate corrected-source internal
counterreview confirms the local fixes but finds one P1 nested lifecycle-fault
ownership defect. Exact scope and accepted counterexamples are in
[INTERNAL_CORRECTED_SOURCE_REVIEW.md](INTERNAL_CORRECTED_SOURCE_REVIEW.md).
That finding must be fixed before final-source acceptance. A second intermediate
checkpoint secures this work without claiming completion or release readiness.

Final-source coverage/CRAP, remaining planned counterchanges, both formatting
checks, the complete strict Engineering build, all canonical hosts, package/API
contract inspection, repository-wide architecture/hygiene checks, and final
commit/tag/remote verification are still pending. Coverage is not claimed as
100%, and no absolute correctness or provider-wide execution claim is made.
Protected `review/` and `TestResults/` remain out of scope and unstaged.
