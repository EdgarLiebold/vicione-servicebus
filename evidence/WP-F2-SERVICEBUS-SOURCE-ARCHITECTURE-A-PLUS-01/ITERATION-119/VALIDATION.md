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

## Nested lifecycle ownership follow-up — in progress

The second intermediate checkpoint and its peeled annotated tag are verified on
origin at `2968483f0a085cbb9a8362a8d6828b5c84dd9654`, without force. This is a
backup of the reviewed corrections, not a completed-iteration acceptance tag.

The unchanged checkpoint executes 50 RetryFilter cases: 35 existing cases pass
and all 15 new nested cases fail, with zero skips. The new matrix covers creation,
post-fault, pre-retry, completion, and terminal observation with synchronous
throws, faulted tasks, and genuinely pending tasks. Oracles check exact exception
identity, business attempts and committed effects, both policy disposals, exact
inner events, and absence of outer business-fault notification. Pending cases
use entered/release barriers and drain their operations in finally blocks.
The CTRF is `/private/tmp/vsb-iteration119-nested-observer-red/nested-observer-red.ctrf.json`.

The first scoped correction records only exact lifecycle exceptions in an
internal context payload. Reference-counted leases retain ownership across nested
calls and clear it when the last active operation ends. Dedicated catch filters
propagate ownership before business classification. No RetryContext is fabricated
for an observer failure and no public API is added. This correction compiles with
zero warnings/errors and passes all 50 cases without skips in
`/private/tmp/vsb-iteration119-nested-observer-green/nested-observer-green.ctrf.json`.

Further independent-context projection and same-context/same-exception reuse
oracles are being checked. Nested policy callback faults and independent policy
cancellation are also tested rather than assumed to share observer semantics.
Their checks, recognition-removal counterchanges, repeated scoped review, and
the final repository gates remain pending. The nested finding is not yet closed.

The next selection executes 64 cases: 61 pass and three causal cases fail.
Independent outer/inner context projections and later same-context/same-exception
business retries all pass. Both synchronous/faulted-task policy preparation
callbacks and nested independent-policy cancellation still restart business work.
The artifact is `/private/tmp/vsb-iteration119-nested-boundary-red/nested-boundary-red.ctrf.json`.

Real typed command dispatch, including replacement initial/retry contexts, extends
the five-phase matrix by ten cases; they all pass. Two independent source/policy
delay cases add deterministic injected-clock timer-entry and synchronous timer
disposal oracles, without advancing time. The 76-case pre-correction selection
records 71 passes and five causal failures: the preceding three plus a retained
timer after source cancellation and business restart after policy cancellation.
Finally blocks cancel/drain all operations. The artifact is
`/private/tmp/vsb-iteration119-nested-dispatch-timer-red/nested-dispatch-timer-red.ctrf.json`.

The updated correction treats the complete delay/preparation stage as lifecycle
work, links both cancellation sources before the delay, preserves the original
cancelled token, and marks the final propagated exception after normalization.
Fault callbacks also use lifecycle ownership. All code, comments, fixtures and
seven new method requirement tuples are written manually. A separate internal
read-only corrected-source counterreview and the new build are in progress;
no completed acceptance is asserted.

The corrected extended suite now passes 76/76 with zero skips. Product formatting
verification exits successfully; its existing workspace-load warning is not a
format finding. Full Core coverage is running. The completed separate internal
review confirms the original ordinary in-memory nested observer correction but
accepts four adjacent open findings: P1 redelivery composition, P1 cleanup replay,
P2 infrastructure classification/admission ownership, and P2 stale terminal
business ownership on reused contexts. Their counterexamples and exact review
scope are in [INTERNAL_NESTED_OWNERSHIP_REVIEW.md](INTERNAL_NESTED_OWNERSHIP_REVIEW.md).
All four remain unclosed before this intermediate checkpoint.

The complete current Core host passes 3,709/3,709 with zero skips. This is an
intermediate-checkpoint result, not closure of the four open review findings or
full-provider acceptance. Artifacts:

- `/private/tmp/vsb-iteration119-nested-checkpoint-core/nested-checkpoint-core.ctrf.json`,
  SHA-256 `e64d7b07c19ce35bbbab384b2575770bf02405edb4e9bfa8fec9d1c2991936c7`.
- `/private/tmp/vsb-iteration119-nested-checkpoint-core/nested-checkpoint-core.cobertura.xml`,
  SHA-256 `9f9a5c08f590a204286241bb33ffd4f4c64dc503fcda52799536b5ff97a15ef2`.

Read-only coverage/CRAP analysis and the separate ownership counterchanges are
in progress. The prior strict Engineering/canonical/provider package results
predate the new lifecycle coordinator and are not final-source acceptance.

## Coverage denominator finding — author-discovered, remediation in progress

The default-profile Core artifact reports 49,537/63,847 measured lines
(77.5870%) and 17,301/24,603 measured branches (70.3207%). Those are instrumented
graph values, not complete-source or full-provider coverage. In particular, the
RetryFilter SendAsync/AttemptAsync and both redelivery SendAsync state machines
are absent from the artifact. Only RetryFilter preparation/nested propagation
state machines are present. Default-profile CRAP cannot therefore assess the
omitted critical operations and must not be represented as their risk score.

