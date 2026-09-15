# Iteration 119 internal counterreview

Status: remediation required; not final A+ acceptance.

The separate read-only reviewer inspected checkpoint
`97c1b364bf37ed48387373f5feee3e23f065fd27` against baseline
`ba585cf4a89af198b3154c22f35b1875352f78a5`. This is an internal counterreview,
not an independent external acceptance or an external product-team verdict.

## Complete reviewed delta

The tracked `src` and `tests` delta contains 75 production/project paths and
13 C# test/helper paths. The reviewer read those files completely: 5,357
production/project lines and 3,497 test/helper lines. Three requirement-manifest
deltas were inspected. The manifests themselves and unchanged provider suites
were not claimed as completely read by this reviewer.

Additional complete reads covered the public Retry factory, host retry
configuration, observer and consume-pipe construction, Connectable,
SplitFilter/PipeConfigurator, consume-retry contracts, activity scopes,
redelivery specifications, and existing message-retry/specification tests.
Remaining provider call sites were symbol-traced, not claimed as complete reads.
No reviewer tests or mutations were executed. Protected review/results trees
were not inspected or changed.

## Findings

1. P1: RetryFilter creation and completion observer failures enter business
   retry classification. A completion observer can fail after a successful
   operation and cause that committed operation to run again. Creation failure
   can likewise permit downstream execution after unsuccessful observation.
   Required evidence: synchronous and pending asynchronous observer failures,
   exact failure identity, business/effect counts, and exactly-once disposal.

2. P1: ExponentialRetryPolicy admits positive sub-millisecond intervals but
   truncates them to integer milliseconds. Positive bounds become zero; a
   truncated increment may prevent eager schedule construction from reaching
   its cap. The public no-limit overload uses Int32.MaxValue. That unsafe
   allocation scenario was established statically and was not executed.
   Required evidence: tick-precision minimum bounds, positive increments,
   cap convergence, and bounded schedule construction.

3. P1: RetryFilter passes the default token to PreRetryAsync. Cancellation
   cannot release custom pending callback work that uses this argument.
   Required evidence: callback-entry barriers, source and policy/bus
   cancellation, exact source-token identity, no extra operation, and disposal.
   No observer API expansion is necessary: observer contexts expose cancellation.

4. P2: ConsumeContextRetryPolicy leaks an acquired disposable policy context
   if nested-context validation, projection, or representation fails before
   ownership transfers. Required evidence: typed/untyped missing contexts,
   projection throws/null output, and exactly-once disposal.

5. P2: Typed and untyped ConsumeContextRetryContext adapters directly await
   underlying callback results. Null results become NullReferenceException
   instead of the explicit InvalidOperationException reported by bare retry
   execution. Required evidence: both projections and both callback boundaries.

6. P2 assurance: the visibility guard uses activity-filter generic arity two
   instead of one and excludes RetryPolicies child namespaces. Both currently
   internal implementation types can be made public without that guard failing.
   Required evidence: correct namespace/arity closure and both killed visibility
   counterchanges.

No causal defect was established in the changed rescue projections or shared
Split validation correction. Their identity and validation-order assertions are
useful evidence, not a proof of absolute correctness or repository-wide coverage.

## Author disposition

All six findings are accepted for causal regression, owning-boundary correction,
counterchange validation, and corrected-source review within Iteration 119.
Acceptance remains open until the applicable final-source gates pass.

## Red-first author execution

The unchanged product source executes 57 directly selected cases: 39 pass and
18 fail, without skips. Every failure belongs to one of the accepted production
counterexamples. Report:
`/private/tmp/vsb-iteration119-counterreview-red/counterreview-red.ctrf.json`.
The unsafe unlimited-allocation example is not executed. Both exponential
regressions use bounded budgets; they distinguish fractional bounds and positive
growth without attempting a dangerous allocation.

Initial test compilation identifies authoring errors, not product failures:
invariant generic policy contexts, duplicate primary-constructor parameter
capture, and missing test-run cancellation arguments. All are corrected
explicitly without analyzer suppression; the red-first host builds with zero
warnings and errors before producing the 18 accepted failures.

The corrected-source cancellation fixture uses independent source and policy
tokens. Its oracle verifies cancellability and exact terminal source/policy
identity, not an implementation-specific linked-token identity.

## Corrected-source author execution

The serial Unit build completes with zero warnings and errors. The corrected
three-class selection passes 57/57, then 59/59 after adding typed/untyped
representation-failure evidence. The latter checks exactly-once nested disposal
and removal of the cancellation registration: cancellation after failed
construction must not call the disposed nested policy.

Reports:
`/private/tmp/vsb-iteration119-counterreview-green/counterreview-green.ctrf.json`
and
`/private/tmp/vsb-iteration119-counterreview-green-v2/counterreview-green-v2.ctrf.json`.

The completion-observer counterchange moves observation back inside business
classification. Exactly its three causal cases fail among 35 RetryFilter cases:
two synchronous/faulted-task cases and one pending asynchronous case. Attempt
counts become four instead of two. The creation-observer counterchange likewise
kills its three owning cases, with 32/35 passing. Each counterchange is separate;
the accepted RetryFilter bytes are restored between them to SHA-256
`78a85794cd11d54bf64f906cd1a45c9dfb11dc3c6a42ce15c36e6fc0b8b792fd`.

These focused successes and counterchanges do not replace final-source canonical,
coverage, package/API, formatting, and repository acceptance. Those remain open.

## Additional author finding from mutation execution

The first visibility counterchange survives. An isolated child-namespace
counterchange also passes 1/1 against the old guard. The actual cause is broader
than generic arity and namespace selection: `typeof(IBus).Assembly` identifies
Abstractions, not Core. The guard does not inspect Core exports at all.

The author corrects the guard to the existing `ProductAssemblyFacts.Core` anchor
and asserts its exact Core assembly identity. Both visibility counterchanges
must be repeated against that corrected guard. The initial activity attempt
overlaps the start of the next build and is not claimed as an isolated proof;
the following isolated child-namespace survivor independently establishes the
false-confidence defect. Subsequent counterchanges do not overlap builds or
source restoration with an active test process.

A scoped search finds the same obsolete anchor in the dependency-injection
absence guard and the telemetry API absence guard. Those checks must inspect
both Core and Abstractions. The foundation assembly catalogue and the Core
removal boundary already anchor the two real assemblies correctly and do not
need changes. This is an author-discovered assurance finding, not an additional
finding attributed to the separate reviewer.

The corrected child-namespace and activity visibility counterchanges each fail
1/1, separately, with the exact mutated Core family in the leak collection.
Both accepted source files are restored byte-for-byte. Reports:
`/private/tmp/vsb-iteration119-mutation-child-visible-corrected/child-visible-corrected.ctrf.json`
and
`/private/tmp/vsb-iteration119-mutation-activity-visible-corrected/activity-visible-corrected.ctrf.json`.
The activity constructor is temporarily internal during its public-type mutation
to keep the counterexample compilable with the internal observer parameter.
The corrected activity source hash is
`7b6457c3947b82669b2c89ff6b4f062f263ab84d5b2d6394ea9b5b5cb8610eda`.
