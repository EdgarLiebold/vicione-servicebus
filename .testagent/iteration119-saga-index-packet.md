# Iteration 119 — saga query/index integrity

## Research and authority

The original overall A+ goal remains active. Starting commit
`a582426c8aa17c3a582022ec87e4ba8f8bf3f1ed` and annotated saga-ownership tag
are verified on origin. The same hash-bound PO order/development slice remains
the authority. Protected review/results trees are not accessed or staged.

The lead personally reads the complete indexed dictionary/property/interface,
SagaInstance, SagaQuery, ISagaQuery, ISaga, IndexedAttribute and ReadPropertyCache,
plus relevant existing repository concurrency/integration/capability sources.
The previous provided parse-only pairing result is reused as heuristic research,
not rerun or mistaken for behavioral proof. All code/comments remain manual.

## Confirmed source diagnostics, not causal execution yet

- Bare indexed boolean member queries compile/invoke the whole predicate on null.
  Binary equality already scans. Stale mutable-key buckets cannot prove completeness.
- State-derived SagaInstance equality/hash can strand or collapse index memberships.
  Retain public wrapper equality but use exact wrapper-reference membership internally.
- Removal recomputes current keys; capturing a mutable reference key also does not
  freeze that key object's equality/hash. Missing/stale removal can evict or invalidate
  a different equal wrapper or a replacement.
- Later indexed getter/null-key failure can partially publish earlier indices.
  Duplicate correlation identifiers can make ID lookup ambiguous.
- Select defers enumeration beyond its lock; Where invokes predicates under that
  lock. Membership snapshots and callback execution must be separate.
- Reflection assumes concrete public CorrelationId; explicit ISaga and unsupported
  secondary properties need deliberate handling. SagaQuery lacks required expression
  validation; direct index callback/key/state boundaries need exact diagnostics.

First internal Sol advice explicitly discloses incomplete governance reading; it
is advisory source diagnosis only, not formal accepted counterreview. The reviewer
must finish its own mandatory reads and reissue advice with hashes and RELEASE.
Main has independently completely read the instructions and selected source.

## Acceptance checklist and manual implementation plan

1. Evaluate bare boolean, equality, null, non-indexed and mutable-key predicates
   against actual referenced state, never against null.
2. Snapshot membership before user predicates/transformers; evaluate callbacks
   outside owner locks and materialize results. Callback dictionary mutation must
   not corrupt current enumeration or admit future membership to an old snapshot.
3. Keep exact wrapper references independent of mutable state equality/hash.
   Repeated same-wrapper Add is idempotent without repeated getters. Reject another
   wrapper's occupied correlation ID without changing existing membership.
4. Remove only registered references using captured keys; stale/missing/equal-other
   wrappers cannot remove/invalidate replacements. Deliberately support nullable
   indexed values rather than partially publishing a failure.
5. Stage all getter results before publication and safely unwind failed admission.
   Getter staging is not universal transactionality: comparer/allocation commit
   faults require safe ownership/unwind or an explicitly recorded limitation.
6. Preserve direct property-index key APIs and distinct-key Count. Standard stable
   keys can retain a hashed fast path; unknown mutable keys need a correctness
   fallback over canonical captured memberships, not cloning or stale hashes.
7. Canonical Guid comes from ISaga, including explicit implementations. Validate
   readable instance non-indexer secondary properties. Define correlation mutation
   semantics explicitly; captured IDs ensure cleanup but not immutability.
8. Required expression/delegate/state/key/property inputs fail before effects with
   exact diagnostics. Read/understand then manually rewrite all changed comments,
   including reference, registration-key and snapshot limitations.

Write exact manual tests and requirement tuples against the secured baseline first.
Await every owned process/reviewer freeze before executable edits. Strict owning
build and native causal red precede coherent source correction. Focused/new and
prior ownership/capability/integration tests remain mandatory. Compile/exercise
single-cause predicate/reference/original-key/staging/duplicate-ID/snapshot mutants,
restore accepted bytes, then final strict full Core, scoped format/fresh coverage,
proportional API/package gates and normal commit/annotated-tag/atomic push.

No source/test/comment generator, arbitrary state clone, second persistence truth
or compatibility alias. No whole-source/whole-product/API/provider/overall A+
acceptance from this bounded packet. Adjacent saga acquisition cancellation,
provider Undo/dispatch cleanup, timer and final global gates remain active.
Record results only after owned process termination.

## First observed causal baseline