Microsoft documents built-in attribute exclusions, including
DebuggerNonUserCodeAttribute, and that empty exclusions retain the defaults
unless mergeDefaults is explicitly false. See the
[official coverage configuration](https://github.com/microsoft/codecoverage/blob/main/docs/configuration.md#merging-with-the-built-in-default-lists).
The omitted operations carry that attribute. This is a coverage-completeness
finding, not evidence that their meaningful behavioral tests were never run.

The author writes `tools/ci/coverage.settings.xml` manually. It excludes test
assemblies, includes auto-properties, and explicitly disables built-in attribute
exclusions without adding replacement source-code exclusions. Future coverage
runs must pass `--coverage-settings tools/ci/coverage.settings.xml`. Source
debugger behavior and product functionality are unchanged; no generator is used.
The profile includes product source paths under `src` on both Unix and Windows;
shared test-support sources outside that tree are not product coverage. It does
not make unloaded provider assemblies execute, so all-host/provider acceptance
and measurement-completeness checks are still required.
An unchanged-accepted-source repeat must verify the critical operations appear
before reporting complete instrumented-source risk. The expanded denominator
will not be numerically comparable with the historical default-profile values.

## Nested ownership controlled counterchanges — in progress

- Separate recognition removal compiles with zero warnings/errors and executes
  76 cases: 39 causal failures, 37 passes, zero skips. The exact accepted
  RetryLifecycleFaults SHA-256 is restored to
  `89326d7d2e945c884d814f3d0aa0a1ea67416f523db94246fc82bd93f4c39d39`.
- Separate lease-release omission compiles with zero warnings/errors and executes
  76 cases: precisely both later-business reuse variants fail, 74 pass, zero skip.
  The same accepted source hash is restored independently.
- Independent source-cancellation omission during the delay is being checked
  separately. Its source restoration and accepted-source repeat remain pending.

The first two CTRF artifacts are
`/private/tmp/vsb-iteration119-mutation-lifecycle-recognition/lifecycle-recognition.ctrf.json`
and `/private/tmp/vsb-iteration119-mutation-lifecycle-retention/lifecycle-retention.ctrf.json`.
No repository-wide mutation score is claimed.

The separate delay-token counterchange now compiles with zero warnings/errors
and executes 76 cases: exactly the independent source-cancellation timer case
fails, 75 pass, zero skip. RetryFilter is restored byte-for-byte to
`f35da80e74682bd507bebb4db9f63ab2e1f5ac502e269132d9e083c226dfc7f8`.
Its CTRF is `/private/tmp/vsb-iteration119-mutation-delay-source/delay-source.ctrf.json`.
All three counterchanges are separate, compilable, causally killed and restored.
The accepted-source rebuild and explicit-profile Core repeat are pending.

## Executed complete-profile checkpoint verification

The restored-source build passes with zero warnings/errors. The explicit-profile
Core repeat passes 3,709/3,709 with no skip, using the frozen accepted RetryFilter
and coordinator hashes above. Both Product and Unit formatting verifications
exit successfully, and Git whitespace verification passes. The existing format
workspace-load warning is not a source formatting finding.

The XML profile is validated by actual collection: all four previously omitted
RetryFilter Send/Attempt and ordinary/activity redelivery Send state machines
are present. Coverage graph values are 49,210/60,856 lines (80.8630%) and
16,818/23,068 branches (72.9062%). This src-scoped, loaded-assembly Core graph
is still not the entire multi-provider product. No source attribute or
auto-property is excluded by this profile.

The read-only owner cut contains 80 source paths across Core and Abstractions,
including the new coordinator. Select `.cs` paths matching Retry, Rescue,
TechnicalFailure, CompositeExceptionFilter, CompositeFilter, CompositePredicate,
or IExceptionFilter; exclude `/Providers/`, `/Configuration/Composite` and
`/Configuration/Redelivery`. Sixty-five paths contain instrumentable methods;
the other fifteen are interfaces, delegates or an enum, not executable methods.
Owner counts are 1,510/1,821 lines (82.9215%) and 632/824 branches (76.6990%).
Neither these values nor the changed graph denominator are directly comparable
to the earlier default-profile cut.

| Critical operation | Complexity | Measured line coverage | Measured branch coverage | CRAP |
|---|---:|---:|---:|---:|
| Ordinary redelivery Send | 44 | 69.8413% | 56.8182% | 97.1061 |
| Activity redelivery Send | 32 | 65.3846% | 50.0000% | 74.4725 |
| Retry Attempt | 26 | 80.4878% | 76.9231% | 31.0219 |
| Retry Send | 26 | 89.1304% | 80.7692% | 26.8681 |
| Retry preparation | 16 | 100.0000% | 93.7500% | 16.0000 |

CRAP uses `complexity² × (1 − measured line fraction)³ + complexity`, read-only
Ruby analysis of the emitted Cobertura because pwsh is unavailable. No source,
test or comment generator is used. The newly visible redelivery risk hotspots
must be addressed with the four open review findings before owner acceptance;
default-profile risk values did not measure these operations.

Accepted complete-profile artifacts:

- `/private/tmp/vsb-iteration119-complete-profile-core/complete-profile-core.ctrf.json`,
  SHA-256 `5b8dc355c958bad31dbe392d288bf2e61ce3bb3998c291d2ee0a7f22f11130d6`.
- `/private/tmp/vsb-iteration119-complete-profile-core/complete-profile-core.cobertura.xml`,
  SHA-256 `174f63e530be4f0cdb5732568647c437dfa071d7aa58f4d8f7234cd420690389`.
- `tools/ci/coverage.settings.xml`,
  SHA-256 `3838fc1b6b73f21d8fb447beecad11a5c91cca053bd6c31f906a6036f248464d`.

The denominator finding is corrected for this actual repeat. All-host/provider
coverage, future completeness guarding, the four NN findings, remaining planned
counterchanges, canonical/strict Engineering/package/API gates and final
iteration publication remain pending. The next tag is explicitly intermediate.