The strict owning-project build exits 0 with zero warnings/errors (84.95 seconds).
Thirty-two manually authored methods contribute 50 native cases; the requirement
catalogue has 2,896 unique tuples. The complete focused baseline exits 2 with
43 failed / 7 passed / zero skipped. Failures are real null-expression/null-filter/
predicate, reference/key/hash/removal, duplicate-ID, getter publication, correlation
invariant and snapshot/lock assertions, not fixture type-load failures.
The cross-thread callback case cancels only its own bounded observation and drains
the actual observer after the source lock unwinds; no native task is abandoned.

Raw build and CTRF/log are under the existing exact temporary artifact root with
`index-baseline-causal-*` names. No product source changes precede this red run.
The internal reviewer subsequently attests complete mandatory governance reading,
re-reads all five unchanged source files, confirms entry/exit hashes and explicitly
releases its freeze. Its renewed advice supersedes the earlier qualified observation,
but remains static internal advice, not executed mutation/cloud/external acceptance.

## First coherent correction and connected contract refinement

All seven affected/new source files are written manually after their full read.
The first strict correction build exits 0 with zero warnings/errors (50.06 seconds).
An expanded native run includes all 50 new index cases, all 82 ownership cases and
the existing concurrency/capability/classic-saga integration classes: 153/153 pass,
zero failures/skips. No prior capability is deleted to obtain the green result.

Canonical Guid comes from ISaga; registration/publication stages all getters before
locked apply, with local reverse rollback and primary-first cleanup aggregation.
Reference membership never calls mutable SagaInstance equality/hash. Standard
immutable key types retain hashed buckets; unknown mutable key types use captured
membership snapshots and current key equality. Direct key APIs retain registration-key
semantics and distinct-key Count. Ordinary queries evaluate actual current state over
the canonical membership snapshot outside owner locks. No state clone is introduced.

Correlation identifiers are deliberately stable while registered. Lookup/Where/Select
validate before results, including after user callbacks; reference removal still
cleans up the original captured identifier after illegal state mutation. Dictionary
operation capacity is bounded to one, so an unowned/repeated Release cannot silently
create extra concurrent owners. Pending dictionary admission/removal prevents reentrant
same-wrapper staging or reinsertion while invalidation happens outside owner locks.

The connected refinement removes the inappropriate notnull constraint on the public
key type: nullable value types are now directly constructible in normal C#, not only
through reflection. Stable hashed buckets use already-boxed key objects, avoiding an
additional box on lookup. Add/Remove wrapper parameters on the public property index
and its interface are unified to instance; no compatibility alias is added. Direct
property admission also rejects same-wrapper reentrancy before repeating its getter
and releases its pending record on every outcome. Six further manual methods add
nine cases for nullable/nonnullable keys, bounded release, invalidated registration
and safe reentrant retry; total 38 methods/59 new cases, catalogue 2,902 tuples.
The refined build log reports zero warnings/errors and the complete native report
contains 162 passing cases, but the original live session handles were lost when
tool output was truncated during context handover. Exact process inspection confirms
both hosts have terminated; their logs/reports are retained, not attributed an
unobserved process exit code. An unchanged repeat subsequently provides observed
strict build exit 0 (3.78 seconds, zero warnings/errors) and native exit 0:
162/162 passing cases, zero failures/skips (3.291 seconds). Its distinct raw names
are `index-recovered-observed-*`; no executable source changes occur between runs.

The lead personally rereads all seven source files and all 38 new test methods.
A read-only inventory counts 181 direct assertion calls, with no assertion-free
method. Reference-identity assertions verify the retained state/snapshot contracts;
equal-wrapper arrange checks deliberately establish the hostile state-equality case,
not tautological output checks. The synchronous cross-thread callback observation
is a lock-boundary oracle with bounded cancellation and a drained actual task,
not an unawaited asynchronous production operation. Final assertion categorization,
compiled mutations and native full-suite evidence remain required.

A fresh internal Sol reviewer holds the current eleven-file code/test/catalogue
snapshot. It completes its own mandatory instruction reads and will explicitly
release the freeze before any executable correction. Scoped whitespace verifiers
also run read-only. A connected suspected query boundary is investigated: correlation
immutability applies while a wrapper is registered, not after legitimate retirement;
query identifier results must use original registration keys rather than mutable
state identifiers read after callbacks.

## Connected causal boundary run and correction

The reviewer releases the byte-identical eleven-file freeze and reports two concrete
medium-severity source defects: retirement is confused with continuing registration,
and both repository queries discard captured IDs before their final live-state
projection. Its metadata/allocation observations remain static until exercised.
This is internal source advice only; it has no fresh architecture-revision rationale
read and grants no external/product/provider/global acceptance. The lead independently
disposes the bounded source defects within the unchanged authoritative slice.

Thirteen further manually written methods, direct foreign-property metadata and
their exact requirement tuples expand the new packet to 51 methods/86 cases;
catalogue 2,915 unique tuples. The strict unchanged-product build exits 0 with zero
warnings/errors (37.81 seconds). Native execution exits 2: 20 failed/66 passed,
zero skipped (1.782 seconds). Each failure is a concrete predicate-retirement,
required-input precedence, interface/override getter staging, duplicate canonical
getter, or property-specific construction diagnostic assertion. There are no
fixture type-load failures, skipped tests or discarded live test hosts.

The coherent manual correction uses the same pair snapshot for public wrapper
results and internal captured-ID projection. A changed current ID is rejected only
after confirming that exact wrapper reference is still registered; a removed old
wrapper cannot be confused with a same-ID replacement. State getters continue to
run outside owner locks. A positive direct-invalidation regression distinguishes
wrapper invalidation from real dictionary removal; no IsRemoved shortcut is used.

Supported inherited/interface property metadata is discovered with the framework's
inherited-property attribute API and resolved implementation getter identities.
Getter identities include module, token and declaring closed type, not member names
or mutable state hashes. The canonical ISaga getter is excluded from duplicate
secondary admission. Static/indexer/write-only/ref-return/ref-like members fail before
generic construction with their exact member diagnosis. Direct property tests retain
base/interface dispatch and same-name different-type member semantics.
The framework distinction is verified against the primary
[MemberInfo.GetCustomAttributes documentation](https://learn.microsoft.com/en-us/dotnet/api/system.reflection.memberinfo.getcustomattributes?view=net-10.0):
property inheritance requires the Attribute overload rather than the old provider
call whose inherit flag does not traverse property ancestors. No shared reflection
helper is mechanically rewritten or claimed fully reviewed from a selected call site.

Both repository query entries retain captured registered IDs. Required query and
message-pipeline inputs are checked before cancellation/acquisition. Public wrapper
parameters are uniformly instance, with four exact manually changed packed-contract
rows and no compatibility aliases. The added direct-invalidation positive test brings
the final declaration to 52 methods/87 cases and 2,916 tuples. The new correction
build is running; passes, actual mutations and final coverage are still awaited.
The existing no-await readonly QueryAsync implementation is deliberately not silently
replaced with different synchronous-fault/cancellation semantics in this correction;
its completed-task/fault/cancellation contract belongs to the connected generic
query cleanup still required by the overall goal. The earlier handover's CS1998
suppression description is not accepted as evidence: selected configuration reads
find no such suppression, and read-only default-configuration evaluation reports
NoWarn 1701;1702, WarningLevel 10, TreatWarningsAsErrors true and enabled NET analyzers.
The observed correction build exits 0 with zero warnings/errors (52.92 seconds),
and the expanded native host exits 0: 190/190 passing cases, zero failures/skips
(2.566 seconds). Two obsolete LINQ using directives are manually removed after
that host terminates; final compilation/replay must bind the resulting bytes.

## Frozen correction review and empirical filter gap

The clean strict snapshot build exits 0, zero warnings/errors (8.56 seconds).
Full native Core exits 0: 3,997/3,997, zero failures/skips (21.544 seconds), including
all 87 new cases and the compiled 2,916-tuple requirement projection. Source-only
loaded-graph coverage is 49,599/61,163 lines (81.0931%) and 17,053/23,268 branches
(73.2895%). These are one loaded Core graph, not whole-product/provider coverage.
The unexercised registration Apply-failure/Rollback allocation paths remain disclosed.

Internal Sol rereads all nine source/four test files, all 52 relevant catalogue
tuples and four intentional packed rows; entry/exit all fifteen hashes match and
it explicitly RELEASES. It finds no further concrete source deviation in the coherent
correction. Its remaining static test hints are keyed filter selectivity, direct
property value materialization, closed-generic getter identity, inherited default
interface getter dispatch and pointer/function-pointer construction diagnostics.

The keyed-filter candidate is actually injected alone: the keyed Where returns its
bucket without applying its required filter. Strict build exits 0, zero warnings/
errors (52.34 seconds); all 30 existing direct Property cases still pass natively
with observed exit 0 (1.801 seconds). This is an empirical survivor, not a guessed
gap or a claim that the otherwise strong test suite is globally weak. Source is
manually restored to exact accepted SHA e3e1ee47e51382b1286cdc6e20728fd4b5d7aea01ae35e01b0d7c379b632712b.

Two manual positive/negative keyed-query/value-materialization methods and two
closed-generic/default-interface theories, plus the two unsupported pointer datasets,
add eight cases and four exact manual tuples. Packet declaration is 56 methods/
95 cases, catalogue 2,920. The green replay is awaited. Reinject the exact same
keyed-filter candidate after the new test passes; credit a kill only from its direct
state/filter assertion after an individually observed successful mutant build.

The observed strengthened replay exits 0: 95/95 native cases, zero failures/skips
(1.919 seconds). Two further manually designed admission oracles remove ambiguity:
an empty dictionary must remain unpublished after a late captured-key getter fault,
and a getter changing the captured identifier must diagnose the admission phase,
not falsely claim that the rejected wrapper was already registered. Both also prove
successful retry with the same wrapper. The second test is intentionally written
before its small diagnosis correction; actual causal results remain awaited.
Declaration is now 58 methods/97 cases and 2,922 exact requirement tuples.

Admission causal strict build exits 0, zero warnings/errors (33.79 seconds);
native two-case replay exits 2: the empty-dictionary fault oracle passes and the
admission diagnosis assertion fails specifically on "while registered" versus
"during registration" (one pass/one failure, zero skips, 1.703 seconds). The private
admission validator and its captured-ID argument are manually named for that phase;
only its diagnosis changes. The retained-registration validator and its semantics
remain separate and unchanged. Green replay is required before freezing/mutation.

Correction strict build exits 0, zero warnings/errors (8.24 seconds). Expanded
native saga replay exits 0: 200/200, zero failures/skips (2.861 seconds), including
all final 97 new cases. Internal Sol's final bounded delta review independently
reads the new tests/source/catalogue in its retained complete file contexts and
verifies all fifteen own entry/exit hashes unchanged: explicit RELEASE, no new
concrete finding. It performs no builds/mutations and claims no external acceptance.

All twelve individually compiled one-cause candidates M00–M11 are now actually
killed by direct functional tests, twenty failing native cases in total, zero skips.
Every mutant owner build and native host is observed to terminate before manual
restoration; Property restores to e3e1ee47e51382b1286cdc6e20728fd4b5d7aea01ae35e01b0d7c379b632712b,
Dictionary to 035823eaa435ceca70c5eddd543dd2e18ff9dc2350f2e382e4a25aa5fde31b90.
The earlier M00 survivor remains explicit historical evidence of the repaired gap.
No compile failure or arrange-only technical fault is credited as a mutation kill.

The twelve candidates cover keyed filter bypass, registration-map value equality,
bucket value equality, recomputed removal keys, mutable key-object hash indexing,
premature publication during capture, false invariants on retired references,
live rather than captured query IDs, deferred transformation values, unlimited
dictionary lease capacity, folded closed-generic getter identity, and ignored
Boolean predicate results. Final clean build/full native coverage and package/
API/format verification still need to bind the exact restored accepted bytes.

Final strict owner build exits 0, zero warnings/errors (9.69 seconds). Complete
native Core exits 0: 4,007/4,007, zero failures/skips (24.847 seconds), all97 packet
cases passing and compiled2,922-tuple projection. Source-only loaded graph is
49,603/61,163 lines (81.0997%), 17,058/23,268 branches (73.3110%); not whole product.
Dictionary direct class89.6825/87.5%, Property92.5926/90%, Registration61.5385/0%.
Rollback branches remain explicitly unexecuted. Product scoped whitespace exits0
with the known workspace-load warning; Unit/package gates remain awaited.

Unit scoped whitespace also terminates exit0, same known generic workspace-load
warning, no writes. Fresh package/18journey/3isolated-consumer/30runtime-API gate
is running without lock/API update flags; reflected inventory goes to exact
temporary output, not a product source/test/comment generator. Only the four
manually reviewed public wrapper-parameter rows intentionally change the contract.

Fresh whole package script actually terminates exit1 at final API comparison, after
all31packs/checks,18journey execution,3isolated consumers and30runtime reflections.
All consumer builds report zero warnings/errors. Lead completely reads388diff lines;
internal Sol separately maps57omitted public blocks to already-committed internal
declarations and55bound source owners matching HEAD; public feature facades remain.
One paired sealed row represents nested SplitFilterPipeSpecification under the
collector's flawed generic nested naming, not a sealed outer PipeConfigurator.

Concrete collector finding: FormatType strips everything from the first generic
backtick, losing nested member names/arity ownership. Do not blindly regenerate/
accept a contract retaining that ambiguity. Coherent naming formatter/direct oracles/
reviewed baseline reconciliation follow this intermediate normal Git backup; the
whole-package/API gate remains explicitly unpassed. No source/test generator, fake
cmp0, successful whole-script replay, globalA+ or provider claim is made. All16canonical/
59raw bindings are checked; the failed log and diagnostic snapshot are immutable.
