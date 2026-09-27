# A+ remediation test plan

## Current T47 — one combined implementation and measurement packet

Use [t47-saga-journeys.md](coverage-a-plus-20260921/t47-saga-journeys.md) for the
acceptance map and current results. Callback ownership, pending factory/send,
primary failures and failed compensation map to
`StateMachineCallbackJourneyIntegrationTests.CallbackJourney_PreservesOwnershipAndCompletesOnlyTheSelectedOutcomeAsync`.
Request-generation isolation and real Quartz cleanup map to
`QuartzSagaRequestGenerationIntegrationTests.PreviousRequestMessages_CannotCompleteOrCancelTheNextRequestOfTheSameSagaAsync`.
Existing request/timeout suites provide complementary multi-response, missing-ID
and real expiration coverage; do not recreate their overload matrices.

Finish implementation and the combined read-only review, verify focused controls
and single-cause counterprobes, restore, then perform one full33 exact-commit
measurement and independent numerical audit. Update changelog and canonical
CHANGELIST, commit and use the already-authorized push. T47 remains open until all
these gates finish; the continuation and iteration records below are historical.

## Current continuation, 2026-09-26

First reading is complete under the PO's Git baseline plus the 91-file manual
read recorded in [source-read-completion.json](source-read-completion.json).
The current remainder is empty. Use [status.md](status.md)
for the current checkpoint, explicit open work and evidence limits.

1. Completed product corrections include ActiveMQ temporary reply ownership and
   typed destination caches, one-time setup publication, graph repeat/self-edge
   validation, SIMD padding and SQL Server principal-kind/SID validation with
   effective permission checks. Their regressions, counterchanges and provider
   evidence are recorded in status.md; these are not pending investigations.
2. The complete baseline at `98ac8bb78` has 33 profiles, 32 measured assemblies,
   12,522 executions and no method with CRAP above 30. The source/PDB denominator
   audit accounts for all 4,132 compiled product C# files. Retain its explicit
   distinction between measured files and files without visible sequence points.
3. The final candidate `2464cdc45` adds fourteen contract cases for EF inbox
   failure persistence and terminal winners, in-memory cancellation and job
   schedule validation. Product sources remain byte-identical to the baseline.
   All 33 final profiles passed with 12,536 executions. The complete aggregate
   records 90.6209% lines, 83.2089% conservative branches and zero CRAP > 30.
4. Preserve the failed Azure cleanup measurement and its isolated diagnostic
   pass. The original cleanup cancellation was not reproduced and its cause
   remains unproven.
   The complete Azure recheck passed 30/30 with unchanged timeouts and assertions.
   The initial Unit/Architecture run passed 10,836 of 10,837 cases. The missing
   generated NuGet/MSBuild imports were restored successfully; the complete
   architecture recheck passed 445/445 without skips.
5. Identity, exact-count aggregation, method-level CRAP inventory and CHANGELIST
   checks passed. The final adversarial numerical/integrity review also passed;
   publish the reconciled documentation. Existing package and vulnerability evidence retains
   its original commit and source/configuration-equivalence qualification.
6. Before claiming A+ completion, review and explicitly dispose the union of
   4,540 line-gap method identities and 1,532 additional conservative branch-gap
   identities. Prioritize product behavior and failure consequences, reconcile
   complementary profile paths, and use strong assertions and counterchanges.
   Start with nested RabbitMQ consume bindings and activity transformations;
   do not treat zero CRAP hotspots as closure of these gaps.
7. After coverage/CRAP completion, use Roslyn to inventory the complete API and
   associated XML comments across all repositories, then assess contracts,
   consistency and documentation. That follow-up is not covered by ServiceBus
   test results or by its existing package API baseline.

The numbered iteration plans below are historical records, not the current
first-read balance or an A+ acceptance statement.

## Iteration 132 connected ownership and liveness package

1. Complete main FULL shared support/graph reading and exact sorted Git/read/parser
   admission before new Core tests. Accepted at secured input 3e4eae03435f3b7343bb63a1eac66eeca2269139:
   557 Core inputs plus 43 support inputs and effective graph, 621 distinct paths.
2. Manually implement and inline-review these focused requirement mappings:
   - CS01 -> TimerCreationFailure_ReleasesAllocatedOwnershipAndPreservesTheOriginalException
     (linked and unlinked lifetime rows), plus
     SuccessfulConstruction_ForwardsCleanupPolicyAndDisposesItsTimerExactlyOnceAsync.
   - MD01 -> ContentTypeMutation_IsIsolatedAcrossReadsDeliveriesAndMessageContractsAsync;
     remove the unused global mutable holder and return a fresh canonical MIME value.
   - H01 -> SnapshotFilter_DoesNotBlockAConcurrentProducer; bounded join while the
     predicate is active, captured producer fault and final post-predicate join.
   - H02 -> ConnectPublishHandler_TimesOutOnTheHarnessClockWhenTheEndpointCannotBecomeReadyAsync;
     configured-clock timer identity/policy, exact terminal fault and endpoint stop.
3. Preserve features and all previous names/cases. Compile with warnings-as-errors;
   correct fixture mistakes without weakening productive policy or using pragmas.
   Fresh focused 25/25 passes are actual. Internal read-only counterreview is done.
4. Actual separate CTS/gate/shared-MIME/wrong-clock/producer-monitor counterchanges
   compile exit0 and fail exactly the intended cases/assertions. Every source SHA
   is restored; no surviving mutant reaches the final package.
5. Final restored build exits0, zero warnings/errors. Unfiltered Core4,011/4,011
   passes strictly; the same run collects nine productive modules' coverage:
   78.3794% lines/71.7960% branches, not whole-product coverage. Handwritten report
   and dispositions are complete; secure exact owned paths using normal commit,
   new annotated tag/approved atomic push and separately keyed three-ref checks.

All broader A+ axes and prior qualified proof gaps remain active. No duplicate
full coverage/cloud claim or external independent acceptance is inferred.

## Iteration 131 complete Core directory reading and source-contract checkpoint

1. Continue the original goal from secured input
   4ac87c03b95c07bf0414434c031a45fb57d904b6 and unchanged bound authorities.
2. Complete all remaining 150 personally read Core inputs / 32,604 physical lines:
   Initializers46, Mediator14, MessageData13, Requirements1, Serialization48,
   Transformation1, Transports27. Cumulative directory 557/557 / 143,127 lines.
   Every 2,922 JSON record is semantically read in a strictly reconstructed lossless
   data view, not claimed as raw physical-character display or generated review.
3. Verify exact sorted Git/read path union and all input bytes, hashes/line counts.
   Additionally read nine productive sources/1,510 lines, every comment, and five
   graph inputs/443 lines. Manually correct only the buffer ownership comment;
   exact two-line derivative verified, no executable/API/test change.
4. Retain MD01 global mutable Mediator MIME, MD02 notification consistency and PA01
   writer reservation/encoded-size contracts OPEN. Record five High/two Medium/two
   Low existing-test groups OPEN; preserve actual strong controls and all older
   findings. Correct EV01/EV02 reading attribution without inventing stale builds.
5. Preserve history tails and manually validate the complete manifest. Secure five
   owned paths using normal commit/new annotated tag/approved atomic push and
   independent keyed branch/tag-object/peeled refs before remote-security claims.

Next: complete effective shared graph/packages/fixtures/data/execution CI and full
language-parser/GitReadSet admission, then coherent source/regression/mutation work.
Directory reading is not yet full effective-project acceptance; no premature new
Core test design/edit, generator, duplicate native/coverage/CRAP/global Async/cloud
or independent external acceptance. Historical128 4,007 passes stay historical.
Continue every original whole-src manual comment/API/architecture obligation.
[Checkpoint](../evidence/WP-F2-SERVICEBUS-SOURCE-ARCHITECTURE-A-PLUS-01/ITERATION-131/CORE_OWNER_READING_COMPLETION_AND_SOURCE_CONTRACT_REVIEW.md).

## Iteration 130 connected Courier/Futures/Scheduling/Retry/Harness checkpoint

1. Continue the unchanged original goal from secured input
   311a2bea237e77dad480217631a9cdeb5f0a061d; preserve all prior findings and work.
2. Personally read 93 connected owning files completely: 22,693 physical lines,
   every fixture/member/assertion/comment; no truncated range receives credit.
3. Verify exact current/prior Git bytes and reconcile 523 declared method names
   with 597 historical passed cases. Cumulative owner 407/557 / 110,523 lines;
   150 remaining / 32,604 lines. Full owner/parser/graph admission is still open.
4. Manually record two concrete synchronization defects and qualified proof gaps,
   retaining strong counterexamples. Fourteen grouped findings remain open:
   0 Critical / 9 High / 3 Medium / 2 Low. No premature new Core test design/edit.
5. Preserve all three history tails and validate the handwritten manifest; secure
   four owned report/history paths using normal commit/new annotated tag/atomic
   origin push and independently keyed branch/tag-object/peeled refs.

No source/test/project/dependency/code-comment change, generator, fresh duplicate
native run, coverage/CRAP/mutation/provider/global Async or external acceptance.
Historical iteration-128 4,007 passes remain historical. Complete remaining owner
inputs/effective graph/data/full parser/GitReadSet, then connected remediation and
the whole productive-src manual API/architecture/comment quality work.
[Checkpoint](../evidence/WP-F2-SERVICEBUS-SOURCE-ARCHITECTURE-A-PLUS-01/ITERATION-130/COURIER_FUTURES_SCHEDULING_RETRY_AND_HARNESS_READING_CHECKPOINT.md).

## Iteration 129 complete connected Core contract reading checkpoint

1. Continue the original goal from actually secured input
   020c146f8067d8f5bf38ef51aa35344e7dd7709e; preserve all earlier work/findings.
2. Finish all 59 remaining connected files through EOF personally: 14,429 lines,
   338 declarations / 398 historical passed cases. Connected packet 102/102,
   23,480 lines / 508 declarations / 665 historical cases is now fully read.
3. Bind every new/prior full read to exact input Git bytes and historical metadata.
   Cumulative Core owner 314/557 / 87,830 lines; 243 remaining. Name reconciliation
   is not full-parser/effective-project/GitReadSet admission under agreement §4.3.
4. Handwrite scoped positive observations and 0 Critical / 9 High / 3 Medium /
   2 Low deferred groups, all open. No new test design/edit, source change,
   executed mutant, fresh native/coverage/CRAP/provider or A+ acceptance claim.
5. Preserve all three history tails; manually validate the exact manifest, then
   normally commit/tag/atomically push four owned files and independently verify
   branch, annotated tag and peeled commit before claiming remote security.

Seven authority bindings and exact prior DECISIONS reconstruction exit 0. Only
the fully read Licensing PO-2026-09-15-03 row/section differs; ServiceBus scope and
the selected Slice remain unchanged. Historical iteration-128 Core build/native
4,007/4,007 passes remain historical. CS01 fault/mutation/interval proof and all
earlier findings remain open. No independent counter-review is claimed here.

Next concrete action: remaining 243 Core owner inputs, effective shared project/
packages/fixtures/data/execution CI, full parser and GitReadSet admission; only
then new Core test design/edits and effective targeted regressions/mutations.
Continue the whole-src manual comments/API/architecture and original quality
goal; do not reopen resolved informational questions.
[Checkpoint](../evidence/WP-F2-SERVICEBUS-SOURCE-ARCHITECTURE-A-PLUS-01/ITERATION-129/CORE_COMPOSITION_TELEMETRY_AND_TOPOLOGY_READING_CHECKPOINT.md).

## Iteration 128 cache initialization repair and connected reading checkpoint

1. Preserve the original goal and all earlier work. Actually verify secured input
   6c0916b20eb5b8f71ca924c84b87b3bca054f50e and unchanged authority before edits.
2. Fully personally read the constructor/cleanup/disposal path, manually repair
   failure cleanup without changing the public API or losing a feature.
3. Continue connected Core reading: 43 complete new files / 9,051 lines;
   170 declarations / 267 historical and fresh passed cases. Cumulative owner
   255/557 / 73,401 lines; 302 remaining. No partial-owner test design/edit/admission.
4. Strict fresh Core Release build and native 4,007/4,007 pass, zero warnings/errors
   and failed/skipped/pending/other records. Exact source/read/native diagnostics 0.
5. Handwrite the report and preserve history tails. Normally secure five owned
   files with commit, annotated tag, atomic push and independent keyed refs.

CS01 ownership implementation is repaired but its fault regression, effective
cleanup mutants and portable interval contract remain open. Scoped existing-test
review has 0 Critical / 8 High / 2 Medium / 1 Low, all eleven open. The attempted
internal advisor provides no admitted source judgment or independent acceptance.
This is an intermediate checkpoint, not a completed whole-product iteration.

Next concrete continuation: DI ConfigurationContract line 236 through EOF, then
the remaining 59 connected inputs and complete owner/parser/GitReadSet admission.
Do not reopen resolved informational questions. The original whole-src manual
comments/API/architecture/feature/provider/coverage/CRAP goal remains active.
[Checkpoint](../evidence/WP-F2-SERVICEBUS-SOURCE-ARCHITECTURE-A-PLUS-01/ITERATION-128/CACHE_INITIALIZATION_REPAIR_AND_CORE_READING_CHECKPOINT.md).

## Iteration 127 intermediate Cache source-structure checkpoint

1. Continue the original goal from actually remote-secured iteration 126 and
   unchanged complete authority bindings. Preserve all earlier work/findings.
2. Completely personally read the six Cache owning test files and seventeen
   productive input files. Manually correct comments only after understanding
   complete code; place the existing untyped index base in its matching file.
3. Prove exact non-XML/body/signature equivalence and fresh strict Core/Architecture
   builds/native execution. Review all 104 existing Cache methods; retain seven
   open findings with calibrated independent deferred acceptance requirements.
4. Revalidate prior readings and final bytes. Preserve the failed inventory
   receipt and correct the owner-qualified glob exclusion, not product code.
   Handwrite one intermediate report; no generator or partial-owner test edit.
5. Normally secure exactly six source files, the report and three history prefixes
   with scoped commit, annotated tag, atomic push and independent keyed refs.

Cache scope fully personally read: 17 productive input files / 1,701 lines,
18 final productive files / 1,707 lines; six test files / 2,974 lines, 104 methods,
105 historical and fresh passed cases. Cumulative Core: 212/557 / 64,350 lines,
345 remaining. The connected 108-file selection remains in progress, 102 unread.
Fresh Core 4,007/4,007 and Architecture 439/439; bidirectional Async naming passed.
0 Critical / 6 High / 1 Medium / 0 Low remain open. No owner/A+ acceptance.

Continue complete connected owning inputs before new test design/change, then
causal product/test repairs and effective mutations. This checkpoint does not
shrink, complete or interrupt the original whole-product A+ goal.

## Iteration 126 complete consume, request and native transport reading

1. Continue the original goal from remote-secured iteration 125 and unchanged
   complete authority bindings; preserve all earlier work and findings.
2. Completely personally read all 60 selected files, including every method,
   field, fixture, helper, data arrangement and comment. No new Core test
   design/change or owner acceptance before complete 557-file admission.
3. Review every existing method, calibrate eight scoped weaknesses against
   strong adjacent proofs and record independent deferred correction criteria.
4. Bind all input bytes and prior readings; reconcile 312 methods to 459
   historical native passed cases. Use physical line counts consistently:
   the unchanged lock file has one final line without a newline. Handwrite
   one compact packet; check exact rows, whitespace and source invariance.
   Do not replay unchanged complete builds/tests for reading-only evidence.
5. Secure exactly the packet and three history prefixes with normal commit,
   annotated tag, atomic non-force push and independently keyed remote refs.

Personal reading/review complete: 60 new files / 18,004 lines. Cumulative Core
owner: 206/557 files / 61,376 lines, 351 remaining. Selected Clients, Consumers,
Context and InMemoryTransport folder scopes are complete, not the whole owner.
Settled 0 Critical / 5 High / 2 Medium / 1 Low remain open. Actual input/native
and corrected prior-physical-line diagnostics terminate 0; the initial newline
count mismatch is retained and explained, not treated as lost code or a test failure.

Continue larger coherent remaining-owner reading, then independent red/green
repairs, effective mutations and connected productive contracts. All whole-src
reading/manual comments, greenfield API/type/file/folder/feature, package/provider
and current global coverage/CRAP objectives remain in the original goal.

## Iteration 125 complete middleware, transaction and in-memory saga reading

1. Continue the original goal from actually remote-secured iteration 124;
   revalidate unchanged complete authority bindings and preserve all prior work.
2. Completely personally read all 54 selected owning files, including every
   method, field, nested fixture, data arrangement, helper and comment. Keep
   Core test design/change deferred until complete 557-file owner admission.
3. Manually review every existing method; settle precisely scoped weaknesses,
   independent correction criteria, positives and actual adjacent qualifications.
4. Revalidate all 92 prior readings and selected input bytes; reconcile every
   selected method to the unchanged historical native report. Handwrite one
   compact packet with exact per-file accounting rather than repetitive method
   disposition tables. Check own whitespace/source-scope invariance; no unchanged
   complete build/test replay for reading-only evidence.
5. Secure exactly one packet and three history prefixes with normal commit,
   annotated tag, atomic non-force push and independent keyed remote references.

Reading/review complete: 54 new files / 17,659 lines; 392 existing methods /
637 historical native passed cases. Cumulative Core owner: 146/557 files /
43,372 lines, 411 remaining. All three selected folder scopes are complete,
not the entire owner. Settled 0 Critical / 4 High / 2 Medium / 1 Low remain open.
Actual read-binding/prior-reading/method diagnostics terminate 0. Security follows
actual terminal commands, not future plan statements.

Continue larger coherent remaining-owner reading, then exact independent test
repairs and connected productive contracts with genuine red/green/mutations.
All full-src/manual-comment, greenfield API/structure/feature, metadata/package,
provider and current global coverage/CRAP objectives remain in the same goal.

## Iteration 124 larger coherent Core-owner reading packet

1. Continue the original goal from actually remote-secured iteration 123 and
   matching complete authority bindings; preserve all prior changes/findings.
2. Completely personally read the 45 remaining selected JobService, DurableSend,
   MessageJournal and ReliableMessaging files, including all nested/standalone
   fixtures. No new Core test design/change before complete 557-file admission.
3. Manually review every existing method, calibrate weaknesses against actual
   sibling proofs, and record exact independent correction targets as deferred.
4. Bind the unchanged input bytes, revalidate all 47 previous readings and
   reconcile 283 methods / 514 historical native passed cases. Check manually
   authored reports and owned whitespace; no unchanged full build/test replay
   for reading-only evidence. Reevaluate gates for any later code/comment change.
5. Secure exactly two evidence files and three test-agent history prefixes with
   normal commit, annotated tag, atomic non-force push and independent keyed refs.

Personal reading/review is complete: 45 new files / 11,805 lines; cumulative
92/557 / 25,713 lines, 465 remain. All 51 selected folder files are read.
Settled findings 0 Critical / 4 High / 1 Medium / 1 Low remain open, with exact
targets, positives and adjacent-proof qualifications. Input and historical
native reconciliation diagnostics terminate 0; checkpoint security follows
its actual terminal commands rather than this plan's future intentions.

Next coherent packet continues remaining Core-owner inputs toward full admission,
then independent test repairs, connected runtime/API contracts and selected
effective mutations. The same complete-source/manual-comment, greenfield API/
structure/feature, package/provider and global coverage/CRAP objectives remain.

## Iteration 123 complete saga Core reading and outbox lifecycle source

1. Continue the same original whole-product goal from the actually remote-secured
   iteration-122 input; reconcile unchanged complete authority bindings.
2. Complete the twenty remaining selected saga/job state-machine owner files,
   including all methods, nested fixtures and helpers. Preserve actual read
   progress; no new Core test design/change before full 557-file admission.
3. Fully read nine causal source neighbors and manually correct every inaccurate
   comment after complete understanding. Preserve runtime/API findings as open.
4. Review all 93 methods / 137 cases, record exact independent correction targets,
   obtain bounded internal Sol advice, prove final comment-only byte equivalence,
   strictly build and execute fresh complete Core/Architecture owners, parse actual
   native case records and verify only five changed source whitespace scopes.
5. Secure exact owned comments/reading/review/evidence with normal commit,
   annotated tag, atomic non-force push and independent keyed remote verification.

Steps 1–4 are complete: twenty full personal owner reads / 6,804 lines; cumulative
47/557 / 13,908 lines, 510 remain. Nine full productive reads / 1,099 input lines;
five manual comment-only repairs, 1,105 final lines, exact binary equivalence 0.
Four High test weaknesses and one cosmetic Low naming issue are documented, not
fixed or accepted merely because their cases pass. Internal eight-file advice
produces three manually corrected qualifications and final no mandatory comment
fix; its two protected filename enumerations are documented scope deviations.
Final strict builds Core/Architecture 0, zero warnings/errors (12.60s/4.94s);
fresh native complete owners 4,007/4,007 and 439/439, zero failures/skips
(22.745s/4m03.398s), actual bidirectional Async case passed (204718ms).
Exact source whitespace 0/no writes, independently parsed native counts and
93/137 reviewed cases 0. Actual diagnostic failures are resolved and preserved.
Checkpoint security follows its own actual commit/tag/push/ref outcomes.

Next coherent priority: continue all remaining Core-owner inputs from the bound
progress until full admission, then address exact independent test oracles and
connected runtime failure/cancellation/recovery contracts with real red/green and
effective selected mutations. Preserve sibling assembly and integration-family
source boundaries. No whole-goal reduction or current global coverage certificate.
Complete all-source personal reading/manual comments, metadata/package baseline,
feature equivalence, provider acceptance and whole-product coverage/CRAP remain.

## Iteration 122 state-accessor source and Core-owner reading

1. Reconcile the secured iteration-121 input and any changed governance binding.
2. Continue complete personal Core-owner reading from an exact, checksum-bound
   progress table, prioritizing the existing saga state-machine paths. Do not
   design or edit new owning runtime tests before the full owner is admitted.
3. Fully understand the eight related accessors/contracts/extensions. Manually
   correct inaccurate or generic comments immediately after each complete read;
   retain runtime and API findings rather than disguising them as documentation.
4. Prove the exact eight-file executable/signature equivalence, obtain one
   bounded internal read-only Sol counterreview against matched entry/exit hashes,
   strictly build the affected existing owners, execute their unfiltered native
   tests and verify only the changed source whitespace without writes.
5. Bind actual outcomes and secure only the owned source/comments/reading record
   with a normal commit, annotated tag, atomic non-force push and independent
   reference-keyed verification. Continue the full original goal, not a smaller
   source-comment completion target.

Personal source reading and four handwritten comment repairs are complete.
Core-owner reading is partial: 27/557 files, 7,104 personally read lines.
The internal counterreview completely reads eight files / 517 lines, finds no
mandatory comment correction and leaves all runtime candidates separate.
Exact XML-only equivalence terminates 0. Strict Core and Architecture builds
terminate 0 with zero warnings/errors (64.64s / 23.66s); native Core terminates
0, 4,007/4,007 passed with zero failures/skips (20.439s). Source whitespace
verification terminates 0 without output/writes. Fresh native Architecture
terminates 0, 439/439 passed, zero failures/skips (3m33.469s), including the
actual bidirectional Async case (174206ms). Independently parsed fresh native
reports and persisted exact source/Core-read bindings validate actual outcomes.
Checkpoint security follows actual commit/tag/push/reference observation.

Next: finish the remaining Core-owner files from the preserved read progress,
then address connected declaration/cache/configuration/cancellation boundaries
in the actual owning tests with independent red/green and one-cause mutations.
The unchanged API baseline mismatch, precise remaining metadata contracts,
provider acceptance, full code/branch coverage and all-source A+ closure remain.

## Iteration 121 recursive member nullability

1. Handwrite typed PublicApiMemberNullabilityTests against the unchanged actual
   tool and map every method to REQ-VSB-PACKED-PUBLIC-API in the exact catalogue.
   Cover scalar parameter/return, nested generic argument positions, array root/
   element/jagged/multidimensional nodes, property/field read-write promises,
   combined indexer value/index parameters, constructor and event contracts,
   byref elements, generic uses, oblivious versus known references, private
   accessor exclusion and value-only omission.
2. Require strict compilation 0, then independent metadata arrangements and
   functional native assertion failures before implementing the real formatter.
3. Preserve recursive read/write states in declaration order using a context
   owned by each member enumeration. Canonically omit only fully known NotNull
   reference trees; retain Unknown/Nullable and positional child structure.
   Property-root directions apply only to externally visible accessors.
4. Manually update any old exact oracle only for the intended new contract,
   preserving its original modifier/default/shape assertions. Run all inventory
   and exact requirement projection cases, inspect every assertion and obtain
   bounded internal Sol advice where useful.
5. Compile/execute selected one-cause regressions, manually restore exact bytes,
   then strictly build and run complete affected owners, selected formats and
   the actual fresh package gate. Keep unchanged baseline mismatch open rather
   than automatically updating it. Bind only actual outcomes and normally
   commit/tag/push/independently verify the completed owned checkpoint.

The original whole-product goal stays active. Core runtime test-owner admission,
all connected behavioral findings and final global coverage/A+ proof remain open.

The handwritten bounded repair and actual validation are complete: 19 methods /
38 cases, 194 exact tuples, final inventory/projection 123/123 green, selected
compiled mutations9/9 detected (30 failed cases) and exact source restorations.
Final strict Architecture build0/zero warnings/errors; fresh unfiltered owner
439/439 green including the actual bidirectional Async guard. Two scoped read-only
formats terminate0. Fresh package gate reaches final unchanged baseline comparison
and terminates1 after31 packages/18 journeys/3 consumers/30 inventories. Exact
3230-block duplicate-preserving prior/fresh shape comparison terminates0 after
removing only new nullability metadata. Checkpoint security follows actual Git
outcomes. Next coherent runtime priority is full personal Core test-owner admission
before addressing connected saga declaration/cache/configuration failures in the
real owning tests; metadata, source-reading and global proof obligations stay open.

## Iteration 120 complete state-machine root source review

1. Completely personally read the central declaration file and selected
   accessor/observer/composite/transition neighbors; manually update all relevant
   comments only after understanding the complete corresponding file.
2. Recheck every changed comment against its actual overload and helper flow.
   Prove exact comment-only executable/signature equivalence. Retain connected
   runtime findings instead of disguising them as documentation fixes.
3. Obtain a bounded internal read-only Sol source/comment counterreview with
   exact six-file checksums; never call it external acceptance or executed proof.
4. Strictly build affected Architecture/Core owners, run existing complete native
   owners using confirmed .NET-10 MTP syntax, verify selected source whitespace
   without writes and bind only actually terminal outcomes.
5. Normally commit/tag/push only owned files and independently verify remote
   references. Continue the original unbounded whole-product A+ goal with full
   test-owner admission before new owning runtime tests or behavioral changes.

Steps 1–3 are complete: six personal reads / 2,747 starting lines, two manual
comment-only source corrections, six matched internal read bindings and no
mandatory comment correction. Nine connected findings remain open. Strict
Architecture/Core builds terminate 0, zero warnings/errors (52.05s / 34.21s).
Fresh complete Core is terminal green, 4,007/4,007, zero failures/skips (22.348s).
Exact six-row comment-only/input/current comparison terminates 0 with two changed
files and zero mismatches; scoped two-source whitespace terminates 0 without
writes/output. Fresh unfiltered Architecture terminates 0, 401/401 passed,
zero failures/skips (2m 55.903s overall); the actual bidirectional Async test
passes (139,076ms). All owned validation handles are terminal; capture follows
actual observation. No current whole-product coverage/acceptance is claimed.

## Iteration 119 member modifiers and optional parameter contracts

1. Manually author PublicApiMemberModifierTests against unchanged tooling;
   bind every test method to REQ-VSB-PACKED-PUBLIC-API in the exact catalogue.
2. Observe successful strict compilation followed by functional native red
   assertions, distinguishing arrangement/compiler errors from causal evidence.
3. Correct the shared parameter/default/custom-modifier/accessor/override
   formatters manually. Re-run all inventory tests and the exact projection.
4. Review every assertion and connected gap; empirically compile and execute
   selected one-cause regressions, restore exact source bytes, then validate
   the complete Architecture owner and affected existing product owners.
5. Finish the nine personally read source partials' manual comment corrections;
   prove no executable/signature changes there. Record unresolved runtime and
   inventory findings. Secure only actually proved owned changes with a normal
   commit, annotated tag, push and independent remote reference verification.

The test names in PublicApiMemberModifierTests map to the bounded checklist:
ParameterDefaults_PreserveTypedValueDefaults;
ParameterDefaults_KeepNullableAndReferenceNullDefaults;
ParameterDefaults_EscapeLiteralsAndFormatConstantsInvariantly;
ParameterOptionality_PreserveOptionalWithoutDefault;
ParameterOptionality_DistinguishOptionalFromRequired;
ParameterArrays_PreserveParamsApartFromOrdinaryArrays;
PropertyAccessors_DistinguishInitFromSet;
ReturnModifiers_DistinguishReadonlyFromWritableReferences;
FieldModifiers_PreserveVolatileFieldContracts;
MethodModifiers_DistinguishOrdinaryAndSealedOverrides;
AccessorModifiers_PreserveStaticVirtualAndAbstractContracts.
The internal counterreview's expanded exact oracles add:
ParameterOptionality_KeepDefaultConstantIndependentOfOptionalFlag;
ParameterModifiers_DistinguishRefInReadonlyAndOut;
ParameterModifiers_PreserveVirtualReadonlyLocationContract;
ParameterDirections_DoNotInventByReferenceOrReadonlyContracts;
ParameterDefaults_PreserveTypedGenericDefaults;
ConstructorDefaults_PreserveTypedOptionalValueDefaults;
LiteralFields_KeepCharacterAndStringDelimitersDistinct;
EventAccessors_PreserveEachAccessorContract;
IndexerDefaults_PreserveTypedOptionalValueDefaults.
All 20 methods / 43 cases have exact catalogue bindings (175 overall). Initial
functional red 15/22, expanded red 7/43 and corrected inventory/projection 83/83
are terminal. Ten selected one-cause mutations strictly compile, fail natively
(20 failed cases total), and are manually restored byte-for-byte. Final strict
Architecture/Core/Abstractions builds and full Core 4,007 / Abstractions 749 native
regression are terminal green; three selected read-only formats terminate 0.
Unfiltered Architecture is terminal green, 401/401, including the actual
bidirectional Async guard. Fresh package execution terminates 1 only at the
unchanged baseline comparison, after all packing/journey/consumer stages and
30 inventories; the 20,045-line fresh output is checksum-bound in the report.
All validation handles are terminal. Capture outcomes follow actual observation.
The original whole-product goal remains active, with all connected gates open.

## Iteration 119 nested API source reconciliation

1. Personally read the complete DynamicFilter/SagaConnector/StateMachineInterfaceType
   source families and immediate selected dependencies, updating comments manually
   only after understanding each complete file. Preserve the product assembly
   ownership structure and no-generator requirement.
2. Obtain an internal read-only Sol counterreview distinguishing all corrected
   nested identities from actual prior visibility retirements, preserving every
   old colliding block. Verify exact implementation/interface checksum scope and
   actual full generic identities; qualify static-only extension equivalence.
3. Strictly build and execute existing Core/Abstractions/Architecture owners,
   verify whitespace without writes and observe the actual fresh package gate.
   Record the unchanged baseline mismatch rather than suppressing comparison.
4. Secure the bounded owned source/comment/evidence checkpoint normally with an
   annotated tag, atomic push and independent exact remote verification.
5. Continue remaining state-machine personal reads and the packet's connected
   behavior/API findings with proper test-owner admission, causal tests and
   compiled one-cause checks. Complete original whole-product gates iteratively.

Steps 1–2 produce 24 complete main reads / 1,872 starting lines, 23 manually edited
documentation-only files, 22/35 newly qualified nested source identities covered
including prior PipeConfigurator reads, and a separate static 69-file advisor
scope. Ten concrete follow-up findings stay open. Core 4,007/4,007, Abstractions
749/749 and all three strict Release builds are terminal green; all three selected
formats terminate 0. Unfiltered Architecture terminates 0 with 358/358 passed,
zero skips/failures, including the actual bidirectional Async guard. The initial
109 source/raw bindings match. No new test methods/tuples/mutations are claimed.
Actual fresh package execution terminates 1 only at unchanged baseline comparison:
31 packages, 18 journeys, three isolated consumers and 30 runtime inventories
complete first; fresh output matches the secured fresh API bytes. Git capture
is bound only after actual outcomes. The original whole-product goal is neither
narrowed nor completed.

## Iteration 119 governed traversal and generic API contracts

Before the pending checkpoint, close the real inventory-host runtime failure:
request FrameworkReference items in the existing evaluation helper, use the
executing test assembly's explicit configuration, obtain two functional red
runtime tests against the unchanged Console project, then add the required
versionless ASP.NET Core framework reference. Rebuild, run bounded and complete
Architecture tests, and rerun the real fresh package gate. Preserve the initial
arrangement errors and native 134 as failed attempts, never acceptance evidence.

1. Extract the existing broad build-file walk into a parameterized internal
   helper without executing it at the real repository root. Manually author
   exact isolated-fixture membership tests and observe functional red results.
2. Replace the extracted walk with top-level policies plus the five canonical
   governed roots; fail on inaccessible owned trees and never follow aliases.
   Wire both build-policy consumers and the restore-lock guard to scoped truth.
   Prove the direct fixture and the previously unsafe real-root methods green.
3. Add direct type/method generic contract oracles before correcting inventory
   emission: inherited/own generic parameters, variance, class/struct/new(),
   base/interface/parameter constraints, deterministic order and no duplicates.
4. Review assertions and real compiled one-cause candidates, restore exact bytes,
   then run the full safe Architecture owner. Reconcile actual fresh package API
   differences manually; never relabel baseline comparison failure as success.
5. Commit/tag/push the proved owned checkpoint and report all remaining original
   goal obligations. The original whole-product goal is not narrowed or closed.

Steps 1–3 and the direct empirical review are proved: original traversal 3/3
functional failures; corrected expanded traversal 7/7 green; original generic
contracts 14 failures/15; combined 47/47 green. Four traversal and six generic
single-cause candidates compile and are killed, then byte-restored. First
Architecture owner 356/356 green and three scoped whitespace checks exit 0.
The real native-134 shared-framework regression is causally closed: locked
restore/build 0, corrected bounded 48/48 and final unfiltered 358/358 green,
155 projection tuples and updated whitespace checks 0. The actual fresh gate
now exits 1 only at baseline comparison, thirty runtime APIs / 20,045 lines.
Collision-preserving diagnostics retain all old blocks; reviewed baseline
disposition remains connected follow-up, not implied by Architecture green.

## Iteration 119 packed API type identity

1. Migrate the existing formatter into a regular SDK console project without
   changing its faulty rendering; bind Architecture tests and both engineering
   solution closures to the real tool. Preserve strict package comparison.
2. Manually author independent exact-string tests for open/closed nested types,
   inherited/own arity, nongeneric children, three levels, recursive arguments,
   distinct child/parent identities, generic parameters and CLR modifiers.
   Compile successfully and observe functional red tests before correction.
3. Preserve each metadata segment and distribute the leaf's actual generic
   arguments by each segment's own arity. Distinguish vectors from non-vector
   rank-one arrays. Run direct green tests, review assertions and compiled
   one-cause mutations, restoring exact source bytes after every candidate.
4. Correct protected-input traversal before full Architecture execution. Review
   actual newly packed API differences before any manual baseline disposition;
   do not use the automatic baseline-update switch as acceptance.
5. Record exact bounded proof and remaining whole-goal obligations, then commit,
   tag and push normally at the completed checkpoint. Iteration 119 stays open.

Steps 1–3 are causally proved: original 14 failures/24, final 31/31 bounded,
three compiled single-cause kills with exact restorations. Steps 4 and wider API
constraint/variance inventory remain connected follow-up work, not failed gates
to suppress. The bounded checkpoint is not the whole iteration's acceptance.

## Iteration 119 saga query/index integrity

Continue from the secured ownership checkpoint using
[the saga index packet](iteration119-saga-index-packet.md). Prove actual predicate
evaluation, reference membership, captured-key removal, staged publication, unique
IDs, nullable keys and safe snapshots before correction. The original goal remains.

## Iteration 119 in-memory saga ownership follow-up

Follow the requirement-to-oracle and red-first correction sequence in
[the saga ownership packet](iteration119-saga-ownership-packet.md): removed-load
lease ownership, exact-once disposal, required inputs/modes/tasks, retained state
and cancellation semantics, and replacement-safe deletion. Record real mutation
results only after compilable runs and exact source restoration. Wider API/code/
provider/full-read requirements remain active rather than being narrowed to this packet.

## Iteration 1 outcome

Eliminate confirmed persistence defects, silent configuration contracts, discarded cancellation tokens, mutable process-global identifier configuration, and the unimplemented Azure message-session query path without losing supported behavior.

## Requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-APlus-RUNTIME-001` | EF abandoned inbox entry stores the exact supplied UTC instant and remains terminal after a fresh context/store | EF reliable-store tests | SQLite provider behavior plus restart query |
| `REQ-APlus-CONFIG-001` | Receiver wrapper forwards topology flags, dependencies, dependents, and generic/runtime message topology changes | core configuration tests | Direct state and readiness/completion observations |
| `REQ-APlus-CONFIG-002` | In-memory `AutoStart` changes the bus endpoint configuration | in-memory configuration tests | Both boolean values observed through the built configuration |
| `REQ-APlus-CONFIG-003` | Endpoint registration inclusion is mutable only through the authoritative registration owner | dependency-injection tests | True/false round trip and downstream selection behavior |
| `REQ-APlus-API-001` | Composite filter collections are get-only and matching remains behaviorally correct | abstraction/core tests plus architecture test | Compile-time/reflection shape and include/exclude truth table |
| `REQ-APlus-ASB-001` | Message-session saga query correlation evaluates the current persisted session state and forwards matching queries | Azure Service Bus tests | Matching, non-matching, identity, count, and cancellation behavior |
| `REQ-APlus-ASB-002` | Message-session saga writes propagate the caller cancellation token | Azure Service Bus tests | Exact token identity for save and update operations |
| `REQ-APlus-JOB-001` | Job lifecycle notifications and progress propagation preserve caller cancellation | job-service tests | Exact token identity at provider, send, and progress-buffer boundaries |
| `REQ-APlus-NEWID-001` | The static `NewId` façade is immutable after startup and remains unique under parallel generation | abstractions tests | Public API-shape audit and 100,000-value parallel uniqueness run |

## Test partitions

- Positive: each supported configuration affects its runtime owner.
- Negative: null arguments, pre-cancelled tasks, and unsupported capabilities fail with the narrowest stable exception.
- Boundary: true/false flags, empty/non-empty filter predicates, exact timestamps including non-UTC offsets.
- Persistence: reload with a new EF context and a newly created reliable store.
- Composition: validation happens before a receive pipeline handles a message.
- Compatibility: no alias or obsolete member is added; supported feature behavior remains available.

## Mutation obligations

- Remove the EF `CompletedAt` assignment: the exact timestamp/restart assertion must fail.
- Replace each forwarding setter/method with a no-op: its direct behavior test must fail.
- Reintroduce a composite-filter setter or invert include/exclude semantics: API-shape or truth-table tests must fail.
- Replace Azure query predicate evaluation with an unconditional match: the non-matching and count assertions must fail.
- Drop the Azure state-write or job notification cancellation token: the exact token-identity tests must fail.
- Reintroduce a public process-global `NewId` mutator: the façade API-shape test must fail.

## Execution order

1. Add failing tests against the baseline for each confirmed defect.
2. Implement the smallest coherent behavior correction.
3. Run focused owner projects.
4. Run test-gap, assertion-quality, and anti-pattern checks for changed tests.
5. Execute one-cause mutations and restore byte-for-byte.
6. Run clean Release builds, the complete Unit/Architecture profile, relevant provider profiles, format/static gates, and zero-test/skip guards.
7. Record evidence, commit, tag, push, and verify remote hashes.

## Later iterations

Subsequent plans will cover application options and parameter completeness; member-level public API baselines and layering; source navigation and package ownership; XML documentation/comment/directive cleanup; then full coverage and final multidimensional re-review.

## Iteration 2 outcome

Make SQL URI materialization fail explicitly at invalid database boundaries and replace duplicated 30-bit topology-name suffixes with one deterministic 65-bit shortening policy that preserves provider length budgets.

## Iteration 2 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-SQL-URI-MATERIALIZATION` | Both SQL providers preserve valid absolute/relative values and reject null, database null, blank, malformed, and non-string values | SQL transport tests | Identical result/exception contracts for both handlers |
| `REQ-VSB-TOPOLOGY-NAME` | Bounded temporary names use the full budget, retain a readable prefix, and carry a 13-character hash | core topology tests | Exact length, separator position, alphabet, and invalid-budget guard |
| `REQ-VSB-ASB-SUBSCRIPTION-NAME` | Long names are deterministic, provider-bounded, and collision-resistant; missing names are rejected | Azure Service Bus tests | Exact shape, repeated-input stability, different-input separation, and 10,000-name collision set |

Iteration 2 mutation obligations reject relative SQL values independently in each provider, reduce the shared suffix to six characters, bypass missing-name validation, and remove the minimum-budget guard. Each owning test must fail before the original corrected source is restored byte-for-byte.

## Iteration 3 outcome

Prove every application-level outgoing-options field at the shared context boundary, fail explicitly when a requested partition key cannot be represented by the selected transport, and make both reliable-messaging query surfaces validate and forward their complete contracts consistently.

## Iteration 3 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-APPLICATION-SEND-OPTIONS` | Headers, lifetime, all four identifiers, and partition key reach a send context exactly | abstractions tests | Direct public entry call plus independent field assertions at the context boundary |
| `REQ-VSB-APPLICATION-PUBLISH-OPTIONS` | Headers, lifetime, all four identifiers, and partition key reach a publish context exactly | abstractions tests | Direct public entry call plus independent field assertions at the context boundary |
| `REQ-VSB-APPLICATION-SCHEDULE-OPTIONS` | Destination, due time, message, options, and cancellation reach the advanced scheduler exactly | core scheduling tests | Public application API with a recording schedule provider |
| `REQ-VSB-APPLICATION-REQUEST-OPTIONS` | Metadata reaches the request envelope and an absolute deadline is derived from the injected clock | core client tests | In-memory request boundary with exact metadata and deadline assertions |
| `REQ-VSB-APPLICATION-OPTIONS-CAPABILITY` | An explicitly requested partition key is never silently discarded | abstractions tests | Unsupported context fails with a stable `NotSupportedException` |
| `REQ-VSB-RELIABLE-OPERATIONS-QUERY` | Snapshot, outbox page, and inbox page validate inputs and preserve exact cancellation | core reliable-messaging tests | Recording stores plus behavior against the in-memory store |

## Iteration 3 test partitions

- Positive: every non-null option value changes the corresponding outgoing context property exactly once.
- Default: omitted nullable values preserve existing context metadata and an empty header set adds nothing.
- Negative: null option collections, unsupported partition capability, invalid page sizes, incomplete inbox cursors, null queries, and expired request deadlines fail before provider I/O.
- Boundary: page sizes 1 and 1,000, deadline at and immediately beyond the injected current time, and null-valued headers.
- Propagation: cancellation-token identity is asserted at endpoint, scheduler, request, outbox-store, and inbox-store boundaries.

## Iteration 3 mutation obligations

- Remove each outgoing metadata assignment or header loop: its independent assertion must fail.
- Reintroduce silent partition-key discard: the unsupported-capability test must fail.
- Remove public API pipe forwarding or replace its cancellation token: the recording boundary test must fail.
- Remove either reliable query validation call or replace either forwarded query/token: the store-boundary test must fail.
- Ignore `RequestOptions.Deadline` or use the process clock: the injected-clock boundary test must fail.

## Iteration 4 outcome

Make physical source navigation deterministic without changing runtime behavior: every ordinary C# file must be named for a top-level type it owns, multi-type files without a primary type must be explicitly classified as cohesive declarations, and the convention must cover every evaluated product and native-test compile item.

## Iteration 4 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-SOURCE-NAVIGATION` | Single-type files match their declared type; multi-type files identify a primary type or a reviewed cohesive grouping | architecture tests | Roslyn inventory over all evaluated `src/**` and `tests/**` compile items |
| `REQ-VSB-SOURCE-NAVIGATION` | Partial fragments and generated/global files remain valid without weakening ordinary source rules | architecture tests | Explicit syntactic classification and stale-exception rejection |

## Iteration 4 mutation obligations

- Rename a representative single-type file away from its declared type: the source-navigation guard must fail with the exact repository-relative path.
- Change a reviewed cohesive group without updating its exact type manifest: the source-navigation guard must fail with the exact path and declarations.
- Change an approved partial fragment so that its exact owner identity no longer matches: the source-navigation guard must fail.
- Add a declaration to a `GlobalUsings` file or a secondary test class using a qualified xUnit attribute: infrastructure and test naming checks must fail.
- Add an unnecessary exception for an already conforming file: stale-exception validation must fail.

## Iteration 5 outcome

Remove compatibility-era public metadata and identifier contracts that a .NET 10 greenfield API would not introduce: span-based `NewId` formatting and parsing with exact boundary validation, standard unsupported-capability exceptions, no legacy binary-serialization markers, and no former state-machine product identity in source or package metadata.

## Iteration 5 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-NEWID-SPAN-API` | identifier bytes and text use read-only spans without by-reference managed objects | abstractions tests | exact reflection shape plus unchanged reference corpus |
| `REQ-VSB-NEWID-SPAN-API` | every formatter accepts exactly 16 bytes and every parser accepts exactly 26 characters | abstractions tests | both adjacent invalid lengths, null constructor input, and invalid alphabet/input cases |
| `REQ-VSB-GREENFIELD-CAPABILITIES` | unsupported behavior uses the BCL capability exception rather than an unfinished-code identity | architecture and behavior tests | no custom type or throw site plus stable failure classification |
| `REQ-VSB-GREENFIELD-METADATA` | product declarations carry no legacy binary-serialization opt-in | architecture tests | complete evaluated product compile-item scan |
| `REQ-VSB-GREENFIELD-IDENTITY` | source, documentation, and package metadata contain no former state-machine brand | architecture tests | complete product project and compile-item scan |

## Iteration 5 mutation obligations

- Reintroduce a by-reference `string` or `byte[]` parameter into the public `NewId` surface: the API-shape test must fail.
- Bypass the exact 16-byte formatter boundary independently for short and long input: boundary tests must fail.
- Reintroduce the custom unsupported-capability exception, a `[Serializable]` product declaration, or the former state-machine brand: the corresponding whole-product architecture test must fail.

## Iteration 6 outcome

Eliminate externally mutable process-global metadata and shared routing-slip state, and replace the RabbitMQ cluster-node parser with a canonical .NET parsing contract that round-trips DNS, IPv4, and IPv6 nodes while rejecting invalid ports before connection work.

## Iteration 6 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-METADATA-IMMUTABILITY` | message types, contract names, and reflected properties cannot mutate cached process state | abstractions tests | read-only public shape, mutation rejection, and stable repeated/parallel observations |
| `REQ-VSB-METADATA-IMMUTABILITY` | the core metadata facade cannot weaken the abstraction-level immutability contract | core metadata tests | exact public return types, stable collection identity, and mutation rejection |
| `REQ-VSB-CONSUMER-METADATA-IMMUTABILITY` | consumer convention metadata cannot be externally mutated | core consumer tests | read-only public shape and stable repeated observation |
| `REQ-VSB-COURIER-BUILDER-ISOLATION` | no-argument activities cannot share a publicly mutable dictionary | core courier tests | no public mutable sentinel, immutable activity arguments, and independent builders |
| `REQ-VSB-RABBITMQ-CLUSTER-NODE` | node text follows standard string/span parsing and canonical formatting | RabbitMQ tests | DNS, IPv4, bracketed/raw IPv6, absent/edge ports, invalid input, and culture independence |

## Iteration 6 mutation obligations

- Return a cached array directly from one metadata surface: the immutability shape or mutation test must fail.
- Restore the public mutable `NoArguments` dictionary: the API-shape test must fail; make the private empty dictionary mutable: the behavior test must fail.
- Restore the nullable-port formatting defect or accept a port outside `1..65535`: the cluster-node round-trip or boundary test must fail.

## Iteration 7 outcome

Make every application-interface capability a compile-time contract. Root interfaces must not use default implementations that probe for an Advanced interface at runtime and fail only after deployment; Advanced adapters may implement the required application member only when they provide the complete underlying capability.

## Iteration 7 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-GREENFIELD-APPLICATION-CONTRACTS` | application interfaces have no runtime-probing default members | architecture tests | reflection over every member declared by send, publish, scheduler, and typed consume contracts |
| `REQ-VSB-GREENFIELD-APPLICATION-CONTRACTS` | advanced implementations bridge options and scheduled cancellation without capability casts | abstraction/core behavior tests | exact option/cancellation propagation plus compile-time implementation closure |

## Iteration 7 mutation obligations

- Restore a runtime-probing default body on any application interface: the reflection guard must fail.
- Drop an options pipe or cancellation token from an Advanced adapter: the existing independent metadata and token assertions must fail.

## Iteration 8 outcome

Make application-level outgoing options stable at the asynchronous boundary, reject invalid lifetimes before provider work, and ensure every physical product source file belongs to exactly one evaluated product compilation.

## Iteration 8 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-APPLICATION-OPTIONS-IMMUTABILITY` | Default header collections cannot be mutated and caller-owned headers are snapshotted before asynchronous work | abstractions and core client tests | mutation rejection plus send, publish, schedule, and request boundary observations |
| `REQ-VSB-APPLICATION-OPTIONS-VALIDATION` | Zero and negative message lifetimes fail synchronously before endpoint or provider use | abstractions and core client tests | both adjacent invalid partitions across option pipes and request entry point |
| `REQ-VSB-SOURCE-OWNERSHIP` | Every physical `src/**/*.cs` file has exactly one owner in the evaluated product graph | architecture tests | complete file-to-`@(Compile)` ownership comparison across all product projects |

## Iteration 8 mutation obligations

- Retain a caller-owned header dictionary instead of copying it: all three outgoing pipe snapshot tests must fail.
- Permit a zero lifetime by changing the boundary from `<=` to `<`: the zero-boundary test must fail while the negative partition remains valid.
- Replace one immutable default header collection with a mutable dictionary: the default-shape test must fail.
- Add a product source path excluded from every project: the compile-ownership test must fail with the exact path and zero owners.

## Iteration 9 outcome

Make dependency resolution centrally owned and exact, remove accidental layer edges, and prove the
provider-testing delivery packages through isolated package-only consumers. Generate the packed
public API inventory deterministically across independent package runs.

## Iteration 9 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-DEPENDENCY-PINNING` | Every centrally managed project enables central transitive pinning | architecture tests | evaluated property across the complete governed project graph |
| `REQ-VSB-DEPENDENCY-VERSION-OWNERSHIP` | Every central-transitive lock entry equals its central minimum | architecture tests | all frameworks and governed lock files |
| `REQ-VSB-DEPENDENCY-CATALOG` | The central catalog contains no unused identity | architecture tests | direct and resolved package closure plus file-based tool locks |
| `REQ-VSB-TESTING-DI-BOUNDARY` | Testing packages depend on DI abstractions without the full container | architecture tests | exact direct package references for all testing packages |
| `REQ-VSB-CAPABILITY-DEPENDENCY-BOUNDARIES` | Capability packages contain only their intentional direct edges | architecture tests | exact visualizer and EF Core dependency sets |
| `REQ-VSB-TESTING-PACKAGE-CONSUMERS` | Each provider-testing package restores, builds, and executes without another direct ViciOne package | package consumer and architecture gates | three isolated consumers against a fresh package feed |
| `REQ-VSB-PACKED-PUBLIC-API` | Repeated package-only API inventories are byte-identical | package gate and architecture tests | stable structural inventory and identical SHA-256 values |

## Iteration 9 mutation obligations

- Disable central transitive pinning: the evaluated-property guard must fail for every centrally managed project.
- Change one central-transitive resolved version: the lock-version guard must report the exact project and package.
- Restore a full DI-container dependency in a testing package: the exact dependency-boundary guard must fail.
- Add a second direct ViciOne package to an isolated consumer: the package-isolation guard must fail.
- Include NuGet archive hashes in the public API inventory: independent identical package runs must produce different output and expose the nondeterminism.

## Iteration 10 outcome

Turn the deterministic package API inventory into an enforced, versioned public contract for every
delivered package. Pack the complete thirty-package catalog, restore all twenty-nine runtime
packages through a dedicated package-only consumer, and reject any unreviewed difference from the
committed packed API baseline.

## Iteration 10 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-PACKED-PUBLIC-API` | The gate compares a freshly generated inventory with a committed baseline and has an explicit update operation | architecture and package gates | exact script contract, tracked non-empty baseline, and default mismatch failure |
| `REQ-VSB-PACKAGE-CATALOG` | Every packable product project produces its exact documented package | architecture and package gates | evaluated project/package catalog equals all thirty expected package files |
| `REQ-VSB-PACKED-PUBLIC-API` | Every runtime package participates in a package-only restore before reflection | architecture and package gates | dedicated consumer with twenty-nine direct locked package references and no source references |

## Iteration 10 mutation obligations

- Replace the baseline comparison with unconditional success: the architecture contract must fail.
- Change one committed API line: the default fresh-package gate must fail and report the diff.
- Remove one runtime package reference from the complete API consumer: the exact catalog and lock
  assertions must fail.
- Remove one expected package artifact from the gate catalog: the exact packable-project/package
  comparison must fail.

## Iteration 11 outcome

Preserve caller cancellation as cancellation across timeout wrappers, job shutdown, provider
cleanup admission, and test-harness polling. Genuine elapsed deadlines must remain timeouts, while
job-owned cancellation remains a successful shutdown condition.

## Iteration 11 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-TASK-TIMEOUT-CANCELLATION` | generic and non-generic, pre-canceled and canceled while waiting | abstractions tests | exact caller token in `OperationCanceledException` for all four partitions |
| `REQ-VSB-JOB-CANCELLATION` | caller cancellation during the completion wait | core job tests | exact caller token escapes while the job-owned token is canceled |
| `REQ-VSB-ACTIVEMQ-CANCELLATION` | queue and topic deletion canceled during bounded admission | ActiveMQ tests | queued broker operation is not invoked and exact token escapes |
| `REQ-VSB-TEST-HARNESS-CANCELLATION` | state observation canceled during polling | testing tests | polling returns caller cancellation without waiting for another interval |

## Iteration 11 mutation obligations

- Restore either timeout-wrapper cancellation branch to `TimeoutException`: its exact generic or
  non-generic cancellation test must fail.
- Omit the caller token from the job-completion wait or catch caller cancellation: the job-handle
  test must fail.
- Restore `CancellationToken.None` for either ActiveMQ deletion: the corresponding saturated-queue
  test must fail.
- Restore the non-cancelable state-machine polling delay: its in-flight cancellation test must fail.

## Iteration 12 outcome

Make asynchronous intent mechanically unambiguous in both directions and normalize every public
product cancellation signature to the standard final-parameter shape. Update all implementations,
forwarders, call sites, documentation, and the intentionally versioned package API contract as one
atomic change.

## Iteration 12 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-API-ASYNC-NAMING` | every evaluated product and test method, including local functions | architecture tests | no async contract without `Async`; no `Async` marker without async contract |
| `REQ-VSB-API-CANCELLATION-SHAPE` | every externally visible product method with a token | architecture tests | token is the final declared parameter |
| existing request behavior requirements | reordered request factory signatures | core and abstraction tests | unchanged exact timeout, token, destination, context, and message forwarding |

## Iteration 12 mutation obligations

- Add `Async` to a synchronous helper or remove it from an asynchronous local function: the exact
  bidirectional architecture branch must fail.
- Move one public request-factory token before its timeout again: the cancellation-shape gate must
  report the exact member and following parameter.
- Swap or omit timeout/token forwarding at an implementation boundary: the existing request
  metadata and dependency-injection forwarding tests must fail.

## Iteration 13 outcome

Replace embedded compiler maintenance with an explicitly owned current package; remove redundant or
convenience-only compiler directives and IDE/maintenance markers; and correct confirmed API comments
whose stated timing, acknowledgement, provider, or return-value semantics disagree with the code.

## Iteration 13 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-SOURCE-DIRECTIVES` | every product directive is an exact reviewed feature-preserving exception | architecture tests | syntax-aware full product inventory equals the two-line Amazon S3 allowlist |
| `REQ-VSB-SOURCE-COMMENTS` | maintenance markers and historical/speculative construction narrative | architecture tests | Roslyn comment-trivia scan including trailing comments |
| `REQ-VSB-SOURCE-COMMENTS` | known false timing, acknowledgement, provider, and lifecycle contracts | architecture tests | zero complete-comment matches across all product sources |
| `REQ-VSB-DEPENDENCY-OWNERSHIP` | expression compilation is centrally versioned, package-owned, explicitly imported, and never embedded | architecture and product builds | exact version, owner set, source absence, and import closure |

## Iteration 13 mutation obligations

- Insert a redundant nullable directive: the exact directive inventory must fail.
- Add a trailing maintenance marker: the Roslyn comment scan must fail.
- Restore a false broker-acknowledgement contract: the semantic documentation guard must fail.
- Remove one expression-compiler import: the owning product build must fail at the call site.

## Iteration 49 outcome

Make Azure Table one coherent greenfield capability: align public namespaces and physical ownership
with the package, hide provider implementation types, normalize all composition verbs, preserve the
complete saga and bounded-journal feature set, and prove behavior against the real Azurite API.

## Iteration 49 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-AZURE-TABLE-PUBLIC-API` | exact intended exported types and greenfield names | Azure Table unit tests | reflection over the complete exported type/member surface |
| `REQ-VSB-AZURE-TABLE-CONFIGURATION` | direct saga, registered saga, runtime-type, Job Service, and message-journal composition | Azure Table unit and local-integration tests | fail-fast ownership plus real persisted behavior |
| `REQ-VSB-AZURE-TABLE-SAGA-BOUNDARY` | nulls, cancellation, insert conflict, ETag update/delete, unsupported query | Azure Table unit tests | exact exceptions, token identity, provider-call counts, and error identity |
| `REQ-VSB-AZURE-TABLE-ENTITY-CONVERSION` | native and serialized property conversion | Azure Table unit and real-provider tests | exact round trip plus malformed-value failure |
| `REQ-VSB-AZURE-TABLE-MESSAGE-JOURNAL` | finite capacity, retention, property limits, lease concurrency, and foreign-row ownership | Azure Table unit and local-integration tests | ordered transaction assertions and real concurrent Azurite writes |
| requirement projection completeness | unit and local manifests include their projection gates | both Azure Table test projects | projection tests pass against exact embedded manifests |

## Iteration 49 mutation obligations

- Remove custom-formatter key validation: the unsafe-key boundary test must fail before provider I/O.
- Stop mapping an Azure 412 write response to typed concurrency: the exact exception test must fail.
- Stop mapping an Azure 409 duplicate save to typed concurrency: the exact save contract must fail.
- Broaden the journal row-key range to include foreign rows: the real ownership test must fail.
- Permit a null repository dependency: the exact fail-fast boundary test must fail.
- Re-expose one provider implementation type: the exact exported-surface test must fail.
- Discard a saga-specific formatter during registration: the two-saga DI contract must fail on the
  exact formatter instance without conflating registrations across saga types.

## Iteration 50 outcome

Make MessagePack a self-contained greenfield serialization capability: expose only composition and
the advanced serializer factory, align namespaces and folders with the package, remove optional
Courier and Job Service product dependencies without losing their contract behavior, and make
mutable descriptors and concurrent resolver creation safe.

## Iteration 50 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-MESSAGEPACK-PUBLIC-API` | exact two-type exported surface | MessagePack tests | complete reflection inventory of exported types |
| `REQ-VSB-MESSAGEPACK-CONFIGURATION` | extension null ownership, shared concurrent serializer, independent media-type descriptors | MessagePack configuration tests | exact exceptions, reference identity, and mutation isolation |
| `REQ-VSB-MESSAGEPACK-DEPENDENCIES` | no Courier or Job Service assembly dependency | MessagePack configuration and domain tests | assembly-reference exclusion plus retained nested Job and Courier round trips |
| `REQ-VSB-MESSAGEPACK-RESOLVER` | production resolver chain selects ServiceBus mappings and generic interface formatters | MessagePack resolver tests | exact formatter types returned by the composed production options |
| `REQ-VSB-MESSAGEPACK-FORMATTER-CACHE` | concurrent cold access returns one formatter instance | MessagePack formatter tests | sixteen synchronized callers observe one reference |
| requirement projection completeness | projection gate is represented in the MessagePack manifest | MessagePack requirement tests | compiled metadata and embedded manifest are identical |

## Iteration 50 mutation obligations

- Re-expose an implementation envelope: the exact public-surface test must fail.
- Remove a composition null guard: the extension-boundary test must report the wrong exception.
- Return one mutable media-type descriptor: the isolation test and transport consumers must fail.
- Bypass the formatter cache: the synchronized resolver identity test must fail.
- Replace untrusted-data security with trusted-data mode: the security contract must fail.
- Restore a Courier product mapping and dependency: the assembly-reference contract must fail.
- Remove the ServiceBus resolver from the composed options: the production-chain selection test must fail.

## Iteration 77 outcome

Make cache and request-client lifetimes deterministic at concurrency boundaries, make absolute
deadlines truly absolute, expose factory ownership through the public contract, fail at each owning
API boundary before dependency work, and give client internals explicit physical and namespace
owners without changing request, publish, send, scoped, mediator, or cache features.

## Iteration 77 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-CACHE-MULTI-INDEX` | synchronous index projection concurrent with disposal | cache generation/locking tests | disposal cannot reach a resource while its selector is active |
| `REQ-VSB-CACHE-EXPIRATION` | absolute versus sliding usage observation | cache lifecycle tests | absolute mode never subscribes; sliding mode retains touch behavior |
| `REQ-VSB-REQUEST-CLIENT-BOUNDARY` | context, address, message, initializer, and wrapper boundaries | client boundary tests | exact argument exception occurs before endpoint or wrapped-factory work |
| `REQ-VSB-REQUEST-LIFECYCLE` | repeated/concurrent disposal and hostile synchronization context | client lifecycle tests | one owned disposal, no post-disposal creation, cancellation completes without ambient-context pumping |
| `REQ-VSB-APPLICATION-REQUEST-OPTIONS` | endpoint acquisition spans an absolute deadline | request metadata tests | send aborts after the exact deadline and implicit TTL cannot extend it |
| `REQ-VSB-SOURCE-NAVIGATION` | client internal responsibility layout | architecture tests | exact files and namespaces in `Contexts`, `Endpoints`, and `Requests`; no stale flat internals |

## Iteration 77 mutation obligations

- Remove the active-operation guard from synchronous index projection: the disposal race test must
  observe resource disposal while the selector is blocked.
- Re-enable usage subscription for absolute expiration: the exact subscriber-count test must fail.
- Move one argument guard after endpoint or wrapped-factory lookup: the boundary spy must record an
  unintended dependency call.
- Invoke the owned factory context twice or permit creation after disposal begins: the lifetime
  tests must fail on exact count or exception ownership.
- Start the deadline timer before delayed endpoint acquisition: the deadline test must observe a
  completed send after the absolute deadline.
- Restore ambient scheduler capture: the hostile-context test must observe a queued callback instead
  of prompt cancellation completion.
- Return one client internal to the former flat namespace or folder: the exact layout inventory and
  internal implementation manifest must fail.
- Remove addressed response-endpoint readiness gating: the receive-endpoint context test must fail.
- Make host response-endpoint disposal a no-op: exact owned-disposal evidence must fail.
- Let stale pending cache creation survive invalidation: creation ownership evidence must fail.
- Make response-handler disposal skip disconnection: exact handle forwarding must fail.
- Make keyed-cache removal or clearing a no-op: their independent release assertions must fail.
- Drop configured consume-pipe options: exact bus-context forwarding must fail.
- Remove response host metadata: exact response projection must fail.
- Disable direct-interface discovery in the public API extractor: the exact API architecture guard
  must fail before a changed interface contract can be accepted.

## Iteration 77 completion

All planned boundaries were implemented and all seventeen isolated mutations were killed, restored,
and followed by fresh validation. The bounded profile passes 190 tests, the complete core profile
passes 2,721 tests, and the architecture profile passes 260 tests. Bounded coverage is 92.46% line
and 86.92% branch with no method above CRAP 30. The fresh-package/API gate passes all 18 journeys,
31 packages, three isolated provider consumers, and 30 runtime API contracts with direct-interface
metadata now enforced.

## Iteration 78 outcome

Turn consumer creation, infrastructure events, metadata, and message-data storage into coherent
greenfield responsibilities. Preserve every supported feature while making lifetime ownership,
event snapshots, storage retention, cancellation, path safety, stream ownership, and timing
semantics explicit; expose only intentional application contracts; and align all source comments,
types, namespaces, filenames, and directories with their final owners.

## Iteration 78 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-CONSUMER-FACTORY-LIFETIME` | default, delegate, object, and external-instance factories on success/failure | core consumer tests | exact sync/async disposal counts, async precedence, exception propagation, and zero disposal for external instances |
| `REQ-VSB-CONSUMER-CONVENTION-STATE` | stable cache snapshot while process-global conventions are mutated | core consumer tests | serialized mutation collection plus repeated and concurrent same-version identity |
| `REQ-VSB-EVENT-SNAPSHOTS` | readiness, endpoint/transport lifecycle, and fault projections | core event tests | exact host/input identifiers, addresses, tags, metrics, time-provider values, immutable copies, and null-element rejection |
| `REQ-VSB-METADATA-OWNERSHIP` | public message metadata versus internal implementation-type creation | core and architecture tests | no redundant public facade; exact dynamic implementation caching retained through all serializers/initializers |
| `REQ-VSB-MESSAGEDATA-PUBLIC-API` | intentional application surface only | architecture and package API tests | exact exported type/member allowlist and updated deterministic packed baseline |
| `REQ-VSB-MESSAGEDATA-REPOSITORIES` | in-memory and file-system null, cancellation, retention, missing data, and path containment | core MessageData tests | fail-before-I/O guards, exact token identity, injected-time expiry boundaries, round trips, and traversal rejection |
| `REQ-VSB-MESSAGEDATA-CONVERSION` | byte/text/object/stream conversion and lazy resolution | core MessageData tests | input snapshots, null/type failures, single fetch, explicit stream-retention capability, and exact disposal ownership |
| `REQ-VSB-MESSAGEDATA-PROPERTY-PROVIDERS` | synchronously/asynchronously completed, faulted, canceled, empty, and populated inputs | core MessageData tests | identical value and exception behavior independent of task completion timing |
| `REQ-VSB-MESSAGEDATA-COMPOSITION` | selector, repository, path, policy, and convention boundaries | core MessageData tests | exact caller-parameter failures before registration, repository, or transport work |
| `REQ-VSB-SOURCE-NAVIGATION` | consumer/event/metadata/message-data owners | architecture tests | exact matching namespaces, filenames, folders, visibility, and no stale former owner |
| `REQ-VSB-SOURCE-COMMENTS` | every comment in the bounded production scope | manual review plus architecture hygiene tests | comments state only current code/function behavior; no historical, procedural, stale, filler, or generated narrative |

## Iteration 78 mutation obligations

- Skip owned-consumer disposal on success or failure, prefer synchronous disposal over
  `IAsyncDisposable`, or dispose an external instance: the corresponding exact lifetime assertion
  must fail.
- Remove convention-test serialization or return a new same-version cache snapshot: the stability
  test must fail without relying on arbitrary delays.
- Drop one event field, reuse a caller-owned mutable collection, or admit a null message type: the
  direct snapshot assertion must fail.
- Re-expose one implementation cache, converter, value, property provider, or configuration
  specification: the exact public-surface inventory must fail.
- Ignore pre-cancellation, bypass in-memory expiry, permit either adjacent expiry boundary, or allow
  a file path outside the configured root: the repository contract test must fail before unrelated
  I/O.
- Infer source-stream retention from a concrete converter type or omit a required disposal: the
  custom-converter ownership test must fail.
- Restore `.Result`, branch on task completion timing, or treat `HasValue == false` differently in
  one timing partition: paired property-provider tests must observe the mismatch.
- Let a null selector result, path, repository, converter, address, or payload reach a dependency:
  the owning boundary spy must record the unintended call or the wrong parameter name.
- Return one type to the former flat/singular namespace or top-level `Metadata` owner: the exact
  source-navigation inventory must fail.
- Restore a stale or procedural comment from the bounded inventory: the syntax-aware hygiene gate
  and the manual file ledger must reject the exact source location.

## Iteration 78 completion

All planned consumer, event, metadata, and message-data boundaries were implemented and the nine
isolated product mutations were killed and restored. The reviewed capability has 88.62% line and
80.59% branch coverage across 246 instrumented methods with no CRAP score above 30. The internal
Async Red Team converted every surviving suffix, alias, Release, same-FQN, and Quartz-interface
attack into semantic protection; its final focused profile passes 30 tests on an unchanged hash.
The final architecture assembly passes 289 tests and the complete sequential Unit solution passes
5,323 tests with no failures or skips. The protected `review/` and untracked `TestResults/` trees
remain outside the iteration commit.

## Iteration 79 outcome

Make the abstractions project root a strict application-contract boundary. Separate the
consume-scoped outgoing implementation into its owning context capability, prove all five outgoing
operations and their argument/dependency boundaries directly, pin the named conservative message
policy, and retain every public feature while making file, type, namespace, and comment ownership
deterministic.

## Iteration 79 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-SOURCE-NAVIGATION` | exact abstractions project-root files and namespaces | architecture tests | root contains only reviewed application contracts and project infrastructure |
| `REQ-VSB-SOURCE-NAVIGATION` | public outgoing contract versus internal context implementation | architecture tests | exact declared type identities in `IOutgoingMessages.cs` and `Context/ConsumeContextOutgoingMessages.cs` |
| `REQ-VSB-APPLICATION-CONSUME-OUTGOING` | routed and explicit sends | core context tests | exact route, endpoint resolution, message/options/token forwarding, and dependency-call counts |
| `REQ-VSB-APPLICATION-CONSUME-OUTGOING` | default and configured publish | core context tests | exact publish endpoint resolution, message/options/token forwarding, and dependency-call counts |
| `REQ-VSB-APPLICATION-CONSUME-OUTGOING` | scheduled send and unavailable scheduler | core context tests | exact destination/due time/message/token/result identity and fail-before-scheduler behavior |
| `REQ-VSB-APPLICATION-CONSUME-OUTGOING` | constructor and every public null boundary | core context tests | exact parameter name and zero downstream work |
| `REQ-VSB-MESSAGE-LIMITS` | named conservative policy | configuration tests | exact five values and stable singleton identity |
| `REQ-VSB-SOURCE-COMMENTS` | all comments in the bounded root scope and moved implementation | manual review plus hygiene tests | comments describe only current behavior, completion, ownership, and failure semantics |

## Iteration 79 mutation obligations

- Reinsert an implementation into an application contract file or add an unreviewed root file: the
  exact source-navigation inventory must fail on the changed path or declared type set.
- Ignore the configured route, alter the routed destination, or resolve/send more than once: the
  routed-send test must fail on exact route, address, message, token, or call count.
- Drop explicit options or replace either caller token: the direct send/publish tests must fail on
  reference identity or token equality.
- Bypass default publication or invoke a downstream provider for a null message: the publish
  partition must fail on exact call count or boundary exception.
- Ignore the consume-context scheduler, alter due time/destination/message/token, or manufacture a
  different result: the scheduled-send test must fail on exact forwarding and result identity.
- Remove one null guard or move it after dependency work: the boundary matrix must fail on exact
  parameter name or nonzero dependency calls.
- Change one value of `MessageLimits.Conservative` or allocate a replacement per access: the named
  policy contract must fail.
- Restore generic task-filler wording or stale ownership language in the bounded comments: manual
  review and the repository comment-hygiene gate must reject the changed source.

## Iteration 79 completion

The fifteen original Abstractions application-root files and the project definition were read
manually in full. The root is now an exact application-contract boundary: the internal consume
outgoing implementation moved to `Context`, all five operations and their argument/dependency
boundaries have direct tests, the real InMemory journey covers routed and default operations, and
the conservative message policy is pinned exactly. Seven isolated product/structure mutations were
killed and restored. The final Release build has no warnings or errors; the complete sequential
Unit solution passes 5,332 tests, the Async guard passes 30 tests, and fresh package/API validation
preserves the 19,773-line public contract. The overall goal remains open; iteration 80 continues
with the Abstractions `Context` owner.

## Iteration 80 outcome

Replace the mixed Abstractions `Context` bucket with explicit API-layer and runtime owners. Preserve
public send/publish proxy and scope functionality as Advanced SPI, internalize and functionally
name runtime dispatch, split each options implementation into its matching source file, relocate
Core-only consume/retry/sentinel implementations, and correct typed-proxy scope preservation. Prove
every moved behavior and boundary directly before deleting the old catch-all tests and directory.

## Iteration 80 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-SOURCE-NAVIGATION` | exact Advanced context SPI, internal dispatch/options, and Core owner layouts | architecture tests | exact paths, namespaces, type identities, visibility, and absent former `Context` directory |
| `REQ-VSB-API-LAYERING` | public proxies/scopes versus internal runtime mechanics | architecture tests | Advanced SPI remains exported; dispatchers, sentinel, outgoing facade, and pending faults are not exported |
| `REQ-VSB-RUNTIME-DISPATCH` | send, publish, response, pipe, initializer, and task forwarding | Abstractions dispatcher tests | exact generic contract, object, pipe, token, result task, and one downstream invocation |
| `REQ-VSB-RUNTIME-DISPATCH` | null, incompatible, open, value, by-ref, and pointer contracts | Abstractions dispatcher tests | exact parameter and zero downstream invocations |
| `REQ-VSB-CONTEXT-PROXY` | all send/publish getters, setters, typed messages, payload behavior, and replacement views | Abstractions proxy tests | exact state forwarding and replacement view retains the current proxy |
| `REQ-VSB-CONTEXT-SCOPE` | self/local/parent precedence, isolated add/update, cancellation, typed message, and replacement view | Abstractions scope tests | exact factory counts, identity, owner mutation, and retained local payload |
| `REQ-VSB-MISSING-CONSUME-CONTEXT` | every property and operation family | Core context tests | every member fails with the domain exception; token-bearing operations preserve pre-cancellation |
| `REQ-VSB-PENDING-FAULTS` | validation, sealing, concurrency, cancellation, complete notification, and sync/async failures | Core retry tests | exact data/token forwarding and all collected notifications are attempted |
| `REQ-VSB-APPLICATION-OPTIONS-*` | every options type, snapshot, apply, partition capability, and probe | Abstractions options tests | exact immutable snapshot, fields, failure order, and scope name |
| `REQ-VSB-SOURCE-COMMENTS` | all comments in the original and final bounded scope | manual review plus hygiene gates | only current functional, state, ownership, and failure semantics remain |

## Iteration 80 mutation obligations

- Return a replacement send view over the wrapped parent instead of the current proxy: the local
  payload-retention assertion must fail.
- Omit or redirect any send/publish proxy getter or setter: the complete property matrix must fail
  on exact value or recorded mutation.
- Drop a pipe, token, explicit runtime type, values object, or returned task from any dispatcher
  overload: the exact invocation and task-identity tests must fail.
- Re-export one dispatcher, sentinel, outgoing implementation, or pending-fault collection, or
  restore the former mixed directory: the API-layer/source-owner architecture test must fail.
- Stop notification enumeration after one synchronous observer failure: the every-fault attempt
  test must observe the missing later call.
- Let a late fault enter after sealing, seal on pre-cancellation, or permit a second notification:
  the collection state tests must fail.
- Change payload lookup precedence, invoke an unnecessary factory, update a parent payload in
  place, or lose the typed message: the scope identity and factory-count tests must fail.
- Apply an unset option, share caller-owned headers, mutate context before rejecting an unsupported
  partition key, or use the wrong probe scope: the options tests must fail.
- Restore a stale path, cache-centric public description, or operation-forwarding claim that the
  proxy does not implement: the exact source inventory and manual comment ledger must reject it.

## Iteration 80 completion

All original `Abstractions/Context` files, their contracts, callers, final owners, and comments were
read and adjudicated manually. The public proxy/scope SPI moved to `Advanced/Contexts`; runtime
dispatch and option snapshots became internal implementation owners; Core-only consume and retry
types moved into Core; and the former mixed directory was removed. The send proxy now retains its
current payload scope across typed replacement, and pending fault notification attempts every
collected fault even when an observer throws synchronously or returns no task.

Eight isolated mutations were killed and restored. The final Release Unit-solution build has zero
warnings and errors, the complete Unit solution passes 5,356 tests, and the fresh package/API gate
passes all 18 journeys, 31 packages, three provider consumers, and 30 API assemblies. The three
executable Core files reach 136/136 covered lines and 20/20 covered branches. The old directory,
empty source directories, preprocessor directives, stale public cache/sentinel exports, and bounded
dummy/legacy markers are absent. The repository-wide A+ source goal remains active.

## Iteration 81 outcome

Make Advanced context metadata and consume-scoped send operations precise, idiomatic, directly
tested, and navigable while preserving the coherent project boundary. Correct inconsistent message
body boundaries discovered while validating the context contract, and explicitly retain the
cross-project mediator-body and context-interface naming decisions for their complete owning passes.

## Iteration 81 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-ADVANCED-CONTEXT-KEYS` | consume/send partition and routing lookup | Abstractions Advanced-context tests | exact payload value, absence result, and null receiver |
| `REQ-VSB-ADVANCED-CONTEXT-KEYS` | set, clear, try-set, unsupported capability | Abstractions Advanced-context tests | exact payload mutation, boolean result, exception, and no unrelated mutation |
| `REQ-VSB-CONSUME-SEND` | all ten typed/runtime/initialized overloads | Abstractions Advanced-context tests | exact endpoint, message/type/values/pipe/token forwarding and one dependency call |
| `REQ-VSB-CONSUME-SEND` | receiver, destination, value/type/pipe boundaries | Abstractions Advanced-context tests | declaration-order parameter ownership and zero endpoint resolution/send calls |
| `REQ-VSB-CONSUME-SEND` | context token, caller token, shared token, and linked tokens | Abstractions Advanced-context tests | exact token identity where possible and cancellation from either linked source |
| `REQ-VSB-CONTEXT-TRANSFER` | reused send context receives a new consume scope | Abstractions Advanced-context tests | current metadata and payload identify the same consume context |
| `REQ-VSB-CONTEXT-TRANSFER` | host, fault, causality, identifier, and redelivery metadata | Abstractions Advanced-context tests | exact copied headers, lineage, source, and payload behavior |
| `REQ-VSB-RECEIVE-METADATA` | direct, textual, and `DateTime` timestamp representations | Abstractions context tests | equal UTC result for every accepted representation |
| `REQ-VSB-RETRY-METADATA` | attempt/count/redelivery reads and null receivers | Abstractions Advanced-context tests | exact one-based semantics, zero pre-retry state, and parameter ownership |
| `REQ-VSB-EMPTY-HEADERS` | singleton shape, empty reads, enumeration, and invalid keys | Abstractions serialization tests | sealed type, stable property identity, empty values, and exact key failures |
| `REQ-VSB-SERIALIZER-CONTEXT` | header-provider parameter identity | Core serialization boundary tests | both generic overloads reject a null `headers` parameter by its public name |
| `REQ-VSB-MESSAGE-BODY-CONTRACT` | default segment and required constructor inputs | owning body tests | consistent empty views and fail-fast exact parameter names |
| `REQ-VSB-SOURCE-NAVIGATION` | project roots and Advanced context types | architecture tests and manual ledger | one assembly per project root, provider grouping retained, exact file/type/namespace owner |
| `REQ-VSB-SOURCE-COMMENTS` | every comment in the bounded files | manual review plus hygiene gates | current behavior only; no history, procedural narrative, filler, or unsupported promise |

## Iteration 81 mutation obligations

- Restore noun-shaped `PartitionKey()` or `RoutingKey()`, use the wrong capability payload, or
  report success without assignment: the exact API and behavior tests must fail.
- Validate a later consume-send parameter before the receiver/destination, resolve an endpoint for
  rejected input, or route through the wrong overload: the parameter-order and recording-endpoint
  assertions must fail.
- Drop either cancellation source, create an unnecessary linked token for one source, or use a
  different token for resolution and send: the four token partitions must fail.
- Reintroduce divergent default-array body views or defer a required native/text constructor
  failure: the direct body-boundary tests must fail on value, accessor, timing, or parameter name.
- Retain an earlier consume-context payload while copying later metadata, or skip UTC normalization
  for a direct `DateTimeOffset`: the exact identity/offset assertions must fail.
- Remove retry receiver validation, permit an invalid empty-header key, or restore the misleading
  `dictionary` name for a header provider: direct boundary checks or the XML documentation contract
  must fail on the exact public parameter.
- Split the public Advanced namespace cosmetically, move a provider project into an unrelated root,
  or add a second type to a context file: the source-navigation inventory must fail.
- Restore historical repair prose, generic "member" wording, an inaccurate copy/ownership claim,
  or an incomplete linked-cancellation description: manual review and comment hygiene must reject
  the exact location.

## Iteration 81 completion

The 31 Advanced context files and every followed source dependency changed by this iteration were
read manually in full, including their comments, types, namespaces, filenames, and physical owners.
The repository retains Core and sibling feature packages as independent project roots, while
Persistence, Scheduling, and Transports remain category roots for provider projects. This is an
ownership distinction, not an accidental duplicate source tree, and no cosmetic relocation is
introduced.

All planned context-key, consume-send, cancellation, context-transfer, receive-metadata, retry,
empty-header, serializer-parameter, and message-body boundaries are implemented and directly
protected. Ten isolated counterchanges were killed and restored. The final Release build has zero
warnings and errors; the complete sequential Unit solution passes 5,392 tests, including 292
architecture tests; and whitespace plus warn-level style gates pass. Fresh-package validation
passes 18 journeys, 31 packages, three isolated provider consumers, and all 30 runtime API
assemblies. The deliberate 19,701-line package API has SHA-256
`982dc572231657c53b09f70a396f7cdec26ac93fe07401f06eb681da1931a6a1` and reproduces on a second
unchanged run.

Core-module coverage is 70.09% line (43,449/61,986) and 62.70% branch
(14,990/23,907). It is explicitly module instrumentation rather than a fabricated whole-suite
percentage because the direct Abstractions test project has no MTP coverage provider. Global source
hygiene finds no C# preprocessor directives, dummy/stub/TODO/FIXME markers, or empty directories.
The non-readable mediator body, mutable body-array ownership, and repository-wide context-interface
naming remain queued for complete owning passes so no cross-cutting API decision is made from a
partial inventory.

The separate internal Async Red Team passes the bidirectional semantic naming audit over all 4,112
physical production C# files: the 30-test guard and two independent compilation-based scanners find
no mismatch, no `async void`, and no hidden conditional-compilation case. Its documentation axis
identified eight generic Task return comments in this iteration's fully read files; all eight are
now operation-specific. Another 698 remain in source owners not yet manually read. They form
an explicit cross-iteration worklist and will be rewritten only after each owning method has been
read and understood; no comment generator or bulk substitution may be used.

## Iteration 82 outcome

Make the Abstractions serialization owner internally minimal, culture-invariant, symmetric, and
directly tested. Remove the internal camel-case lookup mechanic from the public API, align metadata
key validation and duplicate handling in both dictionary directions, relocate the complete
extension boundary contract to its actual test owner, and manually correct every stale comment in
the owner and any fully read dependency. Resolve the cross-project message-body capability only
after independent Red-Team analysis of every implementation and caller.

## Iteration 82 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-CAMEL-CASE-METADATA` | exact JSON camel-case normalization | Abstractions serialization tests | Turkish culture, ordinary PascalCase, acronym prefix, mutable and read-only dictionaries |
| `REQ-VSB-CAMEL-CASE-METADATA` | required key and API visibility | Abstractions serialization tests | null/empty/blank rejection and non-public compile-bound type |
| `REQ-VSB-SERIALIZER-CONTEXT-EXTENSIONS` | dictionary/header-provider reads | Abstractions serialization tests | exact and camel-case lookup, reference/value conversion, fallback, and zero conversion on absence |
| `REQ-VSB-SERIALIZER-CONTEXT-EXTENSIONS` | try-get semantics | Abstractions serialization tests | present, absent, and rejected conversion for reference and value types |
| `REQ-VSB-SERIALIZER-CONTEXT-EXTENSIONS` | dictionary serialization/deserialization | Abstractions serialization tests | null filtering, valid keys, case-insensitive last value, empty normalization, and exact delegation |
| `REQ-VSB-SERIALIZER-CONTEXT-EXTENSIONS` | consume/send header reads | Abstractions serialization tests | consume conversion, direct text, defaults, send runtime-type requirement, and missing values |
| `REQ-VSB-SERIALIZER-CONTEXT-EXTENSIONS` | consume object projection | Abstractions serialization tests | exact source identity and returned dictionary identity |
| `REQ-VSB-SERIALIZER-CONTEXT-EXTENSIONS` | every required public boundary | Abstractions serialization boundary tests | every reference/value overload, exact parameter, and zero dependency calls |
| `REQ-VSB-SOURCE-NAVIGATION` | serialization test ownership | requirement gates and manual ledger | boundary test and immutable requirement entry belong to Abstractions, not Core |
| `REQ-VSB-SOURCE-COMMENTS` | all owner comments and followed header contract | manual review plus hygiene gates | only current function, representation, ownership, and failure semantics |

## Iteration 82 mutation obligations

- Restore current-culture or one-character lowercasing: the Turkish or acronym contract must fail.
- Export the camel-case helper again: the visibility contract must fail.
- Remove key validation or perform dictionary/header work first: boundary tests must fail on exact
  parameter identity or unexpected dependency invocation.
- Use `Dictionary.Add` during deserialization or preserve the first case-insensitive duplicate: the
  symmetric last-value assertion must fail.
- Deserialize an absent key, serialize an all-null dictionary, convert direct send headers, or
  deserialize an already textual consume header: call counts and exact values must fail.
- Return a different object projection or pass a replacement source: identity assertions must fail.
- Move the boundary test back to Core or restore its Core requirement entry: owner and requirement
  completeness gates must reject the mismatch.
- Restore generic header-provider or serializer filler documentation: the manual comment ledger must
  reject the fully read source.

## Iteration 82 completion

All 22 Abstractions Serialization files were read manually in full and adjudicated together with
their tests, public callers, and concrete body dependencies. The internal metadata lookup is no
longer exported; ordinary and acronym-prefixed keys use the same invariant JSON camel-case rule;
invalid retained metadata keys fail at their owning boundary; both dictionary directions use
case-insensitive last-value semantics; and every one of the sixteen public extension overloads now
has direct reference/value behavior and boundary evidence. The former Core boundary test and its
requirement entry moved to the owning Abstractions test project. `EmptyHeaders` remains under
Serialization because its namespace and test owner already match that path.

Seven valid test-first failures and four isolated counterchanges prove the corrected semantics and
API visibility. The final Release Unit-solution build has zero warnings and errors. The complete
sequential Unit solution passes 5,405 tests with no failures or skips, including all 292 architecture
tests; the Abstractions assembly contributes 588 direct tests. Repository-wide whitespace and
warn-level style verification pass. Fresh-package validation passes 18 developer journeys, 31
packages, three isolated provider-testing consumers, and 30 runtime API assemblies. The reviewed
19,698-line packed API contract has SHA-256
`29152df6b6532748a802e66de982ab65db03479397c235cb855c4764e8665ece` and matches the independently
regenerated artifact byte-for-byte.

Global source hygiene remains at zero C# preprocessor directives, zero empty source directories,
and zero dummy/stub/TODO/FIXME/compatibility-shim markers. The fully read Serialization owner and
followed `IHeaderProvider` contain none of the known generic task/header/operation filler. The
independent body Red Team reports FAIL for the existing cross-project `MessageBody` contract and
defines the next atomic owner: immutable byte ownership, explicit text encoding, readable Mediator
content, all implementations/callers, adversarial mutation tests, and an Embedded-target allocation
gate. This is retained as an explicit next iteration rather than hidden by a partial local fix.

## Iteration 83 outcome

Replace the implementation-defined `MessageBody` capability with one Greenfield contract that is
always materialized, length-known, byte-stable, independently streamable, and explicit about the
text representation required by text-only transports. Consolidate redundant array, byte-array, and
memory implementations without losing segment, empty, UTF-8, Base64, JSON, MessagePack, native
provider, mediator, forwarding, durable, journal, outbox, or scheduling behavior.

## Iteration 83 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-MESSAGE-BODY-CONTRACT` | public shape and concrete owner census | Abstractions, Core, MessagePack, and provider tests | exact length, defensive `byte[]` copies, independent `OpenReadStream`, optional transport text, sealed concrete types |
| `REQ-VSB-BINARY-MESSAGE-BODY` | selected memory, empty content, ownership | Abstractions serialization tests | exact segment, constructor snapshot, source mutation isolation, no implicit binary-to-text conversion |
| `REQ-VSB-TEXT-MESSAGE-BODY` | UTF-8 text and Base64 carrier text | Abstractions serialization tests | exact bytes/text, eager validation, source stability, malformed Base64 boundary |
| `REQ-VSB-MESSAGE-BODY-STREAM` | repeatable independent streams | shared owner assertions | position zero, read-only behavior, independent positions and disposal, unchanged content |
| `REQ-VSB-SERIALIZED-BODY-SNAPSHOT` | JSON and MessagePack materialization | Core and MessagePack tests | source mutation isolation, accessor order, concurrent access, exact bytes, single serialization |
| `REQ-VSB-MEDIATOR-BODY` | bounded canonical JSON materialization before dispatch | Core mediator and architecture tests | readable exact body, byte length, oversize rejection, cancellation, dispatch ordering, bounded growth |
| `REQ-VSB-NATIVE-BODY-SNAPSHOT` | ActiveMQ, SQS, and Azure Service Bus adapters | owning provider tests | constructor-time snapshot, no caller/native mutation leakage, exact text/binary representation |
| `REQ-VSB-BODY-CROSS-OWNER` | forwarding, move, persistence, journal, outbox, scheduling | owning unit/integration tests | byte-for-byte transport paths and exact text-only carrier paths preserve all features |
| `REQ-VSB-SOURCE-NAVIGATION` | body filenames, namespaces, and visibility | architecture tests and manual ledger | one type per file, no redundant binary implementation, provider adapters remain with providers |
| `REQ-VSB-SOURCE-COMMENTS` | every fully read body and caller file | manual review plus hygiene gates | current ownership, encoding, stream, failure, and completion semantics only |

## Iteration 83 mutation obligations

- Return caller/native memory directly, delay a snapshot, or expose a mutable array: mutation after
  construction and returned-view adversarial tests must observe the breach.
- Remove exact length or defensive byte-copy access, or make optional transport-text discovery
  throw for an opaque body: public-shape and mediator contract tests must fail.
- Reuse one stream, expose a writable stream, inherit position, or let disposal affect later reads:
  independent-stream tests must fail.
- Decode opaque binary bytes as text or conflate Base64 carrier text with decoded bytes:
  exact-representation and capability tests must fail.
- Serialize JSON or MessagePack more than once, race first materialization, or observe later source
  mutation: concurrency, invocation-count, and snapshot tests must fail.
- Measure mediator content without retaining it, allocate beyond its declared hard bound, dispatch
  before materialization, or lose cancellation: direct and architecture tests must fail.
- Read SQS, NMS, or BinaryData again after construction, or return native storage: provider mutation
  tests must fail.
- Decode a binary body at a byte-only destination or Base64-encode a textual body at a text-only
  destination: cross-owner transport and persistence assertions must fail.
- Restore `ArrayMessageBody` or `MemoryMessageBody`, keep an unsealed concrete body, or export a
  provider-only implementation: compile-bound census and public API review must fail.
- Retain implementation-defined ownership prose, ambiguous `GetString`, or obsolete measured-only
  mediator comments: manual review and documentation checks must reject the exact source.

## Iteration 83 completion

The complete public body contract, every production implementation, every changed byte/text
consumer, and their directly affected comments were read manually before the final design was
accepted. `MessageBody` is now one immutable, materialized contract with an exact `Length`, a
defensive `ToArray`, an independent read-only `OpenReadStream`, and an optional explicit transport
text capability. The three overlapping array/memory implementations were replaced by one sealed
`BinaryMessageBody`; JSON, MessagePack, Mediator, native-provider, forwarding, journal, durable,
outbox, scheduling, and SQL owners now preserve the same snapshot and representation rules.

The apparently repeated `ViciOne.ServiceBus` path was also adjudicated during this pass. The
directory `src/ViciOne.ServiceBus` is the Core project, while the sibling
`src/ViciOne.ServiceBus.*` directories are separate product assemblies. Persistence, Scheduling,
and Transports remain repository-level provider groups containing separate adapter projects. This
topology makes assembly and dependency ownership visible; nesting sibling projects inside the Core
project would create a false ownership relationship and SDK glob hazards. Genuine type, namespace,
filename, and folder mismatches will still be corrected in each complete owning pass.

Five isolated counterchanges were compiled, executed, killed, and restored: forwarding with a copy
serializer, non-Base64 Quartz persistence, non-Base64 Entity Framework outbox persistence, ActiveMQ
text transport for binary MessagePack, and acceptance of the native SNS wrapper instead of its
payload. The final sequential Release Engineering build and both formatting gates have zero
warnings, errors, or changes. The complete Unit solution passes 5,477 tests with no failures or
skips. Fresh package validation passes 18 developer journeys, 31 packages, three isolated provider
consumers, and all 30 runtime API assemblies. The deliberate 19,674-line public API has SHA-256
`7841eea6a51d14b0dfbe8062838e5d1cacb10248b55da34ad5add0f6f0cc186d`.

Real-provider acceptance passes for ActiveMQ OpenWire and AMQP (2/2), Amazon SQS/SNS through
LocalStack (1/1), PostgreSQL (1/1), and SQL Server (1/1). The new cross-owner tests additionally
prove successful in-memory forwarding, Quartz store/rehydrate/deliver, and classic Entity Framework
outbox store/replay with exact MessagePack binary payloads. An internal read-only Red Team found no
remaining Critical or High implementation defect; its originally missing cross-owner acceptance
cases are now implemented. This internal review is supporting evidence, not an independent external
acceptance.

Core-host instrumentation reports 43,447 of 62,001 lines (70.07%) and 14,967 of 23,883 branches
(62.67%). The separate Quartz host passes 216 tests and instruments the Quartz product project at
98.01% line and 85.36% branch coverage. These figures are reported per instrumented host rather
than merged or extrapolated into a false whole-suite percentage; repository-wide merged coverage
remains a later dedicated owner because most test projects do not yet carry the MTP coverage
provider. Source hygiene finds no C# preprocessor directives, no empty source directories, and no
dummy, stub, TODO, FIXME, or compatibility-shim marker. The remaining generic asynchronous return
comments belong to source owners not yet manually read and remain queued for their mandatory
file-by-file passes; no comment generator or bulk rewrite is used.

## Iteration 84 outcome

Make the complete MessagePack assembly a binary-owned Greenfield serialization boundary. Remove
the inherited inner Base64/object payload compatibility path while retaining the explicit outer
Base64 carrier required by text-only transports. Align serializer-independent metadata conversion,
fail fast at every owned construction and configuration boundary, preserve cancellation, and close
the whole assembly with direct coverage and mutation evidence.

## Iteration 84 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-MESSAGEPACK-ENVELOPE` | binary-only payload and immutable ownership | MessagePack envelope tests | compile-bound `byte[]` property, independent constructor/clone bytes, null metadata projection |
| `REQ-VSB-MESSAGEPACK-DESERIALIZATION` | native and overlay decoding, malformed input, cancellation | MessagePack serializer-context tests | supported/unsupported paths, exact boundaries, false for malformed payload, observable cancellation |
| `REQ-VSB-MESSAGEPACK-OBJECT-SERIALIZATION` | direct, dictionary, scalar, JSON text, binary, blank, and JSON null | MessagePack object tests | exact value/default result for every serializer-independent metadata form |
| `REQ-VSB-MESSAGEPACK-CONFIGURATION` | endpoint/bus, serializer/deserializer, both default states | MessagePack configuration tests | exact call, factory, default flag, ordering, and shared bidirectional factory |
| `REQ-VSB-MESSAGEPACK-FORMATTER-CACHE` | complete type/factory/delegate boundary | MessagePack formatter tests | null, interface, abstract, unrelated type, concurrency, failure caching, and weak-key behavior |
| `REQ-VSB-MESSAGEPACK-MESSAGE-DATA` | inline text/bytes, external reference, empty handle, and wire nil | MessagePack data tests | exact value/address/ownership and canonical empty normalization |
| `REQ-VSB-MESSAGEPACK-FORWARDING` | private snapshot, overlay, admission, and supported types | MessagePack forwarding tests | repeated owned bodies, exact overlay behavior, capacity-path survival, no legacy payload form |
| `REQ-VSB-MESSAGEPACK-SCHEDULING` | text-backed Quartz carrier | Quartz integration tests | exact binary payload, canonical outer Base64, metadata identifiers, and application header |
| `REQ-VSB-MESSAGEPACK-COVERAGE` | complete assembly host | native MTP coverage run | package line/branch rates, exact uncovered classification, no aggregate-host extrapolation |
| `REQ-VSB-SOURCE-NAVIGATION` | all fourteen production files | manual owner ledger | one primary type per file and matching root/Serialization/Formatters ownership |
| `REQ-VSB-SOURCE-COMMENTS` | every comment in all fourteen files | manual review plus format/hygiene gates | current behavior and failure semantics only; no history, filler, or generated rewrite |

## Iteration 84 mutation obligations

- Return the caller's serialized byte array instead of a snapshot: the payload-ownership test must
  fail on reference identity before content corruption can be hidden.
- Replace JSON metadata parsing with absence/default behavior: the complete reference-input test
  must fail on the JSON object projection.
- Omit the receive-side registration from the endpoint serializer composition: both default-state
  rows must fail on the exact missing bidirectional operation.
- Accept an unrelated concrete type in the formatter cache: the complete type-boundary test must
  fail before delegate compilation.
- Ignore the retained overlay bytes when payload admission is active: the forwarding test must
  observe the original value instead of the applied replacement.
- Remove cancellation propagation from `TryGetMessage`: the cancellation contract must fail because
  the abort is converted into an ordinary unsupported/invalid result.

## Iteration 84 completion

All fourteen MessagePack production files and 1,345 physical lines were read manually in full,
together with their direct tests and the Quartz scheduling integration. The envelope now owns only
encoded `byte[]` payloads; all byte-bearing constructors and clones take defensive snapshots; and
the old inner object/Base64 compatibility branch is gone. The outer Base64 message body remains as
the explicit lossless carrier for text-only brokers and schedulers, so no transport capability was
lost. Metadata strings now use the shared JSON contract rather than being guessed as Base64.

Every formatter/cache delegate and serializer/configuration parameter has a direct exact-owner
boundary. Cancellation raised by MessagePack callbacks is unwrapped and remains observable rather
than being converted to `false`. Quartz delivery additionally proves that an application header
survives the real stored MessagePack scheduling path. Seven red executions cover the initial
cancellation defect and six isolated counterchanges; every counterchange was restored immediately.

The final MessagePack host passes 113/113 tests with no failures or skips. Its package reports
99.36% line coverage and 97.75% branch coverage. The only uncovered handwritten line sequence
points are the compiler-emitted continuations after two non-returning
`ExceptionDispatchInfo.Throw` calls. The remaining partial branches are defensive invariant guards
for a non-null JSON object/dictionary projection and a statically declared formatter table, plus a
MessagePack source-generator branch; none is represented as executed evidence.

The normal sequential Release Engineering build has zero warnings and errors. The complete
sequential Unit solution passes 5,485 tests with no failures or skips. Both repository format gates
and `git diff --check` pass. Fresh-package verification passes 18 developer journeys, 31 packages,
three isolated provider-testing consumers, and all 30 runtime API assemblies. The packed public API
remains exactly 19,674 lines with SHA-256
`7841eea6a51d14b0dfbe8062838e5d1cacb10248b55da34ad5add0f6f0cc186d`.

The reviewed assembly has no preprocessor directives, dummy/placeholder implementation, optional
Courier/Job Service dependency, file/type/namespace mismatch, or stale construction-history
comment. Repository-wide source hygiene still reports zero C# preprocessor directives and zero
empty source directories. The complete A+ source goal remains active for the next unreviewed owner.

## Iteration 85 outcome

Review the complete initializer feature as one coherent owner: the optional
`ViciOne.ServiceBus.Initializers` API assembly and the Core initializer engine that implements its
conventions, factories, contexts, header/property initialization, property providers, and type
conversion. Preserve the separate package boundary unless the fully read dependency graph proves a
better Greenfield ownership model. Make every public and internal boundary explicit, cancellation-
correct, deterministic, testable, and discoverable; align type, namespace, filename, folder, and
comment ownership without compatibility-only API or feature loss.

## Iteration 85 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-INITIALIZER-API` | typed/runtime send, publish, request, and schedule entry points | Core initializer and contract tests | every overload, exact forwarding, null boundaries, cancellation, and unsupported-capability behavior |
| `REQ-VSB-INITIALIZER-VARIABLES` | identifier and UTC timestamp capture/reuse | variable and integration tests | explicit value, generated value, per-initialization reuse, cross-initialization isolation, and cancellation |
| `REQ-VSB-INITIALIZER-CONTEXTS` | payload, values, headers, and scoped state | context and initializer tests | exact propagation, isolation, missing value behavior, and deterministic ownership |
| `REQ-VSB-INITIALIZER-CONVENTIONS` | object, dictionary, dynamic, and registered convention selection | convention and registry tests | precedence, unsupported input, concurrency, cache behavior, and exact selected factory |
| `REQ-VSB-INITIALIZER-FACTORIES` | message/header/property factory construction | factory and contract tests | complete parameter validation, stable inspection, immutable plans, and failure boundaries |
| `REQ-VSB-INITIALIZER-CONVERTERS` | scalar, nullable, collection, dictionary, object graph, task, variable, and message-data conversion | converter tests | positive, negative, null, boundary, cancellation, and exact-type behavior |
| `REQ-VSB-INITIALIZER-PROVIDERS` | synchronous/asynchronous input and converted property values | provider tests | exact value, task completion/failure/cancellation, invocation count, and context propagation |
| `REQ-VSB-INITIALIZER-HEADERS` | copied, dictionary, provided, string, and fixed headers | header tests | normalization, overwrite semantics, invalid keys/values, exact output, and cancellation |
| `REQ-VSB-INITIALIZER-OWNERSHIP` | optional API package versus Core implementation dependency | architecture tests and manual ledger | no cycle, no hidden facade, no accidental default-glob ownership, and minimal explicit imports |
| `REQ-VSB-SOURCE-NAVIGATION` | all package and Core initializer files | architecture tests and manual ledger | one primary type per file where practical and matching type/namespace/folder ownership |
| `REQ-VSB-SOURCE-COMMENTS` | every comment in the complete owner | manual review plus hygiene gates | current code/function semantics only; no history, filler, workaround, or generated prose |

## Iteration 85 mutation obligations

- Remove or misroute any public initializer overload: its exact forwarding, boundary, or
  cancellation test must fail.
- Reuse a variable across initialization scopes or create a new value per property: reuse and
  isolation assertions must fail.
- Change convention precedence, cache the wrong input/message pair, or accept an unsupported shape:
  convention and concurrency tests must fail.
- Skip an owned null/type/key validation or invoke a dependency before validation: exact parameter
  and zero-side-effect assertions must fail.
- Collapse nullable, collection, dictionary, task, variable, or message-data conversion into an
  apparently similar path: positive and negative partition tests must observe the semantic change.
- Invoke an asynchronous provider twice, hide its cancellation/failure, or use the wrong context:
  invocation-count and propagation tests must fail.
- Restore a broad unused global import, relocate a sibling assembly under the Core directory, or
  keep a type/file/namespace mismatch: architecture and manual ownership checks must fail.
- Retain a stale construction-history comment or replace understood semantics with generic filler:
  the manual full-source ledger must reject the exact file.

## Iteration 85 completion

All 91 Core initializer files and all nine files in the optional initializer package were read
manually in full. No generator or scripted comment rewrite was used. Every comment was checked
against the implementation while its owning file was reviewed. Generic filler and construction-
history prose were removed, and the remaining documentation describes current behavior,
boundaries, cancellation, or failure semantics.

`ViciOne.ServiceBus` remains the Core project. Persistence, Scheduling, Transports, Initializers,
and the other capability packages remain sibling projects beneath `src` because they have
independent package and dependency boundaries. Within the initializer owners, folders,
namespaces, filenames, and types now agree: optional variables live in `Variables`, the unused
optional-package global imports are gone, and implementation-only convention, cache, factory,
provider, converter, and initializer types are internal. Compatibility-only public cache
interfaces were removed without removing runtime behavior.

The implementation now preserves cancellation and dependency faults across collection, task,
variable, header, message-data, and provider paths; rejects null-returning asynchronous delegates;
uses input contexts without inventing another message-object depth; initializes concrete
dictionary object graphs; applies invariant and range-safe temporal, numeric, string, and enum
conversion; and keeps cache and provider behavior deterministic. Public extension APIs have direct
typed/runtime forwarding, validation, cancellation, probe, pipe, request, publish, send, and
schedule coverage.

The final Initializers selection passes 175/175 tests. Its unique aggregate coverage across the
Core and optional initializer source is 2,404/2,461 lines (97.68%) and 1,045/1,160 branches
(90.09%). The uncovered code is not represented as executed evidence: it is dominated by 41
defensive false-return paths in the nested property-provider factory and two double-checked cache
race paths, with a small remainder of isolated defensive null and exception branches.

Four isolated manual counterchanges were compiled and executed: increasing input-view depth,
rejecting concrete dictionary object graphs, ignoring a pre-canceled exact-array conversion, and
accepting an out-of-range Unix timestamp. Each intended test failed for the changed semantic. Each
source file was then restored manually and verified byte-for-byte against its pre-mutation SHA-256
before the final build and test runs.

The restored final Core host passes 2,879/2,879 tests, and the complete architecture host passes
292/292 tests, both without failures or skips. The sequential Release Engineering build has zero
warnings and errors. Format verification, `git diff --check`, and the requirement-projection gate
pass. Fresh-package verification passes 18 developer journeys, 31 packages, three isolated
provider-testing consumers, and all 30 runtime API assemblies. The packed public API is 19,265
lines with SHA-256 `09ca6bb86e6d74034de1689eddae0d3e35b914f93b8d6f0a52b60091f22131e4`.

Repository-wide source hygiene reports zero C# preprocessor directives and zero empty source
directories. The only textual placeholder and `NotImplementedException` matches are respectively
a real schedule-declaration placeholder in state-machine semantics and a real technical-failure
classification; neither is dummy implementation. The complete A+ source goal remains active for
the next unreviewed owner.

## Iteration 86 outcome

Review the complete Core batching owner as one coherent runtime capability: batch context and
message projection, collection and release, lifetime ownership, consumer dispatch, factory
construction, and runtime settings. Preserve batching features while making ordering, timing,
capacity, cancellation, fault, disposal, concurrency, and boundary semantics explicit and
deterministic. Align every type, namespace, filename, folder, and manually reviewed comment with
its final responsibility.

## Iteration 86 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-BATCH-CONTEXT` | batch projection over member consume contexts | context tests | identity, ordering, metadata, payload access, cancellation, and invalid boundaries |
| `REQ-VSB-BATCH-COLLECTION` | collect, capacity release, timeout release, and shutdown release | collector tests | exact membership/order, trigger precedence, no loss/duplication, and terminal state |
| `REQ-VSB-BATCH-CONCURRENCY` | simultaneous delivery and release signals | lifecycle and runtime tests | single release, race safety, stable counts, and no post-terminal mutation |
| `REQ-VSB-BATCH-TIME` | time-provider-driven delivery limits | time-provider tests | deterministic boundaries, delayed completion, cancellation, and no wall-clock dependency |
| `REQ-VSB-BATCH-DISPATCH` | consumer invocation and result propagation | integration tests | exact batch, dependency context, success, fault, and cancellation propagation |
| `REQ-VSB-BATCH-FACTORY` | consumer/factory construction and lifetime | factory and lifecycle tests | parameter validation, exact dependencies, cleanup, and repeated creation isolation |
| `REQ-VSB-BATCH-SETTINGS` | normalized runtime capacity, timeout, and concurrency | runtime-state tests | defaults, valid boundaries, invalid values, and immutable runtime snapshot |
| `REQ-VSB-SOURCE-NAVIGATION` | all eight batching files | architecture tests and manual ledger | one primary type per file where practical and matching type/namespace/folder ownership |
| `REQ-VSB-SOURCE-COMMENTS` | every comment in the complete owner | manual review plus hygiene gates | current behavior only; no history, filler, workaround, or generated prose |

## Iteration 86 mutation obligations

- Reorder, omit, or duplicate a collected message: exact identity and ordering assertions must
  fail.
- Move a capacity or timeout boundary by one item or one time tick: boundary tests must fail.
- Allow two competing release paths to win: concurrency tests must observe duplicate dispatch or
  an invalid terminal transition.
- Replace the injected time source with wall-clock time: deterministic time-provider tests must
  fail without sleeping.
- Hide a consumer fault or cancellation, or release after shutdown incorrectly: propagation and
  lifecycle assertions must fail.
- Remove owned parameter validation or retain mutable settings: exact boundary and snapshot tests
  must fail.
- Retain a stale construction-history comment or a type/file/namespace mismatch: the manual source
  ledger and architecture checks must reject the exact file.

## Iteration 86 completion

The complete final batching API/runtime inventory and every directly affected production consumer
were read manually in full. No generator or scripted comment rewrite was used. Each comment was
checked while its implementation was understood, including batching contexts and runtime,
configuration and conventions, dependency-injection observers, in-memory outbox integration,
Job Service convention selection, JSON conversion, and the public Abstractions contracts. The
Core assembly remains `src/ViciOne.ServiceBus`; Persistence, Scheduling, Transports, Initializers,
and the other dependency-bearing capabilities remain sibling projects under `src`. Within every
reviewed project, the final namespaces, folders, filenames, and primary types express their actual
ownership.

The public collection contract is now the Greenfield `IMessageBatch<TMessage>` interface under
`Advanced`. It inherits `IReadOnlyList<ConsumeContext<TMessage>>`, exposes the standard `Count`
and indexer shape, and no longer carries the redundant legacy `Length` member. The former public
`Batch<TMessage>` contract is absent from product assemblies and the packed API. Remaining
`Batch<TMessage>(...)` source occurrences are only the intentional receive-endpoint configuration
verb. Completion mode now shares the same public capability folder, while grouping adapters are
internal implementation details under `Configuration/Consumers`.

Collection timing now starts with the first successfully admitted unique message. `FromLast`
restarts only after a later unique admission, while duplicate identifiers cannot replace the
accepted context, timestamps, log context, or admission activity. `MessageBatch<TMessage>` rejects
empty snapshots, null member contexts, undefined completion modes, and reversed timestamps before
capturing an immutable ordered copy. Runtime settings snapshot validated options, default values
are canonical across API and runtime, and the configuration callback accepts an optional endpoint
name without nullable suppression.

Connection teardown has one dedicated `BatchConsumerConnectHandle` owner. Synchronous disconnect,
`Dispose`, and `DisposeAsync` share one idempotent operation; new admissions stop before the batch
lifetime drains; synchronous and asynchronous cleanup failures remain observable; and independent
disconnect and drain failures are aggregated in owner order. The architecture gate was updated to
bind these lifecycle invariants to the extracted owner instead of the connector's former concrete
source layout. In-memory-outbox nested batch mechanics are internal and descriptively named, so no
legacy nested public runtime surface remains.

Four isolated counterchanges were compiled and executed. Moving activity capture before duplicate
admission made the duplicate-activity test fail; returning zero from `Count` made the exact batch
projection test fail; disabling `FromLast` restart made the deterministic timer test fail; and
accepting a null reference grouping key made the grouping-boundary test fail. An initial activity
counterchange survived because it changed which successfully admitted unique message owns the
delivery activity rather than allowing a duplicate to mutate state; that ambiguity was reviewed,
the intended latest-unique-admission behavior was retained, and the precise duplicate mutation was
then killed. Every mutation was restored manually. Final SHA-256 values are
`9a46e174c4a7bd38c2294b31e615fd82d829fc10a60a0fe579e96e186a032650` for `MessageBatch.cs`,
`27cfbf7b5eb3c489d9d2c66ef7fb4cd0ba96f997128bb1e105ae409bb9892777` for `BatchConsumer.cs`,
and `5bcd12bcc51dd798dddcc5e7909a7e8d35cac73bfcf57708e9a0d029463722ee` for
`GroupKeyProvider.cs`.

The restored final Batching selection passes 70/70 tests, the direct Abstractions selection passes
64/64, the direct `BatchOptions` selection passes 38/38, and the strengthened API architecture
test passes. Unique executable coverage across the Core and Abstractions batching owner is
757/772 lines (98.06%) and 239/266 branches (89.85%). `BatchOptions`, both grouping adapters,
`BatchEndpointExtensions`, `MessageBatch`, runtime settings, the consumer factory, configurator,
connect-handle primary logic, and the connector factory each have complete line coverage; the
public option and grouping contracts also have complete branch coverage. Remaining misses are
defensive activation, executor-race, and terminal cleanup paths and are not represented as
executed evidence.

The final Core host passes 2,903/2,903 tests and the complete architecture host passes 292/292,
both without failures or skips. The official serial Unit solution passes 5,653/5,653 tests across
22 hosts with no failures or skips. The Release Engineering solution builds with zero warnings and
errors. Both repository format gates pass; the Engineering gate inspected 5,733 files and changed
none. `git diff --check` passes, as do requirement projection, bidirectional async naming, source
layout, source-comment, and public-API gates through the complete architecture host.

Fresh-package verification covers 18 developer journeys, 31 packages, three isolated provider-
testing consumers, and all 30 runtime API assemblies. The packed public API contains 19,222 lines
with SHA-256 `05a971207d33b8476a17cf9217376969c9cccc37aa0e43449f5607ee36f7c615`.
Repository-wide source hygiene reports zero C# preprocessor directives and zero empty source
directories. The broad marker scan contains only real temporary-endpoint semantics, the real
state-machine schedule declaration placeholder, and `NotImplementedException` as a technical
failure classification; the architecture hygiene gates find no dummy or placeholder
implementation. Protected `review/` and `TestResults/` contents were neither changed nor staged.
The complete A+ source goal remains active for the next unreviewed owner.

## Iteration 87 outcome

Review the complete `ViciOne.ServiceBus.Courier` capability as one coherent owner: public routing-
slip contracts and builder APIs, activity execution and compensation, lifecycle-event routing,
request/response proxies, dependency-injection registration, middleware, serialization boundaries,
and transport dispatch. Preserve every routing-slip feature while making the public model immutable,
the received wire model validated, cancellation and disposal observable, configuration deterministic,
and every runtime boundary explicit. Retain `src/ViciOne.ServiceBus.Courier` as a sibling project of
the Core assembly; within it, align every type, namespace, filename, folder, and manually reviewed
comment with its final responsibility.

## Iteration 87 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-COURIER-CONTRACTS` | itinerary, logs, failures, variables, subscriptions, and lifecycle events | contract and serialization tests | immutable collection surface, exact values, transport round trip, malformed input rejection, and caller-mutation isolation |
| `REQ-VSB-COURIER-BUILDER` | activities, variables, subscriptions, source itinerary, and snapshots | builder tests | every overload, null/empty/flag boundaries, atomic updates, injected clock, repeatable builds, and detached state |
| `REQ-VSB-COURIER-EXECUTION` | empty, active, completed, revised, terminated, faulted, and compensated routing slips | executor and lifecycle tests | exact destination, event sequence, state evolution, cancellation, failure propagation, and no duplicate terminal event |
| `REQ-VSB-COURIER-EVENTS` | topology publication, explicit subscriptions, supplemental delivery, activity filters, and selected contents | event publisher tests | every event/contents flag, ordering, custom envelope, invalid boundary, cancellation, and isolated message state |
| `REQ-VSB-COURIER-ACTIVITIES` | constructor, delegate, and dependency-injection factories | factory and middleware tests | exact instance/context, null return, sync/async disposal, observer sequence, fault, cancellation, and continuation behavior |
| `REQ-VSB-COURIER-REGISTRATION` | typed/runtime/scanned registration and endpoint ownership | registration and host tests | full signature matching, namespace filters, definitions, companion endpoints, duplicate registration, and invalid types |
| `REQ-VSB-COURIER-REQUESTS` | request metadata, completion response, declared fault, standard fault, and retry | request tests | exact metadata, injected time, missing-state diagnostics, retry count/delay, cancellation, and null-response rejection |
| `REQ-VSB-COURIER-ACCESSORS` | typed arguments, data, and variables across all lifecycle contexts | accessor tests | reference/value types, defaults, precedence, null context, invalid key, missing dictionaries, and conversion failures |
| `REQ-VSB-COURIER-OBSERVABILITY` | tracing, metrics, probe, consumed/faulted notification | host and observability tests | exact tags, duration source, observer order, owned cancellation classification, and probe shape |
| `REQ-VSB-SOURCE-NAVIGATION` | all 135 Courier production files | architecture tests and manual ledger | project boundary retained; one primary type per file where practical; exact type/namespace/folder ownership |
| `REQ-VSB-SOURCE-COMMENTS` | every comment in the complete Courier owner | manual review plus hygiene gates | current code and behavior only; no history, filler, workaround, or generated prose |

## Iteration 87 mutation obligations

- Make a routing-slip or lifecycle-event collection mutable, retain a caller-owned collection, or
  skip received-state validation: immutability, mutation-isolation, and malformed-wire tests must
  fail.
- Change activity/variable precedence, remove a selected lifecycle-event payload, or publish when a
  non-supplemental subscription owns delivery: exact contract assertions must fail.
- Route execution or compensation to the wrong endpoint, skip a state transition, or emit a terminal
  event twice: lifecycle ordering and count assertions must fail.
- Drop cancellation, replace the injected clock with wall time, hide an activity/disposal failure,
  or continue the pipeline after a failed boundary: propagation and zero-side-effect assertions must
  fail.
- Accept a mismatched runtime activity/definition type, lose a companion endpoint, or scan outside
  the requested namespace: registration-matrix assertions must fail.
- Remove any typed event accessor's context/key validation or change variable-over-argument
  precedence: complete accessor boundary tests must fail.
- Retain a stale construction-history comment or a type/file/namespace mismatch: the manual source
  ledger and architecture checks must reject the exact file.

## Iteration 87 completion

All 135 Courier production files and their 9,374 lines were read manually in full together with
the existing direct tests. No generator or scripted comment rewrite was used. Every comment was
reviewed while its implementation was understood and was retained or rewritten only when it
described current code or behavior. The assembly boundary is the primary source-layout rule:
`src/ViciOne.ServiceBus.Courier` remains an independent sibling of the Core project
`src/ViciOne.ServiceBus`, while provider integrations remain grouped by capability under
`Persistence`, `Scheduling`, and `Transports`. Within Courier, context types, advanced API types,
root-namespace types, filenames, and folders now express their actual ownership. There are no C#
files directly under the `src` root and no empty source directories.

Routing-slip and lifecycle-event contracts now expose read-only collections backed by detached
snapshots. Received routing-slip state is validated and isolated before execution. Typed revised-
event variable access is complete for reference and value types. Event publication preserves
subscription ownership and supplemental-delivery semantics without exposing mutable state.
Builder, host, request-proxy, and executor boundaries reject invalid values before side effects.
Request responses and routing-slip subscriptions retain their distinct endpoint addresses, and
executor timestamps use the injected `TimeProvider`.

Activity factories, scope providers, middleware, and dependency-injection registration now have
explicit lifecycle ownership. Scope cleanup attempts both ambient-context restoration and scope
disposal, keeps the original failure ordering, and aggregates independent failures instead of
leaking a scope. Registration validates runtime activity and definition compatibility and retains
companion endpoint ownership. The Core consume-scope provider no longer leaves an ambient context
behind when scope creation or cleanup fails. Futures consumes the same read-only Courier variable
contract without restoring a mutable compatibility surface.

Four isolated counterchanges were compiled and executed. Returning a caller-owned routing-slip
collection made the mutation-isolation test fail. Sending a subscription to the request-response
address made the two-address request-proxy test fail. Restoring ambient context before protected
scope disposal made the dual-failure cleanup test observe an undisposed scope. Replacing the
injected clock with `TimeProvider.System` made the deterministic executor timestamp test fail.
Every mutation was restored manually and byte-for-byte verification produced final SHA-256 values
`994189b32c22cdd03732c4adbf8c07e28219c08e0470ccc1f0fc231c145434db` for
`RoutingSlipMessageState.cs`, `09093dd430022a00e47721a885eb85b64d00b6d6b3cf149bd83ff6d9bddc683b`
for `RoutingSlipRequestProxy.cs`, `661ecc35d7448e2eadda8173ee7cbbf79df9a466521467934bb9523d8088c1eb`
for `ActivityScopeDisposal.cs`, and
`aaeabbb04356e39f3b18cf648ec1adb7e4ee792f1aedf23ca3b7db8da894bd8d` for
`RoutingSlipExecutor.cs`.

The final direct Courier selection passes 150/150 tests. Courier coverage increased from 69.02%
lines and 48.62% branches to 84.33% lines and 70.64% branches. These are measured values rather
than a claim of complete execution; unexecuted paths remain visible for later risk-directed work.
The final Core host passes 2,968/2,968 tests and the complete architecture host passes 292/292,
both without failures or skips. The official serial Unit solution passes 5,718/5,718 tests across
22 hosts with no failures or skips. The final Release Engineering solution builds with zero
warnings and errors. Both repository format gates pass without changes, and requirement
projection plus the complete bidirectional async naming gate pass on the final names and manifest.

Fresh-package verification passes 18 developer journeys using 31 freshly packed ViciOne packages,
three isolated provider-testing consumers, and all 30 runtime API assemblies. The intentional
Greenfield API changes were reviewed before updating the contract: Courier collection contracts
are read-only, revised-event typed accessors are complete, and the corresponding Futures helpers
accept read-only variables. The packed public API contains 19,224 lines with SHA-256
`47b29f2bdc942446f27c7f5e9597e062d6add4ad16d5af941273047e65b5343c`.

`git diff --check`, JSON validation, both format gates, source-layout checks, comment hygiene, and
the complete architecture suite pass. Repository-wide product source contains zero C#
preprocessor directives. The broad marker scan contains only the real state-machine schedule
declaration placeholder semantics and `NotImplementedException` as a retry failure
classification; neither is a dummy implementation. Protected `review/` and `TestResults/`
contents were neither changed nor staged. The complete A+ source goal remains active for the next
unreviewed owner.

## Iteration 88 outcome

Review the complete `ViciOne.ServiceBus.Futures` capability as one coherent owner: durable state
and stored messages, command correlation, pending request and routing-slip execution, terminal
result and fault publication, subscriber replay, definitions, discovery, registration, and
persistence-facing contracts. Preserve every future feature while removing dispatch/tracking race
windows, configuration-order dependence, mutable stored-message aliases, incomplete cancellation,
and delayed-fault state inconsistencies. Retain Futures as an independent feature assembly beneath
`src`; align every type, filename, namespace, folder, and manually reviewed comment with its final
responsibility.

## Iteration 88 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-FUTURE-STATE-PERSISTENCE` | command, pending identifiers, subscriptions, variables, results, faults, and concurrency metadata | state and message contract tests plus provider integration tests | non-null rehydration, required values, comparer semantics, detached inputs, exact serialization, and provider round trip |
| `REQ-VSB-FUTURE-REQUEST-DISPATCH` | publish, fixed send, context-selected send, one request, and request ranges | request contract and integration tests | exact destination, pending-before-dispatch, duplicate rejection, rollback on failed dispatch, deterministic range ordering, cancellation, and null boundaries |
| `REQ-VSB-FUTURE-ROUTING-SLIP` | callback-built and container-planned itineraries | routing-slip contract and persistence integration tests | FutureId, subscription, tracking-before-dispatch, failed-dispatch rollback, callback cancellation, terminal result, terminal fault, and configuration-order independence |
| `REQ-VSB-FUTURE-TERMINATION` | immediate fault, deferred fault, all-completed result, and durable replay | state-machine and batch integration tests | exact transition timing, no premature terminal state, late subscriber acceptance, one terminal outcome, and no repeated child work |
| `REQ-VSB-FUTURE-SUBSCRIPTIONS` | response addresses with and without request identifiers | subscription contract tests | value equality, duplicate suppression, detached enumeration, request-id propagation, fan-out, cancellation, and send failure |
| `REQ-VSB-FUTURE-VARIABLES` | synchronous and asynchronous values from typed events and state | variable extension tests | all overloads, case-insensitive lookup, null/empty keys, null factories/results, cancellation, replacement, and conversion failure |
| `REQ-VSB-FUTURE-CONFIGURATION` | request, response, result, fault, routing-slip, and definition configuration | configuration contract tests | every overload, invalid callbacks, mutually required choices, repeated calls, endpoint settings, and exact validation diagnostics |
| `REQ-VSB-FUTURE-REGISTRATION` | typed, runtime, assembly, explicit-type, namespace, and request-consumer registration | registration boundary and host tests | exact definition association, filter scope, invalid/abstract/open types, null elements, duplicate registration, endpoint ownership, and repository requirement |
| `REQ-VSB-FUTURE-API` | complete public Futures surface | architecture and reflection contract tests | Greenfield names, cancellation placement, read-only stored-message exposure, no legacy aliases, and intentional package boundary |
| `REQ-VSB-SOURCE-NAVIGATION` | all 55 Futures production files | architecture tests and manual ledger | independent project retained; one primary type per file where practical; exact type/namespace/folder ownership |
| `REQ-VSB-SOURCE-COMMENTS` | every comment in all 4,430 Futures source lines | manual review plus hygiene gates | current code and behavior only; no history, filler, workaround, or generated prose |

## Iteration 88 mutation obligations

- Move pending registration after request or routing-slip dispatch, retain a failed-dispatch
  identifier, or allow duplicate pending identifiers: exact observation and rollback assertions
  must fail.
- Replace sequential range state mutation with unsynchronized concurrent mutation, change input
  order, or drop cancellation: deterministic dispatch assertions must fail.
- Replace callback cancellation with a default token or make routing-slip tracking depend on
  configuration order: callback and state assertions must fail.
- Transition a deferred-fault future before all tracked work terminates, accept a duplicate command
  as a terminal replay too early, or publish more than one terminal message: lifecycle integration
  assertions must fail.
- Retain caller-owned stored-message or persisted collection instances, accept invalid contract
  names, or lose case-insensitive durable variables: snapshot and rehydration assertions must fail.
- Accept an invalid runtime future/definition type or scan beyond the requested namespace: complete
  registration-matrix assertions must fail.
- Retain a stale construction-history comment or a type/file/namespace mismatch: the manual source
  ledger and architecture checks must reject the exact file.

## Iteration 88 completion

All 55 Futures production files and their original 4,430 lines were read manually in full together
with the directly affected tests. No generator or scripted comment rewrite was used. Every comment
was checked against the implementation while that file was understood, and comments were retained
or corrected only when they described current code or behavior. Futures remains an independent
feature assembly at `src/ViciOne.ServiceBus.Futures`; `src` is the project/assembly list, while
`src/ViciOne.ServiceBus` is the Core project. Persistence, Scheduling, and Transports continue to
group provider assemblies by capability. Within Futures, all primary types, namespaces, filenames,
and folders match their final ownership, and no additional visual wrapper directory is warranted.

Stored future messages and persisted collection inputs now use detached, read-only snapshots.
Message URNs, future locations, subscriptions, request identifiers, runtime registration types, and
assembly/type scans reject malformed or ambiguous inputs before side effects. The registration API
now distinguishes explicit types (`AddFutures`), explicit assemblies (`AddFuturesFromAssemblies`),
and the loaded-assembly convenience (`AddFuturesFromLoadedAssemblies`) without overload ambiguity.
The unused internal runtime `RegisterFuture` path was removed after static reference inspection
confirmed that no product or test caller existed; no behavior or public API was removed.

Pending request and routing-slip identifiers are now registered before transport dispatch and are
rolled back when dispatch fails. Duplicate and empty identifiers are rejected deterministically,
request ranges preserve input order without unsynchronized state mutation, and routing-slip
tracking is independent of configuration order. Callback, dispatch, terminal publication, and
pending-completion paths forward their caller cancellation tokens. Result/fault factories finish
successfully before terminal state mutation, terminal publication failure rolls state back, and a
deferred fault accepts further subscribers until every tracked operation has terminated. Variable,
result, fault, and routing-slip binder names now satisfy the complete bidirectional async contract.

The new direct contract matrix covers stored-state isolation, message URNs and round trips,
locations and subscriptions, request dispatch and rollback, routing-slip configuration and
tracking, all terminal configurator shapes, result/fault lifecycle behavior, variable conversion,
consumer kinds, registration boundaries, and the default routing-slip fault mapping. Four shallow
legacy registration cases and their three requirement projections were deleted only after the new
matrix fully superseded them. The final Futures selection passes 106/106 tests. Futures coverage
increased from 58.86% lines and 49.77% branches to 90.62% lines and 85.48% branches. The final
method-level risk calculation covers 376 methods and reports zero CRAP scores above 30; the former
unexecuted default routing-slip fault mapper now has 100% line coverage.

Seven isolated counterchanges were compiled and executed. Moving pending registration after
dispatch, dropping routing-slip tracking propagation, making deferred fault transition
unconditional, returning a caller-owned stored-message dictionary, removing terminal-result
rollback, mutating state before invoking a result factory, and dropping consume cancellation from
pending completion each made its precise regression test fail. Every mutation was restored
manually. Byte-for-byte verification produced final SHA-256 values
`cbc23f9914735fc7d83c6a331409a8304ac57801baca6bfebc51f807d7d2d27e` for `FutureRequest.cs`,
`8350e06e5d5c0acd416dcaab09701062bf4205bae6244b81d66b577067eade99` for
`FutureRoutingSlipConfigurator.cs`, `f8c5ac8a2861cabd95a3cb4a5ee88e3bcc44bd3dd240ffb430a9af3a2dc511d5`
for `FutureFault.cs`, `1e0e8387c67fe60acbe41dfff7df6d45f20119698dd4685fc9e880329401a6fd`
for `FutureMessage.cs`, `3d7d77dfd6a2ce95748cbae5b078977f5aee9692a80d9fee42a6b0b58ff99a4a`
for `FutureResult.cs`, and `bf616c5391d5221b0ce076f37e0fc4cca8cfcead5305e97a19145f1d0e32140e`
for `FutureStateExtensions.cs`.

The final Unit and Engineering solutions build in Release with zero warnings and errors. The
official serial Unit solution passes 5,790/5,790 tests across 22 hosts with zero failures and zero
skips; this includes the complete architecture, source-layout, comment-hygiene, requirement-
projection, and bidirectional async gates. Both repository format gates pass without changes.
Azure Table and Entity Framework Core local-integration projects compile against the changed
Futures callback contract; real Azure Table cloud acceptance was already established in iteration
49 and is not falsely represented as a new cloud run in this iteration.

Fresh-package verification passes 18 developer journeys using 31 freshly packed ViciOne packages,
three isolated provider-testing consumers, and all 30 runtime API assemblies. The intentional
Greenfield Futures API changes were reviewed before updating the contract. The packed public API
contains 19,225 lines with SHA-256
`74bc8ccbe3fd160506783a365a85ba9ebf13f5657482edacaf75d54167cbf441`.
`git diff --check` and CoreRequirements JSON validation pass. Repository-wide product source has
zero C# preprocessor directives and zero empty source directories. The broad marker scan contains
only explicit unsupported-operation contracts, real state-machine schedule-declaration placeholder
semantics, and `NotImplementedException` as a retry classification; none is a dummy implementation.
Protected `review/` and `TestResults/` contents were neither changed nor staged. The complete A+
source goal remains active for the next unreviewed owner.

## Iteration 89 outcome

Review the complete `ViciOne.ServiceBus.Mediator` capability as one coherent owner: direct and
container construction, send and publish dispatch, request/response routing, observer ownership,
message-body materialization, dependency-injection scope preservation, request-handler adapters,
and the public test harness. Preserve all mediator behavior while removing inherited factory and
host-builder compatibility entry points, closing observer and resource-lifetime defects, enforcing
configuration and address boundaries before side effects, and making every API shape directly
testable. Retain Mediator as an independent feature assembly directly beneath `src`; do not nest
it inside the Core project merely for visual uniformity.

## Iteration 89 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-MEDIATOR-FACTORY` | default/custom address, clock, limits, and construction boundaries | factory contract tests | exact endpoint addresses, pre-callback validation, required limits, and one explicit factory entry point |
| `REQ-VSB-MEDIATOR-CONFIGURATION` | limits and materialization callback ownership | factory and dispatch tests | single declaration, deterministic diagnostics, one callback invocation, and no partial service registration |
| `REQ-VSB-MEDIATOR-OBSERVERS` | configuration-time and runtime consume/send/publish observers | observer contract tests | primary/response consumption, strict send/publish isolation, disconnect, exact fault, and observer-fault containment |
| `REQ-VSB-MEDIATOR-ADVANCED-SEND` | typed, runtime, initializer, and pipe send forms | advanced API tests | every overload, exact contract, exact pipe invocation, null boundaries, cancellation, and endpoint resolution |
| `REQ-VSB-MEDIATOR-ADVANCED-PUBLISH` | typed, runtime, initializer, and pipe publish forms | advanced API tests | every overload, publication semantics, exact pipe invocation, null boundaries, and observer isolation |
| `REQ-VSB-MEDIATOR-REQUEST-API` | direct and consume-context request handles and clients | request API tests | message/initializer/address/client matrix, correlation metadata, scope propagation, null boundaries, and cancellation |
| `REQ-VSB-SCOPED-MEDIATOR` | scoped publish, request, client factory, and connectors | scoped mediator tests | all public forms execute inside one DI scope, thread-safe lazy context, exact routes, and connector stability |
| `REQ-VSB-MEDIATOR-BODY` | canonical bounded JSON materialization | body serializer, receive-context, and dispatch tests | exact bytes, owned buffer, exact limit, overflow, unsupported stream operations, cancellation, and logical diagnostic address |
| `REQ-VSB-MEDIATOR-SERIALIZATION-CONTEXT` | typed/runtime projection and unsupported transport serialization | serialization-context tests | identity-preserving projections, required types, dictionary conversion, and consistent unsupported-operation failures |
| `REQ-VSB-MEDIATOR-RECEIVE-CONTEXT` | metadata, attached work, delivery/fault state, and notifications | receive-context tests | exact body/address/providers, dispatch waits for attached work, required arguments, cancellation-before-state, and terminal flags |
| `REQ-VSB-MEDIATOR-REQUEST-HANDLER` | one-way and request/response handler adapters | request-handler tests | exact message, cancellation, typed response, null context, and explicit null-response failure |
| `REQ-VSB-TEST-HARNESS-MEDIATOR` | observation and owned asynchronous lifetime | harness behavior tests | request/response evidence, exact failure, observer cleanup, mediator disposal, base cleanup, and idempotence |
| `REQ-VSB-SOURCE-NAVIGATION` | all 29 original Mediator source files | architecture tests and manual ledger | independent project boundary, final 28-file owner, matching type/namespace/file/folder responsibility, and no empty directory |
| `REQ-VSB-SOURCE-COMMENTS` | every comment in all 3,082 original Mediator source lines | manual review plus documentation gates | current code and behavior only; no history, filler, generic placeholder text, or generated rewrite |

## Iteration 89 mutation obligations

- Disconnect configuration-time consume observers from the materialized runtime: the exact
  configuration-observer test must fail with no recorded consume stages.
- Route publications through send observers: the strict observer-isolation test must fail by
  recording the publication as a send.
- Report the physical mediator endpoint instead of an addressed request's logical destination on
  body admission failure: the logical-address regression test must fail on the exact URI.
- Allow a request-response handler to return `null`: the handler contract must fail because the
  explicit handler diagnostic was replaced by a downstream argument failure.
- Skip asynchronous mediator disposal in the test harness: the lifetime test must fail because a
  request client can still be created through the leaked mediator.

## Iteration 89 completion

All 29 original Mediator production files and their 3,082 lines were read manually in full together
with every direct test and the affected test-harness implementation. No generator or scripted
comment rewrite was used. Every comment was checked against the implementation while the file was
understood. Two generic return descriptions found by the repository documentation gate were then
rewritten to state the exact one-way handler task contract. The final Mediator owner contains 28
C# files and 2,730 lines because two compatibility adapter files were removed and one explicit
factory file was added.

The physical layout follows assembly ownership rather than visual nesting. `src` is the list of
independent projects; `src/ViciOne.ServiceBus` is specifically the Core project and is not a wrapper
for the other assemblies. Mediator therefore remains at `src/ViciOne.ServiceBus.Mediator`.
Persistence, Scheduling, and Transports remain family folders for interchangeable integration
projects. Nesting Mediator beneath the Core project would misstate ownership and expose it to SDK
default compile globs. Every remaining Mediator filename, namespace, type, and folder matches its
responsibility, and no empty directory remains.

`MediatorFactory.Create` is now the single direct construction entry point. The inherited
`Bus.Factory.CreateMediator` adapter and host-builder `UseMediator` compatibility surface were
removed; dependency-injection construction remains the conventional `AddMediator` entry point.
Base addresses are validated as absolute loopback addresses before callbacks or service-collection
mutation. Direct and container configuration reject duplicate limits and duplicate materialization
callbacks. The advanced send and publish overload-hiding relationship is owned by the shared
Abstractions contracts and remains explicitly recorded for that owner rather than being hidden by
a Mediator-only compatibility layer.

Configuration-time observers now participate in the materialized runtime. Publish observers are
strictly isolated from send observers, receive observers cover both primary and response
dispatchers, and observer cleanup is owned by the mediator. Addressed endpoints no longer create an
unbounded URI cache, admission failures retain the logical destination, and the bounded body stream
rejects invalid ranges, repeated completion, cancellation, and post-completion writes. Receive and
serialization contexts validate every required runtime boundary and expose canonical owned JSON.

Scoped mediator client-factory creation is lazy and thread-safe. All contextual and non-contextual
request handle/client forms and every scoped publish form preserve the calling dependency-injection
scope and exact route. Request-response handlers reject a `null` response with a handler-specific
diagnostic. `MediatorTestHarness` now owns observer handles and the mediator's asynchronous lifetime,
attempts every cleanup path, aggregates independent failures, and is idempotent.

The direct Mediator selection passes 86/86 tests. Package coverage increased from 59.90% lines and
46.97% branches to 88.46% lines and 75.48% branches. The method-level calculation covers 267
methods and reports zero CRAP scores above 10. Remaining unexecuted lines are visible defensive
registration delegation, rare concurrent cleanup, and compiler-generated state-machine paths and
are not represented as executed evidence.

Five isolated counterchanges were compiled and executed. Losing configured consume observers,
mixing publish dispatch into send observers, using the physical endpoint for addressed admission
failure, permitting a null handler response, and leaking the harness-owned mediator each made its
precise regression test fail. Every mutation was restored manually. Final SHA-256 values are
`b2b00e6339be67ae86e1f435f030d15183b3e6954cb50071ff38f365e6690d76` for
`MediatorFactory.cs`, `022f62c0fa0990d68f0b7499e6b715c812ef2fa3c8a3f98952d3bfb2bcb0354e`
for `MediatorSendEndpoint.cs`, `e5ca5266da8197d43fc1f0fc011d3afae8851fa4aa9d3e15d4329c15a87a54cb`
for `MediatorRequestHandler.cs`, and
`5363b3f2430dcf1473984b598b25f70c116556dc84a469c73cdcae3ec8c35ad9` for
`MediatorTestHarness.cs`.

The final Release Engineering solution builds with zero warnings and errors. Both repository
format gates pass without changes. The official serial Unit solution passes 5,840/5,840 tests
across 22 hosts with zero failures and zero skips; this includes all 292 architecture tests, the
complete bidirectional async naming gate, source layout, comment hygiene, public documentation, and
the 42 new requirement projections.

Fresh-package verification passes 18 developer journeys using 31 freshly packed ViciOne packages,
three isolated provider-testing consumers, and all 30 runtime API assemblies. The intentional API
diff contains only the single direct factory, removal of the two compatibility adapter types and
their four methods, and the harness's truthful `IAsyncDisposable` contract. The packed public API
contains 19,222 lines with SHA-256
`92338a749f24cbd843a1bb74359acd423948970efff88ab7e727e9317a2a3103`.
`git diff --check` and CoreRequirements JSON validation pass. Repository-wide product source has
zero C# preprocessor directives and zero empty source directories. The protected `review/` and
`TestResults/` trees were neither changed nor staged. The complete A+ source goal remains active
for the next unreviewed owner.

## Iteration 90 outcome

Review the complete `ViciOne.ServiceBus.Quartz` capability as one coherent scheduling-adapter
owner: direct and dependency-injection composition, scheduler identity and lifecycle, one-time and
recurring commands, persisted trigger data, message reconstruction, delivery, cancellation,
pausing, resuming, retry classification, and the durable Quartz acceptance profile. Preserve all
scheduling features while rejecting malformed commands before scheduler access, making persisted
data unambiguous, and closing every bus-owned resource lifetime.

Keep the project at `src/Scheduling/ViciOne.ServiceBus.Quartz`. The `Scheduling` directory is the
family boundary for interchangeable scheduling integrations, whereas `src/ViciOne.ServiceBus` is
the Core project itself and is not a container for other assemblies. This physical layout therefore
matches assembly ownership, dependency direction, namespace responsibility, and the corresponding
test location.

## Iteration 90 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-QUARTZ-PUBLIC-API` | greenfield scheduling composition and lease surface | configuration contract tests | exact four-type public API, seven composition methods, null boundaries, and no compatibility facade |
| `REQ-VSB-QUARTZ-ONE-TIME` / `REQ-VSB-QUARTZ-RECURRING-CONTROL` | one-time, recurring, cancel, pause, and resume inputs | command validation tests | every invalid member fails with its exact category before scheduler-factory access |
| `REQ-VSB-QUARTZ-JOB-DATA` | content type, body, message types, metadata, and transport properties | serialization, codec, context, and job tests | serializer-owned content type, lossless JSON types, fail-closed decoding, and immutable reconstruction snapshot |
| `REQ-VSB-QUARTZ-TRIGGER-KEY` | one-time and recurring Quartz identities | trigger-key tests | non-empty token, identifiers, groups, namespaces, stable escaping, and collision resistance |
| `REQ-VSB-QUARTZ-SCHEDULER-OWNERSHIP` | direct observer, partitioner, scheduler, and factory lifetime | lease, configuration, and lifecycle tests | complete idempotent cleanup, caller ownership, exact single failure, and aggregate independent failures |
| `REQ-VSB-QUARTZ-MULTIBUS` | bus and scheduler namespace isolation | configuration and lifecycle tests | assembly-stable bus identity, same-FQN distinction, unique claims, and typed registration |
| `REQ-VSB-QUARTZ-DELIVERY` | scheduled send reconstruction and terminal invalid data | job and send-pipe tests | required absolute address and type list, exact body/content type, cancellation, retry, and unscheduling |
| `REQ-VSB-SOURCE-NAVIGATION` | all 29 original Quartz production files | architecture tests and manual ledger | final 30-file owner, matching type/namespace/file/folder responsibility, and no empty directory |
| `REQ-VSB-SOURCE-COMMENTS` | every comment in all 2,654 original Quartz source lines | manual review plus documentation gates | current code and behavior only; exact completion and ownership semantics; no history or filler |

## Iteration 90 mutation obligations

- Persist the inbound receive content type instead of the selected serializer's content type: the
  serialization-boundary test must fail with both exact media types.
- Restore the inherited semicolon-delimited message-type representation: the scheduling-boundary
  test must fail when a valid identifier itself contains a semicolon.
- Resolve the scheduler before validating a one-time command: all seven invalid variants must fail
  because the forbidden scheduler access replaces the intended diagnostic.
- Omit disposal of the lifecycle observer from the direct lease: the lease ownership test must
  observe zero disconnections instead of exactly one.
- Reconstruct headers lazily from mutable Quartz job data: the complete metadata snapshot test must
  observe the post-construction mutation.
- Use only the bus type's full name as scheduler owner identity: the typed-registration test must
  reject the assembly-ambiguous key.
- Accept an empty one-time token when forming a trigger key: the empty-token contract test must fail.

## Iteration 90 completion

All 29 original Quartz production files and their 2,654 lines were read manually in full together
with the project file, all comments, the complete public surface, the owning tests, and the
requirements manifest. No generator or scripted comment rewrite was used. Each comment was checked
while its implementation was understood. The final project contains 30 C# files and 2,786 lines;
the added internal `QuartzMessageTypeList` owns the single persisted JSON representation. Every
remaining filename, namespace, type, and directory matches its responsibility, and no empty source
directory remains.

Scheduling commands now validate tokens, absolute destinations, payloads, message-type identifiers,
schedule identifiers, cron data, time zones, time ranges, and misfire policies before serialization
or scheduler access. Persisted content type comes from the serializer that produced the body.
Message-type identifiers are stored as JSON rather than an ambiguous delimiter string, so valid
identifiers containing semicolons or quotes round-trip exactly and malformed stored data is terminal.
Cancellation, pause, and resume likewise validate trigger identities before resolving a scheduler.

Scheduled-message contexts snapshot standard metadata, user headers, Quartz fire metadata, and
transport properties at construction. Destination and supported message types are revalidated at
the delivery boundary. Direct leases now own the bus lifecycle-observer handle as well as the
partitioner and any adapter-owned scheduler factory; cleanup attempts every resource, preserves one
failure, aggregates independent failures, and remains idempotent. Configuration failure releases
already-created resources through small dedicated helpers. Scheduler ownership uses an
assembly-stable bus identity, preventing equal fully qualified type names from different assemblies
from colliding.

The focused profile grew from 216 to 267 tests and passes 267/267 with zero failures and zero skips.
Fresh package coverage is 97.75% lines and 86.12% branches across 202 instrumented methods, with no
CRAP score above 30. Extracting direct-configuration cleanup reduced that method's complexity from
26 to 18 and its CRAP score from 34.67 to 18.01. Unexecuted lines remain defensive cleanup-failure
paths and are not reported as executed evidence.

Seven isolated counterchanges were compiled and executed. Wrong content-type ownership, delimiter
storage, premature scheduler access, observer leakage, lazy header reconstruction, assembly-
ambiguous bus identity, and an empty scheduling token each made its precise regression test fail.
Every counterchange was restored manually before the final build. Final SHA-256 values are
`788fd94a01b204b3ae86ee243c3b95c604b5e2d8a9be2c09636f3acd4c77232a` for
`ScheduleMessageConsumer.cs`, `0f32a6ca76cfca1a29c6be961337db7840839554a32dadb71dd8aed4e291f237`
for `QuartzSchedulerLease.cs`, `2172882d92f9980414ad432fe8ef7751f686b690e69f24bad17acd67a401a107`
for `QuartzSchedulingExtensions.cs`, `8ce1d85a9ed88963b87667967ef4a5d49bb688bb9215b5bc2b0385deea76b655`
for `QuartzMessageTypeList.cs`, `840ff29f081c6d581637b99a62dc52aa006d0276b445fad366e5f6248e61cef9`
for `QuartzScheduledMessageContext.cs`, `104afeeeeda54e52c7478ebc8c43085799a80ab27f1031a876e3e7b44e052a9c`
for `QuartzSchedulerBinding.cs`, and
`0407a5a570ba43ad8a5471c71165be924300dd9753d58e4f2d03d6c8542e80c6`
for `QuartzTriggerKey.cs`.

The final serial Release Unit/Architecture solution builds with zero warnings and errors and passes
5,891/5,891 tests across 22 hosts with zero failures and zero skips. This includes the complete
architecture, bidirectional async naming, source layout, comment hygiene, documentation, and
requirement-projection gates. Whitespace formatting, warning-level style, requirements JSON, Git
whitespace, C# preprocessor, and empty-directory checks pass.

Fresh-package verification passes 18 developer journeys using 31 freshly packed ViciOne packages,
three executed isolated provider-testing consumers, and all 30 runtime API assemblies. Quartz adds
only an internal codec, so the packed public API remains unchanged at 19,222 lines with SHA-256
`92338a749f24cbd843a1bb74359acd423948970efff88ab7e727e9317a2a3103`. The protected `review/` and
`TestResults/` trees were neither changed nor staged. The complete A+ source goal remains active for
the next unreviewed owner.

## Iteration 91 outcome

Review the complete `ViciOne.ServiceBus.SignalR` integration as one coherent transport-adjacent
backplane owner: public composition, endpoint identity, local connection and subscription state,
cross-node broadcast/connection/group/user delivery, acknowledged group membership, protocol
serialization, client invocation results and cancellation, dependency-injection scope ownership,
logging, and malformed-contract rejection. Preserve SignalR scale-out behavior while replacing the
legacy public implementation surface with one minimal Greenfield composition API and completing the
modern .NET SignalR lifetime-manager contract.

Move the independent project to `src/Transports/ViciOne.ServiceBus.SignalR` and its tests to the
matching `tests/Transports` family because the package adapts ViciOne.ServiceBus as a SignalR
backplane. `src/ViciOne.ServiceBus` remains the Core project itself, not a container for sibling
assemblies. Other independent Core, feature, and tooling packages remain direct children of `src`;
family folders are used only where several interchangeable providers or integrations share one
architectural axis. This avoids false ownership and the SDK's recursive default compile globs.

## Iteration 91 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-SIGNALR-PUBLIC-API` | composition and timeout configuration | configuration tests | exact two-type public API, fluent return, custom/default timeout, duplicate-hub rejection, null boundary, and no partial registration |
| `REQ-VSB-SIGNALR-LOCAL` | connection, group, user, broadcast, disconnect, and failure logging | local lifetime-manager tests | exact recipients, exclusions, ordinal identity, membership cleanup, cancellation, and partial-write diagnostics |
| `REQ-VSB-SIGNALR-SCALEOUT` | cross-node fanout and acknowledged group commands | scale-out tests | exact recipients, no origin echo, remote add/remove acknowledgement, unknown connection, timeout, cancellation, and invalid command rejection |
| `REQ-VSB-SIGNALR-FANOUT` | multi-connection/group/user selection | fanout tests | stable ordinal de-duplication across repeated identifiers and overlapping memberships |
| `REQ-VSB-SIGNALR-CLIENT-RESULTS` | typed client invocations, completion, error, cancellation, disconnect, and stale delivery | client-result tests | local and remote success/null/error/cancel, owning-connection enforcement, protocol/invocation identity, timeout, disconnect cleanup, and duplicate suppression |
| `REQ-VSB-SIGNALR-SERIALIZATION` | protocol fanout and stored frames | serializer and serialization tests | every registered protocol, exact owned payloads, empty/malformed input rejection, and single-completion enforcement |
| `REQ-VSB-SIGNALR-CONTRACTS` | bus-delivered internal command boundaries | contract-validation and boundary tests | every required string, payload collection, exclusion, action, connection, group, user, and invocation member rejected before effects |
| `REQ-VSB-SIGNALR-SCOPES` | per-operation DI scope ownership | configuration and client-result tests | asynchronous scope creation, async-only disposal after success and failure, and required scoped services |
| `REQ-VSB-SIGNALR-RUNTIME-STATE` | subscription and pending-invocation indexes | runtime-state and client-result tests | ordinal keys, idempotent removal, ownership, duplicate invocation rejection, and terminal removal semantics |
| `REQ-VSB-SOURCE-NAVIGATION` | all 32 original SignalR source files | architecture tests and manual ledger | one transport-family project, final 31-file owner, matching type/namespace/file/folder responsibility, and no empty directory |
| `REQ-VSB-SOURCE-COMMENTS` | every comment in all 1,368 original source lines and all replacement code | manual review plus documentation gates | current code and behavior only; exact ownership and concurrency semantics; no history, filler, generated rewrite, or stale names |

## Iteration 91 mutation obligations

- Remove identifier de-duplication from connection, group, and user fanout: each of the three exact
  fanout tests must fail by observing duplicate delivery.
- Change group identity from ordinal to case-insensitive comparison: the group case-sensitivity test
  must fail because two distinct SignalR groups collapse into one.
- Remove owning-connection validation from pending client invocations: the wrong-connection result
  test must fail because a different connection can complete the invocation.
- Omit asynchronous disposal of an operation scope: the DI lifetime test must fail because its
  async-only scoped resource is not disposed.

## Iteration 91 completion

All 32 original SignalR production files and their 1,368 lines were read manually in full together
with the complete replacement implementation, every comment, every direct test, the project file,
the requirements manifest, and the packed public surface. No generator or scripted comment rewrite
was used. Every comment was checked while its implementation was understood. The final owner has 31
C# files and 1,840 lines. Multi-type files were split so each independently meaningful runtime type
has a matching filename. The project and tests now occupy the matching `Transports` family, the old
paths and three empty move-remnant directories are absent, and all current solution, capability,
validation, and repository-graph paths agree.

The public package now exposes exactly `SignalRBackplaneOptions` and
`SignalRBackplaneExtensions.AddSignalRBackplane<THub>`. Registration is fluent, rejects missing or
duplicate composition before partial mutation, and validates the acknowledged remote-group timeout
with the repository's actionable feature/bus/problem/fix diagnostic shape. All consumer definitions,
wire contracts, runtime indexes, serializers, scopes, and lifetime-manager implementation details
are internal.

The lifetime manager now implements modern typed SignalR client results across nodes, including null
results, remote errors and cancellation, caller cancellation, timeouts, disconnect cleanup, stale or
duplicate delivery, and protocol/invocation identity validation. Connection, group, and user
identifiers use ordinal semantics. Multi-target fanout removes repeated identifiers and overlapping
members before delivery. Group commands are acknowledged by the owning node, endpoint identifiers
are deterministic and bounded, request handles are disposed, and bus operation scopes are created
and disposed asynchronously. Operational failures use the injected typed logger rather than ambient
context.

The focused profile grows from 41 to 96 tests and passes 96/96. Fresh focused coverage is 99.5585%
lines and 91.7910% branches across 110 instrumented methods, with no CRAP score above 30. The highest
risk method is `ConnectionConsumer.DeliverAsync` at CRAP 18.0069 with 97.22% line and 83.33% branch
coverage. Remaining formal branch gaps are compiler-generated asynchronous paths, simple consumer
constructors, and defensive binder/delegation branches; they are not represented as executed
evidence.

Four isolated counterchanges were compiled and executed. Removing fanout de-duplication made all
three targeted fanout regressions fail; case-insensitive group identity, missing pending-invocation
ownership, and leaked async scopes each made their precise regression test fail. Every counterchange
was restored manually. Final SHA-256 values are
`d2ae3366c1fa906cdd1cc6978c037c0da3e49ad5b5485fde3c7585178276b625` for
`ServiceBusHubLifetimeManager.cs`,
`6ad37586732aa02c35f044dcf3c7e51bee7c2c81057bb29feb1379650e8cfa85` for
`PendingClientInvocationTracker.cs`,
`618a42ea5f8fd6b86ead8cb2f26f17d2b7571eb2a8deeb38171cb2048513ceec` for
`ConnectionSubscriptionIndex.cs`,
`55ee9e13f63b6092956668f470a274543692dcec02e196101fb9a58748cf7a43` for
`DependencyInjectionBackplaneScopeProvider.cs`, and
`e794618f8ddef6f1d45628e7ecfccca1c99c47935221eaf074825768c8906cce` for
`HubMessageSerializer.cs`.

The final serial Release Unit solution passes 5,951/5,951 tests with zero failures and zero skips;
the complete Architecture project separately passes 292/292. These include the
repository-wide bidirectional async-name/implementation gate, source navigation, comments,
configuration diagnostics, requirement projections, and public documentation. The focused and
Architecture builds finish with zero warnings and errors. Both repository format gates pass with
zero changes across 5,778 Engineering and 5,356 Unit files. Requirements and metadata JSON,
`git diff --check`, repository-wide C# preprocessor, and empty-source-directory checks pass.

Fresh-package verification passes twice: 18 developer journeys using 31 freshly packed ViciOne
packages, three executed isolated provider-testing consumers, and all 30 runtime API assemblies.
The manually reviewed SignalR API diff removes 118 inherited implementation-surface lines and adds
only the two intended Greenfield types. The packed public API contains 19,104 lines with SHA-256
`34c7a90ef04451531e03134e0891e752a410996742627d4648941427f04aee27`.
Protected `review/` and `TestResults/` contents were neither changed nor staged. The complete A+
source goal remains active for the next unreviewed owner.

## Iteration 92 outcome

Make the independent StateMachineVisualizer tooling package own its small Graphviz DOT and Mermaid
serialization boundary directly, produce culture- and platform-independent documents, and encode
every allowed label without retaining a general-purpose graph dependency or changing the exact
two-type public API.

## Iteration 92 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-VISUALIZER-GRAPHVIZ` | canonical DOT, node shapes, all relationships, type labels, and total label escaping | Visualizer Graphviz and projection tests | exact whole documents, syntax-sensitive labels, C0 controls, composite nodes, inheritance, disconnected nodes, and empty graph |
| `REQ-VSB-VISUALIZER-MERMAID` | canonical flowchart, node shapes, all relationships, type labels, and total entity encoding | Visualizer Mermaid and projection tests | exact whole documents, syntax-sensitive labels, C0 controls, composite nodes, inheritance, disconnected nodes, and empty graph |
| `REQ-VSB-VISUALIZER-DETERMINISM` | generated documents use canonical LF and invariant numeric identifiers | both generator suites | exact normalized output and no carriage returns under repeatable and concurrent generation |
| `REQ-VSB-VISUALIZER-API` | exactly two sealed generators with synchronous `Generate()` | Visualizer API tests and packed API gate | constructor guard, package namespace, exact public shape, concurrency, and unchanged packed contract |
| `REQ-VSB-VISUALIZER-DEPENDENCIES` | the project uses only its two required ViciOne project references | Architecture capability test and locked restore | no direct or transitive QuikGraph dependency remains |
| `REQ-VSB-SOURCE-NAVIGATION` | every remaining source file owns its matching type and responsibility | architecture tests and manual ledger | independent project remains directly under `src`; obsolete factory file and empty folder are absent |
| `REQ-VSB-SOURCE-COMMENTS` | every source comment describes current code and behavior only | manual review plus documentation gates | no history, migration narrative, filler, generated comment, or stale dependency terminology |

## Iteration 92 mutation obligations

- Emit a C0 control character verbatim in Graphviz and Mermaid output: each exact control-label
  regression must fail.
- Remove a disconnected node from either serializer: the existing disconnected-node contract must
  fail.
- Render state inheritance as an ordinary edge: the state-machine input contract must fail in both
  formats.
- Stop unwrapping `Fault<T>` labels: both typed-event suites must fail.
- Restore either direct QuikGraph package reference: the exact architecture dependency test must
  fail.

## Iteration 92 baseline

The unchanged behavior suite passes 25/25 with zero failures and skips. Direct package
instrumentation is 100% line and 100% branch coverage. Assertion review finds no assertion-free,
trivial-only, tautological, or unawaited test: equality, string, collection, type, null, exception,
negative, structural, and concurrency/state observations all have behavior-relevant assertions.
The package public API is already appropriately synchronous because both operations are bounded
in-memory text transformations; an `Async` member would be misleading.

## Iteration 92 completion

All four original Visualizer source files and their 300 physical lines were read manually in full
together with every source comment, the project file, the complete owning test project, its
requirements, and the immutable Sagas graph contracts consumed by the renderer. The final four-file
owner has 399 physical lines. Every filename, type, namespace, visibility, responsibility, XML
comment, and implementation comment was reviewed again after remediation. No generator or scripted
source/comment rewrite was used.

The package retains exactly its two sealed public generators and the synchronous `Generate()`
contract. Both renderers now own small deterministic serializers over a shared reference-identity
projection. They preserve every state-machine relationship and the established stable source-node
edge order, include disconnected nodes, use invariant identifiers and canonical LF documents, and
make all syntax-sensitive, control, and malformed-surrogate label data visible without permitting
label text to alter the output grammar. The redundant QuikGraph projection and both QuikGraph
packages are gone from the project, central package management, current source/test/sample graphs,
and lock files. The independent tooling assembly remains a direct `src` project because nesting it
inside the Core project folder would misrepresent package ownership; provider assemblies remain
grouped under `Persistence`, `Scheduling`, and `Transports`.

The focused profile grows from 25 to 29 tests and passes 29/29 with no failures or skips. Fresh
instrumentation reports 100% line and 100% branch coverage with complexity 125 for
`ViciOne.ServiceBus.StateMachineVisualizer`. Static assertion review accounts for all 29 tests and
181 assertion call sites, with no assertion-free, tautological, trivial-only, unawaited, flaky,
time-dependent, randomized, skipped, or swallowed-exception test. The source-to-test pairing
heuristic finds the two public generators directly paired; the two internal helpers are proven
through manual call-chain review and complete package instrumentation.

Six isolated counterchanges were compiled and killed: raw Graphviz C0 output, raw Mermaid C0
output, an omitted disconnected Graphviz node, ordinary Graphviz state inheritance, ordinary
Mermaid state inheritance, and retained `Fault<T>` wrapper labels. The original dependency guard
also produced the expected 291/292 red architecture result while both QuikGraph references were
present. Every counterchange was restored manually; an attempted index substitution that left the
observable document unchanged was correctly classified as equivalent and excluded from mutation
evidence.

Final SHA-256 values are
`8a505bd8e1e156cfbd2594852beeccdbd82d2ecc89f73eec98b1e810bdeb52c5` for
`StateMachineGraphProjection.cs`,
`59e300721f7359673d91ba2a5e58083ab0b995c220f9b79b80e040ee226ff8ff` for
`StateMachineNodeLabelFormatter.cs`,
`7638cda417f764ba2ae86b60d0fe5726c081b17b44187b23526296e8e4dc5e65` for
`StateMachineGraphvizGenerator.cs`, and
`e50637f53141a3988d87cfc00f4422d37dfdd61925f3526d79d27a48f267f2fd` for
`StateMachineMermaidGenerator.cs`.

The dependency inventory also advanced every available direct stable package version, including an
atomic Microsoft 10.0.12 family alignment and the independently versioned package consumers. A
fresh online recheck reports zero outdated direct packages, zero known vulnerable direct or
transitive packages, and zero deprecated direct or transitive packages. Product, Unit, and complete
Engineering locked restores pass.

The final serial Engineering Release build reports zero warnings and errors. The complete Unit and
Architecture solution passes 5,955/5,955 tests with no failures or skips; the Architecture owner
contains 292 passing tests. Both format/analyzer gates and `git diff --check` pass. All changed JSON
and lock files parse, and source scans find no C# preprocessor directives, QuikGraph references,
empty directories, or Visualizer dummy markers.

Fresh-package verification passes first while updating the five package-consumer locks and again
strictly against those tracked locks: 18 developer journeys, 31 freshly packed ViciOne packages,
three executed provider-testing consumers, and all 30 runtime API assemblies. The packed public API
remains exactly 19,104 lines with SHA-256
`34c7a90ef04451531e03134e0891e752a410996742627d4648941427f04aee27`.
Protected `review/` and `TestResults/` contents were neither changed nor staged. The repository-wide
source goal remains active for the next unreviewed owner.

## Iteration 93 outcome

Review the complete `ViciOne.ServiceBus.JobService` feature assembly as one coherent owner: public
registration and submission APIs, endpoint composition, local execution admission and shutdown,
heartbeat ownership, retry policy configuration, job metadata, consumer dispatch and supervision,
capacity allocation, the three coordinating state machines, recurring scheduling, and cron
parsing. Preserve every Job Service feature while removing public implementation details,
disconnected retry surface, mutable metadata aliases, and lifecycle races.

`src/ViciOne.ServiceBus.JobService` remains a direct child of `src` because it is an independent
first-party feature assembly, just as `ViciOne.ServiceBus` is the Core assembly rather than an
umbrella directory. Interchangeable external integrations remain grouped by architectural axis
under `Persistence`, `Scheduling`, and `Transports`; independent feature and tooling assemblies do
not acquire a false provider identity. Nesting sibling projects inside the Core project directory
would also expose them to the Core SDK project's recursive default compile globs.

## Iteration 93 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-JOB-REGISTRATION-API` | public composition, options, facade forwarding, saga state-machine registration, distribution strategy, and endpoint identity | public configuration API tests | complete successful registration, first-registration ownership, fluent returns, null boundaries, and no public configurator implementation |
| `REQ-VSB-JOB-DIRECT-CONFIGURATION` | explicit instance/options/context configuration | endpoint configuration tests | every option copied, all three endpoints configured, scopes and outbox attached once, addresses unavailable before composition, and every required input rejected |
| `REQ-VSB-JOB-SERVICE-LIFECYCLE` | start, restart, failed start, heartbeat generation, stop, admission drain, and recovery after heartbeat failure | lifecycle tests | exact publication order and count, one generation, no in-flight publication after stop, restart replacement, retry after publication fault, and causal cancellation |
| `REQ-VSB-JOB-SERVICE-ADMISSION` | concurrent identity reservation and pipeline handoff | lifecycle tests | active duplicate rejection, synchronous pipe-failure rollback, successful replacement, stopping rejection, and deterministic cleanup |
| `REQ-VSB-JOB-CANCELLATION` | immutable cancellation deadline and shutdown behavior | execution-context and state-machine tests | timeout snapshot before user code, external cancellation propagation, stopping fault, capacity release, and complete local drain |
| `REQ-VSB-JOB-METADATA` | case-insensitive last-write-wins immutable snapshots | serialization, execution-context, capacity, and state-machine tests | source isolation, replacement semantics, dictionary contract, no mutation capability, and preservation across every runtime/state boundary |
| `REQ-VSB-JOB-PROGRESS` | concurrent lazy progress buffering | execution-context tests | exactly one buffer, ordered publications, and safe concurrent first use |
| `REQ-VSB-JOB-CONSUMER-PIPELINE` | execution, cancellation, retry, terminal fault, and probe contract | consumer-filter tests | exact lifecycle message order, current retry attempt, next delay, terminal exception, required inputs, and probe identity |
| `REQ-VSB-JOB-START-CONSUMER` | command ownership and deserialization | start-consumer tests | matching type forwards all values, foreign type has no side effect, null payload fails before admission, and every required input is validated |
| `REQ-VSB-JOB-SUPERVISION` | local attempt status and cancellation | supervisor tests | every local task state maps to its exact wire status, stale/missing attempts remain unanswered, and required contexts are rejected |
| `REQ-VSB-JOB-CAPACITY` | per-type/global limits, allocation identity, reconciliation, and updates | capacity and type-state-machine tests | exact inclusive limits, idempotency, deterministic duplicate handling, expired/dead/orphan removal, and every update invariant |
| `REQ-VSB-JOB-ATTEMPT-SUPERVISION` | attempt start, liveness, escalation, status, fault, cancellation, and finalization | attempt-state-machine tests | every state transition, schedule identity, retry boundary, late acknowledgement, exception preservation, and terminal cleanup |
| `REQ-VSB-JOB-STATE-MACHINE` | immediate, one-time, recurring, retry, cancellation, and finalization flows | job-state-machine tests | complete state and message effects, capacity release, attempt draining, recurrence, late events, and finalization policy |
| `REQ-VSB-JOB-TYPE-STATE` | configuration, heartbeat, allocation, distribution, suspect removal, and stop | type-state-machine tests | exact persisted state, all strategy scopes, correct partition identities, global limit, and release behavior |
| `REQ-VSB-JOB-OPTIONS` | runtime options and effective retry policy | options tests | each invariant fails causally in isolation, null/invalid factories fail clearly, and configured policy is the policy executed |
| `REQ-VSB-JOB-SUBMISSION-API` | generated/explicit identity and job/value/property overloads | submission API tests | every overload forwards complete state and returns the accepted job identity |
| `REQ-VSB-RECURRING-JOB-API` / `REQ-VSB-SCHEDULED-JOB-API` | recurring and scheduled overload matrices | recurring API tests | every cron/configurator/property/value/identity form forwards exactly once with complete state |
| `REQ-VSB-CRON-*` | named values, ranges, increments, validation, matching, calendar boundaries, and daylight-saving transitions | cron parsing, calendar, scheduling, and DST tests | all twelve months, all weekdays, exact malformed-name diagnostics, named steps/ranges/last weekday, complete calendars, and both DST transitions |
| `REQ-VSB-SOURCE-NAVIGATION` | all 168 original JobService source files | architecture tests and manual ledger | final 170-file owner, matching type/file/folder responsibility, direct independent project placement, and no empty directory |
| `REQ-VSB-SOURCE-COMMENTS` | every comment in all 11,444 original source lines and all replacement code | manual review plus documentation gates | current code and behavior only; no history, migration narrative, filler, generator rewrite, or stale contract |

## Iteration 93 mutation obligations

- Expose `JobServiceConfigurator<T>` publicly and weaken the retry diagnostic: the public-contract
  and exact validation tests must fail.
- Change metadata snapshots from last-write-wins to first-write-wins or make progress-buffer lazy
  initialization non-thread-safe: the snapshot and concurrent-progress regressions must fail.
- Read the cancellation timeout after the user pipeline starts, admit work during shutdown, or stop
  without canceling the in-flight heartbeat generation: each precise lifecycle regression must fail
  or deterministically remain blocked until the bounded mutation host is terminated.
- Invert completed-job finalization, weaken the global capacity boundary, weaken the retry-attempt
  boundary, or ignore the current consumer retry attempt: the owning state-machine/filter test must
  fail.
- Partition attempts by job identity, invert the start-consumer type match, invert the direct
  `TimeProvider` invariant, or stop a heartbeat generation after one publication failure: each exact
  behavioral or causal-validation test must fail.
- Mis-map a named month or accept an unexpected suffix after a named month: the exhaustive month
  table or exact malformed-field theory must fail.

## Iteration 93 baseline

The first bounded review profile exposed four real failures: the concrete configurator leaked into
the public API, materialized runtime options could bypass validation, a retry diagnostic omitted its
closing generic delimiter, and shutdown could wait indefinitely for an in-flight heartbeat that it
did not own a cancellation path for. Coverage review then isolated two meaningful parser gaps:
`GetMonthNumber` had 50% line coverage and `StoreExpressionGeneralValue` had 77.27%. Those findings,
rather than aggregate percentage chasing, defined the final Cron test additions.

## Iteration 93 completion

All 168 original JobService C# files and all 11,444 physical lines were read manually in full along
with every comment, every direct test, the project file, the requirements manifest, and the packed
public API. No generator or scripted source/comment rewrite was used. The final assembly contains
170 C# files and 11,546 physical lines. Every filename, type, namespace, visibility, responsibility,
XML comment, and implementation comment was checked again after remediation.

The local runtime now owns one exact cancellable heartbeat generation, replaces and drains it on
restart, and leaves no publication in flight after stop. Admission is closed and drained before
shutdown, active job identities are reserved atomically, synchronous pipeline failures release the
reservation, and cancellation deadlines and registered options are immutable snapshots. Concurrent
progress initialization is thread-safe. Metadata has one case-insensitive, last-write-wins snapshot
implementation and read-only projection used consistently by execution, distribution, and state
coordination.

The retry API now exposes only the effective `IRetryPolicyConfigurator`; disconnected observer
storage and the public concrete configurator are gone. Null factories and null policy results fail
causally. Endpoint composition, registration, consumer-kind ownership, partition topology,
submission/recurring/scheduled overloads, event access, state response projection, start dispatch,
supervision, filter behavior, all three state machines, capacity reconciliation, and every new or
changed parameter have direct behavioral coverage in the requirement ledger.

Sixteen isolated counterchanges were compiled and executed. Public configurator leakage, malformed
retry diagnostics, first-write metadata, unsafe lazy initialization, late timeout reads, shutdown
admission, inverted finalization, weakened capacity/retry boundaries, ignored consumer retry state,
wrong attempt partitioning, inverted job-type matching, inverted clock validation, terminated
heartbeat recovery, a wrong February ordinal, and accepted `JANX` syntax all made their precise
regressions fail or made the bounded mutation host hang as predicted. Every counterchange was
restored manually. One outer cancellation catch-filter mutation was correctly classified as
equivalent because the owned heartbeat loop already normalizes cancellation, and the redundant
catch was removed instead of counted as evidence.

The final complete Core host coverage run passes 3,209/3,209 tests with no failures or skips.
`ViciOne.ServiceBus.JobService` records 95.4369% line and 89.0917% branch coverage. Both previously
risky Cron methods now have 100% line coverage; named-value parsing records 98.4848% branch
coverage, while the remaining switch-expression branch artifacts do not represent omitted month or
weekday values. The coverage artifact SHA-256 is
`5598866fd4fefca002fba6d50db26ad61436f582821b76ba4784395363dd188f`. The same Core host's
cross-package measurement is 74.1057% line and 67.0712% branch across 83,667 instrumented lines; it
is not presented as a falsely merged whole-repository percentage.

The final serial Engineering Release build completes with zero warnings and errors under CI
determinism and warnings-as-errors. The complete Unit/Architecture solution passes 6,074/6,074
tests across 23 hosts with zero failures and zero skips; the Architecture owner separately passes
292/292. Both repository format gates, requirement JSON, `git diff --check`, repository-wide C#
preprocessor scan, JobService dummy-marker scan, test skip/smell scan, bidirectional async naming,
source layout, comments, documentation, and empty-directory checks pass. A transient Debug failure
was traced to a stale pre-remediation dependency DLL created by an inappropriate
`--no-dependencies` test-only build; rebuilding the complete Debug dependency graph made the same
lifecycle class pass 14/14 and the subsequent coverage run pass 3,209/3,209. Coverage validation
must therefore refresh the full configuration-specific dependency graph whenever product source
changed.

Fresh-package verification passes with 18 developer journeys, 31 freshly packed ViciOne packages,
three executed isolated provider-testing consumers, and all 30 runtime package APIs matching the
committed baseline. The intended Greenfield API diff replaces the overly broad retry configurator
with `IRetryPolicyConfigurator` and removes the 21-line concrete configurator implementation
surface. The packed public API contains 19,083 lines with SHA-256
`5f3b41a65267feac9831887029dbb3bcdec8f4f1ce270df060dd139d7d4eb1da`.

Final SHA-256 values are
`d9add9edef630bedcc687be82d3e9ae3d3d99a9b24b369282b906985e40e2b93` for `JobService.cs`,
`f7889e988492bd4a6143e80a2c23d5051047593ca337eb4536ed29dea27e90b2` for
`ConsumeJobContext.cs`, `5e745e40d4c743d4583e8c4b39a1e77e69ac94273dadb383b60749cb4710875b`
for `JobPropertySnapshot.cs`,
`17df39d478bc6ee8554195ca3e55654716d95b0f0421aace12f8e69b6b6d67a9` for
`ReadOnlyJobPropertyCollection.cs`,
`02d2267572242387203864f524d949835b553c1245002a64943782eaacc3b299` for
`JobStateMachine.cs`, `4df4cdfd840ea49128403dc54d823f9df23bf560d3a8808ae1b456c162f116b2`
for `JobTypeStateMachine.cs`,
`0971695fb6f30b1c670c4a568996e0b12175f98c2268fdfdb70981a7e9ee8a81` for
`JobAttemptStateMachine.cs`, and
`59ddb8f6ec21908dc980f0cf7cc6f0625b185b484a4153d5aff7ae8e12797dca` for
`JobConsumerMessageFilter.cs`.

The protected `review/` and `TestResults/` trees were neither changed nor staged. The complete A+
source goal remains active for the next unreviewed owner.

## Iteration 94 outcome

Review the complete `ViciOne.ServiceBus.Testing` assembly as one coherent developer-facing test
platform: direct and dependency-injection harness lifecycle, mediator ownership, observation
retention and identity, inactivity timing, consumer/handler/saga/state-machine/activity harnesses,
telemetry correlation and output, request clients, dynamic endpoints, service registration, and
failure cleanup. Preserve every testing capability while replacing inherited noun-like and
ambiguous public members with explicit Greenfield `Create`, `Add`, `WaitFor`, and lifecycle APIs.

Keep `src/ViciOne.ServiceBus.Testing` as an independent first-party assembly directly below `src`.
`src/ViciOne.ServiceBus` is the Core project, not an umbrella directory; nesting sibling projects
inside it would falsely imply Core ownership and expose them to that SDK project's recursive compile
globs. External providers and integrations remain grouped by their real architectural axes under
`Persistence`, `Scheduling`, and `Transports`. “Provider” or “integration” is the precise term for
those groups; “adapter” is acceptable only as a loose implementation-pattern description.

## Iteration 94 requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-VSB-TEST-HARNESS-LIFECYCLE` | start, failed start, stop, restart, cancellation, and disposal | direct and DI lifecycle tests | serialized transitions, owned bounded cleanup token, rollback after partial start, primary-failure preservation, cleanup aggregation, idempotency, and no created bus leak |
| `REQ-VSB-TEST-HARNESS-DI` | full public service-registration surface | service-collection, service-provider, endpoint-registration, and MultiBus tests | every overload, exact scopes, concrete/interface identity, formatter/rider/client-factory composition, runtime endpoints, typed-bus isolation, and required-argument rejection |
| `REQ-VSB-TEST-HARNESS-OBSERVATION` | consumed, sent, published, received, activity, saga, and state-machine observation | observation-policy, message-list, telemetry, and state-machine collector tests | causal trace isolation, stable identity, missing identifiers, duplicate suppression, retention modes, exact fault ownership, thread safety, and timeout semantics |
| `REQ-VSB-TEST-HARNESS-INACTIVITY` | activity accounting and completion timers | time-provider and inactivity tests | deterministic clock control, cancellation before token access, no post-dispose callback, no late evaluation, restart behavior, and concurrent activity safety |
| `REQ-VSB-TEST-HARNESS-CONSUMERS` | consumer, handler, saga, state-machine, and activity composition | focused harness behavior suites | all factory/configurator/repository/queue overloads, default endpoint attachment, exact handler exception, async saga mutation, and exact state-selector invocation |
| `REQ-VSB-TEST-HARNESS-MEDIATOR` | owned mediator composition and lifecycle | mediator behavior tests | sealed composition boundary, start, request clients, observer connections, synchronous and asynchronous disposal, partial-failure cleanup, and repeated calls |
| `REQ-VSB-TEST-HARNESS-TELEMETRY` | operation execution, idle waiting, response variants, and diagnostic rendering | telemetry and diagnostic-output tests | action and one/two/three-response overloads, causal faults, response propagation, cycle-safe timelines, detail formatting, and external-trace exclusion |
| `REQ-VSB-TEST-HARNESS-API` | complete public Testing package surface | broad reflection/behavior API tests and packed consumer contract | every public type/member, every new or changed optional parameter, intentional rename/removal diff, package-only compilation, and no legacy alias retained |
| `REQ-VSB-ENDPOINT-REGISTRATION` | standalone and container endpoint definition ownership | endpoint-registration and MultiBus tests | non-null selector, authoritative default definition identity, runtime `AddEndpoint(Type)`, per-bus typed definition creation, and no cross-bus collision |
| `REQ-VSB-ASYNC-API` | method name and asynchronous contract agree bidirectionally | architecture async convention tests | semantic inspection of every source project, external callback allowlist, `Async` as the operation suffix, and both positive and negative synthetic cases |
| `REQ-VSB-SOURCE-NAVIGATION` | all Testing source files and moved extension type | source-file naming and repository-graph architecture tests plus manual ledger | matching type/file/namespace/folder responsibility, `SagaStateMachineTestHarnessExtensions.cs`, correct sibling-project placement, no repeated path concept, and no empty directory |
| `REQ-VSB-SOURCE-COMMENTS` | every comment in every Testing source file | manual read plus documentation/comment architecture gates | current code and behavior only; no history, migration narrative, filler, generated rewrite, stale name, or missing public API block |

## Iteration 94 mutation obligations

- Drop observations without identifiers or accept the same identified observation twice: the exact
  message-list identity tests must fail.
- Disable causal trace filtering in an active observation scope: the external-trace isolation test
  must fail.
- Restore a second default endpoint-definition instance or register an unbound typed endpoint
  definition globally: the authoritative-identity test or same-consumer MultiBus test must fail.
- Omit created-bus rollback after a failed harness start: the direct lifecycle test must fail by
  observing a zero stop count.
- Allow the inactivity timer callback to evaluate after disposal: the deterministic race test must
  fail by observing an evaluation count of one.
- Make cancellation before first token access a no-op: the direct/DI cancellation test must fail
  because the subsequently obtained token is not canceled.

## Iteration 94 baseline

The unchanged focused Testing namespace passed 146/146 tests. Manual file-by-file review then found
real lifecycle, identity, and API-design gaps that aggregate coverage alone did not reveal: start
failure could leave a created bus running; DI endpoint-definition aliases could disagree or collide
between typed buses; the consumer harness configuration constructor did not attach its default
endpoint; the handler wait path could lose the exact exception; pre-token cancellation was not
durable; inactivity disposal had a timer race; observation lists conflated missing identifiers and
duplicates; timeline traversal lacked cycle protection; and several inherited public names did not
state whether they created, added, or waited for a resource.

## Iteration 94 completion

All 100 original Testing C# files and all 9,606 physical lines were read manually in full together
with every comment, every original direct Testing test, the project file, requirements, downstream
call sites, and the packed public surface. No generator or scripted source/comment rewrite was used.
The documentation utility was invoked only in read-only `analyze` mode after the manual review; it
reports zero missing public documentation blocks in `ViciOne.ServiceBus.Testing`. The final project
contains 101 C# files and 9,826 physical lines. Every filename, type, namespace, visibility,
responsibility, XML comment, and implementation comment was checked again after remediation.

The direct and container harnesses now serialize lifecycle transitions, carry cancellation issued
before token access, use owned bounded cleanup tokens, stop a bus created before a start failure,
preserve the primary start exception when rollback also fails, aggregate independent cleanup
failures, and dispose idempotently. `BusTestHarness` explicitly implements `IAsyncDisposable`.
`MediatorTestHarness` is sealed and owns its composed mediator and observers without an overridable
half-initialized lifecycle. Consumer construction attaches its configured default endpoint, handler
waits preserve the exact fault, and saga mutation is consistently asynchronous and serialized.

Observation storage is thread-safe, retains missing identifiers, suppresses only identified
duplicates, applies bounded timing without overflow, and filters active scopes by causal trace.
Inactivity evaluation cannot race beyond disposal. Telemetry response overloads preserve every
response and fault, and timeline generation terminates on malformed cyclic span graphs. State-
machine observation registration is eagerly materialized through one internal marker contract so
the DI harness cannot silently omit its collector.

The public API now uses `CreateTaskCompletionSource`, `CreateConsumeObserver`,
`CreateRequestClient`, `AddConsumer`, `AddHandler`, `AddSaga`, `AddSagaStateMachine`,
`AddActivity`, `AddExecuteActivity`, `WaitForMessageAsync`, `WaitForHandledMessageAsync`,
`WaitForConsumerAsync`, `WaitForHandlerExecutionAsync`, `WaitForCompletionAsync`, and
`GetCompletionTasks`. `ITestHarness` owns `StartAsync`, `StopAsync`, and `RestartAsync`; obsolete
synchronous saga mutation and hosted-service extension aliases are absent. All repository call
sites compile against the new names, and the package contract diff contains no unrelated API
change. Every public Testing API and every new or changed parameter has direct compile-time or
runtime coverage in the focused tests and packed consumers; this is distinct from claiming that
every compiler-generated or defensive internal branch executed.

The endpoint-registration fixes are deliberately small Core changes discovered through Testing:
standalone runtime endpoint registrations receive a real selector, default definition interfaces
and concrete types resolve to the same authoritative instance, and typed-bus definitions are
created inside their bound bus registration rather than leaked as one unbound global service. The
same-consumer MultiBus regression proves the isolation boundary.

The focused namespace grows from 146 to 192 runtime cases and passes 192/192. Its 25 executable
test files contain 186 `[Fact]`/`[Theory]` methods and 1,113 assertion call sites (5.98 per method).
Static assertion and anti-pattern review finds no effective assertion-free, trivial-only,
tautological, unawaited, skipped, wall-clock-dependent, random, mutable-static-state, swallowed-
exception, sleep, or debug-output test. One syntactic zero-assertion method delegates to a helper
that performs the complete retention assertion matrix. Large overload/integration matrices remain
cohesive and intentionally verify distinct observable contracts.

Fresh focused instrumentation passes 192/192 and records 93.1402% line and 75.5636% branch coverage
for `ViciOne.ServiceBus.Testing`. Fresh complete Core-host instrumentation passes 3,256/3,256 and
records 94.2073% line and 77.1867% branch coverage for Testing; the cross-package host measurement
is 75.1781% line and 67.9172% branch and is not presented as whole-repository coverage. The complete
artifact SHA-256 is `e55ac29261894bdddd57fe9da13af4829885d671392517412261e3f2cd42ddc7`.
The remaining zero-line Testing methods are an unreachable activity-start defensive helper,
diagnostic `IProbeSite` forwarding methods, two compiler-generated timeline lambdas, and one
reflection-capability lambda; none is a missing public API or parameter path.

Eight isolated source counterchanges were compiled and killed: dropping missing-ID observations,
accepting identified duplicates, disabling causal trace filtering, duplicating the default endpoint
definition, leaking an unbound typed-bus definition, omitting failed-start bus rollback, restoring
the inactivity disposal race, and making pre-token cancellation a no-op. Every counterchange was
restored manually before the final no-incremental build.

Final SHA-256 values are
`acdfb9e161cd9410938988e5f9d0fee6ee6440c942a917405100d1839a80fc8f` for
`BusTestHarness.cs`, `64695b6285f79f23c20ba2e909d1ccd1dd8d491977909a5c73ca0481b425b9c3`
for `ContainerTestHarness.cs`,
`6f6335cd5e019203ebca6f97cf147ec330ce827d66029cbfc82c603411963d1f` for
`MediatorTestHarness.cs`, `4559c87dfe179b7c6d119994a8ae3523131b1934500e70e38602cab3c0dcea46`
for `AsyncElementList.cs`, and
`37ae5d349344e8448bb620b9b967208745f7e96c9a05ae082baf03e307d09f6f` for
`DependencyInjectionEndpointRegistrationExtensions.cs`.

Product, Engineering, and Unit locked restores pass. Their Release builds pass with warnings as
errors and zero warnings/errors, including every Local-Integration project affected by the rename.
The final hermetic Unit/Architecture solution passes 6,121/6,121 tests across 23 hosts with zero
failures and zero skips; the Architecture owner passes 292/292, including all 30 bidirectional async
convention cases and the repository-wide source-file/type/folder checks. Both official format gates,
requirements JSON, `git diff --check`, repository-wide C# preprocessor scan, targeted dummy-marker
scan, public documentation, package graph, and empty-directory checks pass. The first Engineering
format attempt exposed one import-order error in the new endpoint regression test; after manual
correction both gates returned exit code zero and the rebuilt Core host again passed 3,256/3,256.

Fresh-package verification passes once while explicitly updating the five consumer locks and public
contract and again strictly without update flags: 18 developer journeys, 31 freshly packed ViciOne
packages, three executed isolated provider-testing consumers, and all 30 runtime API assemblies.
The reviewed packed API diff is confined to the intended Testing redesign. The final contract has
19,082 lines with SHA-256
`15a337639e0a3430a2e404fe15839c196b94a97e40835ecfa434f0a8dd5120dd`.
The protected `review/` and `TestResults/` trees were neither changed nor staged. The complete A+
source goal remains active for the next unreviewed owner.

## Iteration 95 transport-provider Testing plan

| Requirement | Planned evidence |
| --- | --- |
| Preserve coherent source ownership | all three provider-testing assemblies stay under `src/Transports`; project references, namespaces, package IDs, filenames, and primary public types are checked together |
| Prevent accidental destructive RabbitMQ root cleanup | direct-harness opt-in property plus pre-connection rejection tests; dependency-injection validation remains consistent |
| Prevent an unowned bus when RabbitMQ startup cleanup fails | cleanup moves before factory construction; a failed-cleanup start test proves no provider configuration callback is reached |
| Preserve RabbitMQ management and broker behavior | direct settings, UTF-8 authentication, dedicated-vhost recreation, real-broker cleanup, provider event, cluster, TLS, and cancellation tests |
| Preserve Azure Service Bus cleanup and harness behavior | queue/topic deletion and disappearance races, exact cancellation, administration endpoint, bus/endpoint callbacks, scheduler toggle, and input-address lifecycle tests |
| Cover every provider Testing public API and changed parameter | compile-time API contract plus direct assertions for defaults, setters, callbacks, null/boundary validation, cancellation, and return identity |
| Demonstrate test strength rather than coverage touching | focused gap/assertion review and isolated counterchanges for each repaired safety or lifecycle invariant |
| Complete the family-level quality gates | locked restore, warnings-as-errors builds, focused and full tests, architecture/async checks, format checks, coverage, package/API validation, mutation evidence, and manual final reread |

## Iteration 95 completion

All 15 production C# files and all 1,438 physical lines in the Azure Service Bus, Event Hubs, and
RabbitMQ provider-testing assemblies were read manually in full before and after remediation,
including every comment, public and protected member, internal execution path, project reference,
filename, namespace, and directory owner. No generator or scripted source/comment rewrite was used.
The three independent provider packages remain under `src/Transports`. The directory
`src/ViciOne.ServiceBus` owns the Core assembly rather than all ServiceBus assemblies, so moving
sibling projects beneath it would misstate dependency ownership and expose nested files to the
Core SDK project's recursive source globs. First-party capability assemblies therefore remain
siblings at `src`, while external integrations stay grouped by `Persistence`, `Scheduling`, and
`Transports`; provider or integration is the precise term for these groups.

RabbitMQ direct cleanup now refuses both literal and encoded root virtual hosts unless the caller
sets the explicit `AllowRootVirtualHostCleanup` safety opt-in. Startup cleanup completes before bus
construction, so a cleanup failure cannot leave a newly created bus outside lifecycle ownership.
Direct connections now refresh effective host settings before connecting. Direct and hosted paths
share one UTF-8 management-client, URI, virtual-host, entity-filter, and AMQP-close-reason
implementation; malformed factory results fail causally, and failure cleanup preserves the primary
exception. The hosted path has deterministic seams for full creation, cleanup, configuration,
TLS, transport, cancellation, and failure-order testing. Azure Service Bus has the corresponding
administration-client seam, exact null-factory handling, complete queue/topic race behavior, and a
correct cancellation comment. Event Hubs required no production change.

The Azure focused host grows from 66 to 78 runtime cases and passes 78/78. The RabbitMQ focused
host grows from 197 to 236 and passes 236/236. The focused Event Hubs producer-resolution owner
passes 3/3. A bounded assertion-quality and anti-pattern review covered every changed/direct test
plus the complete three owning test projects: 242 source test methods contain 920 assertion-bearing
lines, and no assertion-free behavioral case, trivial-only assertion, unawaited task, skip,
wall-clock dependency, sleep, debug output, swallowed exception, or mutable shared test state was
found. Every new public member and changed option is exercised by compile-time API projection and
direct behavior assertions.

Six isolated source counterchanges were compiled, executed one at a time, killed by their exact
tests, and restored manually: cleanup after bus construction, failure to classify an encoded root
virtual host, ASCII rather than UTF-8 management authentication, retention of `amq.*` system
entities, inverted Azure scheduler selection, and rejection rather than tolerance of queue/topic
404 disappearance races. The red outcomes observed the exact wrong callback count, missing root
exception, credential bytes, returned entity set, scheduler probe state, and propagated Azure
exceptions respectively.

Real provider acceptance passes through the canonical pinned-fixture runner. Run
`vicione-e49f15eb1361` creates a dedicated RabbitMQ virtual host, declares real queue and exchange
entities, invokes custom cleanup with the exact cancellation token, and proves both entities are
absent afterwards. Run `vicione-f83044ba0dd7` creates a real queue and topic in the local Azure
Service Bus emulator namespace and proves both are absent after harness cleanup. Each runner used
fresh run-scoped credentials and loopback ports and removed its fixture after the test.

Fresh accepted instrumentation passes all focused cases. `ViciOne.ServiceBus.AzureServiceBus.Testing`
records 100% line and 87.5% branch coverage with artifact SHA-256
`96cf3858a23520995fb340443de2650ba05c63264abd48466a6ddf907bd1dc63`.
`ViciOne.ServiceBus.RabbitMq.Testing` records 94.3262% line and 82.1839% branch coverage with
artifact SHA-256 `4b0460d4a9781f991ae3c10d83f0646cad3182bcf792b8fc848c26d25963555d`.
`ViciOne.ServiceBus.EventHubs.Testing` records 100% line and 100% branch coverage with artifact
SHA-256 `664e7fc9547b325c0e367e2b3071f59a34528c3b26ceab4bfe8edbe45be7b88a`.
The remaining RabbitMQ unit-host sequence points are thin default real-client connection and
management seams; their shared decision logic is directly covered, and the public direct-client
path is additionally executed against the real pinned broker. They are not missing public API or
parameter tests.

Product, Unit/Architecture, and Engineering locked restores pass. Both official Roslyn format
gates pass. The complete serial Release Engineering build passes all 77 projects with zero warnings
and zero errors under warnings-as-errors. The first parallel Unit run passed 6,171 cases and exposed
one unrelated Quartz lifecycle timeout under competing load; the exact test then passed alone in
2.2 seconds, and the authoritative serial full solution passed 6,172/6,172 with zero failures and
zero skips. Its Architecture host passes 292/292, including repository-wide bidirectional async
naming and source file/type/folder checks. `git diff --check`, provider-owner preprocessor and dummy
scans, test-smell scan, and empty-directory scan pass.

Fresh-package validation passes once while explicitly updating the intentional API contract and
again strictly without update flags: 18 developer journeys, 31 freshly packed packages, three
executed isolated provider-testing consumers, and all 30 runtime API assemblies. The reviewed
packed API diff contains exactly one line,
`RabbitMqTestHarness.AllowRootVirtualHostCleanup`. The final contract has 19,083 lines with SHA-256
`1e8055f4700954d11ab7cadd4be01251e01fa17fef1702366df7b9d10eb8c843`.
Final source SHA-256 values are
`9650c4cb1623980252d2874a06dece22cceb723e7f6884bf7cbb689660d3965e` for
`AzureServiceBusTestHarness.cs`,
`431ca2a2c1ff298ba32102bcb4e48957af697a36bac6f9d63dc4b725876fe574` for
`AzureServiceBusTestHarnessHostedService.cs`,
`da2670b4150c7c9cca781351cde7e2d1c3bf88fa436a058e2f5ebc283b02be5b` for
`RabbitMqTestHarness.cs`,
`f1183fd20b5869d2e9494c19cbf2ae9bcbafb91d3507f9c532ae4091fe59bf47` for
`RabbitMqTestHarnessHostedService.cs`, and
`ea3f0f20708d1c4eedd07389ce1a25a0f923f464f6ed761ac4e1c9fe1b2f6c39` for
`RabbitMqManagementApi.cs`.
The protected `review/` and `TestResults/` trees were neither changed nor staged. The complete A+
source goal remains active for the next unreviewed owner.

## Iteration 96 analyzer-toolchain plan

| Requirement | Planned evidence |
| --- | --- |
| Preserve the physical assembly model | treat `src/ViciOne.ServiceBus` as the Core project directory; retain independent capability projects as `src` siblings and external integrations under `Persistence`, `Scheduling`, and `Transports`; verify the complete tree through architecture and package gates |
| Keep the shipped analyzer surface intentional | export only the eight diagnostic analyzers and the two code-fix providers; move shared compiler mechanics to an internal matching folder and namespace |
| Recognize every current producer family | direct Roslyn scenarios for Core, application, Advanced, initializer, request, response, and scheduling APIs |
| Make structural message validation total | recursive contracts, inherited/readable properties, concrete interface implementations, collections, dictionaries, headers, nullable values, and every supported or rejected `MessageData<T>` carrier |
| Prevent unobserved message-production work | expression statements, discards, nullable suppression, parentheses, and `ConfigureAwait` wrappers while preserving ordinary variables and observed tasks |
| Enforce cancellation and consumer safety | exact overload shape matching, all available pipeline contexts, inherited context receivers, every configuration write form, and blocking framework synchronization primitives including timed acquisition |
| Modernize packaging without compatibility residue | remove old `install.ps1`/`uninstall.ps1` integration and prove the analyzer package contains only modern Roslyn assets and package metadata |
| Demonstrate test strength | red-first regressions, isolated source counterchanges, focused coverage/CRAP analysis, assertion-quality review, full build/test/format/package gates, and a final manual reread |

## Iteration 96 mutation obligations

- Replace interface-aware conversion with base-class-only conversion: the concrete-interface
  message test must report `VOSB1002`.
- Remove an application producer identity: the complete producer-family test must lose exactly one
  expected diagnostic.
- Remove lock-operation registration or increment/decrement write extraction: the corresponding
  exact rule matrix must lose its expected diagnostic.
- Broaden serializable-member selection: the exact member-name test must report private, static,
  indexer, or write-only members.
- Stop recognizing `ConfigureAwait` or discard assignment: the exact unobserved-task matrix must
  lose the corresponding diagnostic.
- Accept unsupported `MessageData<T>` value carriers: the incompatibility matrix must lose the
  value-type member, and no input is allowed to hang.
- Treat a property with a private getter as readable or omit timed `TryEnter` handling: the final
  red-first regressions must fail on the exact missing property or diagnostic count.
- Every counterchange must be restored manually before final validation. A surviving mutation must
  be investigated as equivalent or a genuine gap rather than reported as killed.

## Iteration 96 completion

All 17 original production C# files in the analyzer, code-fix, and aggregate-package owner were
read manually in full together with every comment, project file, analyzer release record, packaging
script, directly owning test, and requirement projection. No generator or scripted source/comment
rewrite was used. The final owner contains 16 C# files and 3,018 physical lines after removing the
obsolete attribute polyfill and consolidating shared compiler logic under `Internals`.

The eight diagnostic analyzers and two code-fix providers are now the exact exported surfaces.
Code fixes occupy `ViciOne.ServiceBus.Analyzers.CodeFixes`; shared symbol extensions are internal
and live in the matching `Internals` directory. Diagnostic metadata is complete and unique, every
analyzer rejects a null registration context, and no analyzer instance retains compilation-bound
Roslyn state.

Producer recognition covers the current Core, application, Advanced, request, response,
initializer, and scheduling families. Unobserved producer tasks are found through transparent
parentheses, nullable suppression, `ConfigureAwait`, and discard assignment. Message-contract
analysis now terminates on recursive graphs, handles concrete interface implementations, uses
publicly readable instance properties only, de-duplicates inherited properties, validates the
canonical header set, and rejects unsupported `MessageData<T>` value carriers without looping.
Cancellation analysis understands lambda, accessor, indexer, constructor, local-function,
extension, and inherited consume-context receivers. Consumer rules cover every assignment form,
language locks, blocking waits, and timed `Monitor`/`SpinLock` acquisition while leaving immediate
`Monitor.TryEnter` unreported.

The focused analyzer host grows from 126 to 164 tests and passes 164/164. The focused code-fix host
grows from 32 to 36 tests and passes 36/36. Final instrumentation records 95.5538% line and
85.4072% branch coverage for `ViciOne.ServiceBus.Analyzers`, complexity 905, 174 methods, and no
CRAP score above 30. The code-fix assembly records 94.4915% line and 71.9697% branch coverage,
complexity 138, and 29 methods. Its sole score above 30 is the compiler-generated async `MoveNext`
for the fully line-covered recursive traversal; the highest genuine method score is 25.87.

Eight non-equivalent isolated source counterchanges were killed and restored: concrete-interface
conversion, an application producer identity, lock registration, increment/decrement write
extraction, serializable-member filtering, `ConfigureAwait` recognition, discard recognition, and
unsupported value-type `MessageData<T>` acceptance. Two attempted counterchanges to special-case
reduced extension receivers survived because Roslyn's ordinary receiver path already handled them;
the redundant production branch was removed. The final manual reread additionally produced two
red-first failures for private getters and timed `TryEnter`; both now pass and remain exact
regressions.

The aggregate package no longer contains NuGet `tools` scripts. A fresh `.nupkg` contains only its
relationship/content metadata, README, and the analyzer plus code-fix assemblies under
`analyzers/dotnet/cs`. Product, Engineering, and Unit locked restores pass. The final serial
Engineering Release build passes all 77 projects with zero warnings and errors. The complete
Unit/Architecture solution passes 6,214/6,214 with zero failures and zero skips; Architecture
passes its repository-wide bidirectional async and file/type/folder rules.

Both complete Roslyn format gates, requirements JSON, Git whitespace, analyzer-owner preprocessor,
dummy-marker, and empty-directory checks pass. Final fresh-package validation passes with 18
developer journeys, exactly 31 packages, three executed isolated provider-testing consumers, and
all 30 runtime API assemblies matching the 19,083-line committed contract at SHA-256
`1e8055f4700954d11ab7cadd4be01251e01fa17fef1702366df7b9d10eb8c843`.

The structured build diagnosis is repeatable: the sandbox forbids Microsoft Testing Platform and
Roslyn named-pipe creation (`SocketException: Permission denied`) and may leave `pack`/restore
waiting after its child process has gone. Focused compilation can run inside when it does not hit
that boundary; authoritative coverage, restore, pack, format, and full-suite gates run outside the
sandbox with build-server reuse disabled where appropriate. The protected `review/` and
`TestResults/` trees remain untouched and unstaged. The repository-wide A+ source goal remains
active for the next unreviewed owner.

## Iteration 97 Saga owner plan

Iteration 97 reviews `src/ViciOne.ServiceBus.Sagas` as one coherent owner. The baseline contains
357 production C# files and 32,433 physical lines. Earlier work moved saga contracts into their
own assembly and repaired individual concurrency and request-outcome defects, but it did not read
or accept the complete owner file by file. This iteration therefore covers the public classic-saga
and state-machine API, repository execution, dependency-injection composition, middleware,
scheduling, fault handling, comments, filenames, namespaces, and physical folders together.

| Requirement | Planned evidence |
| --- | --- |
| Preserve every saga feature | direct and integration tests for classic sagas, state machines, queries, repositories, requests, scheduling, retries, observations, and container composition |
| Make registration deterministic | exact DI service graphs, duplicate/idempotent registration behavior, argument validation, definition ownership, and concurrent resolution tests |
| Make repository lifecycle terminal | insertion, load, query, update, delete, discard, undo, cancellation, failure, disposal, and concurrent-removal tests |
| Make state-machine execution total | every configured/unhandled event path, transition, activity, condition, catch, fault, request, schedule, composite event, and completion branch |
| Remove compatibility and dummy seams | every sentinel, fallback, unsupported member, historical alias, placeholder concept, and public implementation type must prove a current capability or be replaced without feature loss |
| Align source navigation | every type, filename, namespace, directory, and visibility must communicate its owner; repeated `Sagas`/`Saga`/`SagaStateMachine` concepts require an explicit final disposition |
| Make async and cancellation semantics exact | bidirectional async naming, token propagation, no sync-over-async, and terminal cancellation identity across all asynchronous paths |
| Make comments truthful | manually read every production file and comment; retain only current code and functional semantics, with no generated, procedural, stale, or filler prose |
| Demonstrate test strength | static source/test pairing, assertion and anti-pattern review, isolated source mutations, focused and full MTP runs, package coverage/CRAP, architecture, format, package, and hygiene gates |

## Iteration 97 baseline and mutation obligations

The unchanged baseline passes 3,256/3,256 Core tests, including 130/130 tests in the direct
`SagaStateMachine` namespace and 17/17 in `Sagas`, with no skips. Instrumentation records 60.8131%
line and 52.8113% branch coverage for `ViciOne.ServiceBus.Sagas`, complexity 2,947 across 2,525
methods, 18 CRAP scores above 30, and 58 additional scores between 15 and 30. The mandatory static
pairing heuristic identifies 269 source files without a filename-based test pair; indirect
integration coverage must be distinguished from genuine missing behavior.

- Remove or bypass one DI registration branch: an exact service-graph or runtime-resolution test
  must fail.
- Change one state-machine dispatch, unhandled-event, or completion branch: the exact observable
  state, fault, or repository terminality assertion must fail.
- Change missing-instance retry exhaustion or delay selection: the exact redelivery count, delay,
  and terminal-pipe assertion must fail.
- Change repository save/delete/discard/undo selection or disposal order: the exact lifecycle test
  must fail.
- Change a schedule/fault/request activity branch: the exact message, address, token, and terminal
  state assertion must fail.
- Change one transition-event classifier shared by binders: every applicable binder family must be
  distinguished by direct evidence.
- Reintroduce an obsolete alias, fallback, dummy seam, repeated owner path, stale comment, or async
  naming mismatch: architecture or hygiene evidence must fail with the exact identity.
- Every non-equivalent counterchange is run separately and restored manually before final gates;
  surviving mutations are investigated rather than reported as killed.

## Iteration 97 runtime and capability completion

The complete 357-file baseline Saga owner and every comment were manually read; the three new
types, their callers, and their tests were then reviewed in full. No generator or scripted rewrite
authored source or comments. The final owner contains 359 C# files and 32,622 lines. Its assembly
remains a sibling of Core because it is an optional capability depending on Core. Provider
assemblies remain grouped by `Persistence`, `Scheduling`, and `Transports`; within the Saga
assembly, public domain/configuration contracts, repository runtime contexts, and state-machine
implementation retain distinct `Sagas`, `Saga`, and `SagaStateMachine` responsibilities.

Repository capabilities are now truthful and fail closed. The dispatch repository no longer
pretends to support load/query operations through throwing stand-ins, and dependency injection no
longer registers a dispatch service that fails by design. Explicit loadable and queryable
capability contracts replace those dummy paths, all affected persistence providers select their
real capability, and a saga without an explicitly selected persistence provider fails during
configuration. Missing-instance redelivery now schedules a real message with its complete
metadata and observable retry lifecycle. Faulted scheduling and state-machine execution preserve
the exact cancellation token and cancellation terminality across completion, dispatch, nested
scheduling, transitions, observers, and telemetry cleanup. Implementation-only types are internal
and sealed, required dependencies are guarded, probes expose current behavior, and changed
comments describe only the resulting code contract.

Nineteen exact requirement-mapped cases were added: four missing-instance redelivery cases, nine
repository capability/configuration cases, four state-machine cancellation cases, and two
faulted-schedule cancellation cases. Five isolated source counterchanges were killed and restored
for redelivery delay, both schedule-cancellation variants, pre-canceled completion, and mandatory
repository selection. The changed-test assertion/anti-pattern audit finds no shallow, unawaited,
blocking, time-dependent, skipped, or shared-state test behavior.

Fresh full-host coverage records 62.4669% line and 54.4440% branch coverage for
`ViciOne.ServiceBus.Sagas`, up from 60.8131% and 52.8113%. Methods above CRAP 30 fall from 18 to 15.
This large declarative owner therefore remains in the source-wide completion audit; the metrics are
not presented as total behavioral coverage. The full Core host passes 3,275/3,275. The authoritative
serial Unit/Architecture solution passes 6,233/6,233 with zero failures and skips, including
292/292 repository-wide async naming and file/type/folder architecture cases. The complete
Engineering Release build passes with zero warnings and errors.

Requirements JSON, whitespace formatting, Git whitespace, preprocessor, dummy-marker,
empty-directory, and changed-test smell checks pass. A Saga-only info-level style audit contains no
warning or error and classifies 660 optional suggestions: 323 conflict with the intentional shared
namespace model, 162 suggest primary constructors, 128 are non-semantic expression preferences,
and 47 identify historical unprefixed interface names. The 47 interface findings are the bounded
next Saga API iteration and will be evaluated contract by contract rather than mechanically
renamed. Fresh package validation passes 18 developer journeys, 31 packages, three isolated
provider consumers, and all 30 runtime API assemblies. The intentional 19,029-line API contract has
SHA-256 `6870002dc25251fe785d4e0bbd51a0f66c15ce533a3be92beb78224a2fa28486`.

The protected `review/` and `TestResults/` trees were not changed or staged. Iteration 97 is ready
for its commit, annotated tag, normal remote push, and verification; the overall A+ goal remains
active for the explicit Saga interface-naming decision and subsequent unreviewed source owners.

## Iteration 98 Saga interface and documentation plan

Iteration 98 starts from remotely verified commit
`2234d03407d2ad2570c12e823e4fe0a01087be9d` and annotated tag
`servicebus-a-plus-remediation-iteration-97-2026-09-13`. It resolves the 47 unprefixed interface
declarations identified by the Saga-only style audit and rechecks every affected comment against
the already manually read implementation. The Microsoft .NET library naming guidance and CA1715
both require interface names to begin with `I`; the old fluent/message-contract naming pattern is
therefore not retained merely for compatibility.

| Requirement | Planned evidence |
| --- | --- |
| Apply one .NET naming rule | all Saga interfaces, including nested internal contracts, begin with `I` followed by an uppercase letter |
| Preserve meaning and features | each rename retains generic arity, variance, inheritance, members, attributes, and all concrete implementations |
| Preserve consumer usability | every source, test, sample, benchmark, provider, reflection identity, expression, and isolated package consumer uses the new identities |
| Align navigation | a top-level interface's filename matches its renamed primary type; multi-arity families remain in one canonical file |
| Make comments current | manually rewrite every affected generic, stale, or implementation-history comment after rereading its declaration and implementation |
| Reject regressions | a red-first architecture rule reports all 47 exact violations and becomes a permanent repository requirement |
| Demonstrate API safety | strict builds, focused behavior/API tests, complete Unit/Architecture run, packed API diff, developer journeys, coverage, mutations, and hygiene gates |

No generator will author behavior or documentation. A symbol-aware rename may mechanically update
references only after each target identity has been classified; every resulting declaration,
filename, public signature, XML reference, and consumer diff is manually inspected. Any semantic
change is implemented and tested separately.

### Iteration 98 completion

The 47 Saga interfaces and 30 corresponding top-level filenames are normalized, their complete
consumer closure is updated, and every affected declaration comment has been reread and corrected.
The permanent red-first naming rule and an isolated killed mutation protect the result. The final
Release build, 6,234-test Unit/Architecture solution, 293-test architecture host, package journeys,
format checks, and hygiene gates pass. Fresh Saga coverage remains 62.4669% line and 54.4440%
branch; its 15 methods above CRAP 30 remain explicit risk inputs rather than hidden completion.

The reviewed layout decision is retained: `src/ViciOne.ServiceBus` is the Core project directory,
independent capability projects are direct `src` siblings, and only cohesive external provider
families are grouped below `Persistence`, `Scheduling`, and `Transports`. Iteration 99 will inventory
and select the next complete source owner while preserving that ownership model.

## Iteration 99 Initializers owner plan

Iteration 99 reviews `ViciOne.ServiceBus.Initializers` as one complete owner: eight product files,
598 lines, and its project definition. Each file and comment is read manually. No behavior or
documentation generator is used.

| Requirement | Planned evidence |
| --- | --- |
| Preserve the public initializer API | exact forwarding, capability, null, cancellation, and package-contract tests remain green |
| Remove needless internal abstractions | replace the two one-implementation payload-key interfaces with sealed owner-specific context types |
| Preserve per-initialization value sharing | a direct test proves distinct explicit ID and timestamp variables use the first value cached in one initialization context |
| Apply .NET naming rules | a red-first project architecture rule reports both old internal interfaces and remains green after remediation |
| Validate navigation and comments | every type, namespace, filename, directory, and XML comment is checked against current behavior |
| Demonstrate owner completion | focused tests, killed mutation, project/full builds, complete tests, format, package/API, coverage, and hygiene gates |

The static Roslyn pairing heuristic reports all eight source files as paired to tests. This is a
discovery aid, not line, branch, or assertion-strength evidence; the relevant tests and assertions
are reread directly before implementation.

### Iteration 99 completion

The complete Initializers owner has been read and reviewed manually. Its public facade remains in
the independent `ViciOne.ServiceBus.Initializers` capability project, while its source folders now
mirror the two public namespace branches relative to the explicit `ViciOne.ServiceBus` root. The
two one-implementation cache-key interfaces are replaced by sealed context types, the ID local-name
copy error is corrected, and timestamp capture accepts a testable `TimeProvider` while retaining the
parameterless system-time convenience API.

Three requirement-mapped behavioral tests and two permanent architecture tests protect the result.
All four meaningful isolated counterchanges were killed after the initially surviving timestamp
default was converted into a direct test gap and closed. The final Initializers package has 100%
line and branch coverage. The complete Engineering build, 6,239-test Unit/Architecture solution,
both full format gates, package journeys, requirements, API-contract, source-hygiene, and Git
whitespace gates pass. The 19,030-line packed contract has SHA-256
`a31b98d00ab15941a47bef08aa05447a24db4e445aba00d0838a0686ca85aa0a`.

The overall A+ goal remains active for the remaining complete source owners and the final
repository-wide completion audit.

## Iteration 103 Mediator owner plan

Iteration 103 starts from remotely verified commit
`c8f7915f48449defd9369cdfb5d10b90b10f09f8` and annotated tag
`servicebus-a-plus-remediation-iteration-102-2026-09-14`. It treats the complete 28-file Mediator
capability as one source owner. Every production file, declaration, implementation, and comment is
read manually before any behavioral or documentation change; no source or comment generator is
used.

| Requirement | Planned evidence |
| --- | --- |
| Preserve the complete in-process messaging capability | dispatch, publish, send, request, observer, scope, serialization, expiration, cancellation, and dependency-injection tests remain green |
| Apply the repository assembly/layout model | retain Mediator as an independent `src` sibling and align each internal path with its declared namespace relative to the project's root namespace |
| Modernize the public Greenfield API | inspect every public type, member, parameter, default, name, null boundary, cancellation boundary, and configuration entry point without retaining compatibility aliases |
| Keep implementation and comments accurate | manually verify every comment against the code and remediate stale, historical, redundant, or misleading text while reviewing the corresponding behavior |
| Close test gaps rather than count test names | map API and meaningful branches to assertions, add red-first tests for uncovered boundaries, and kill controlled counterchanges for substantive corrections |
| Demonstrate owner completion | focused and full builds/tests, owner and aggregate coverage/CRAP, assertion review, format, package/API, async naming, directives, hygiene, and namespace/file gates |

Any cross-owner correction discovered through Mediator's consumer closure is included only when it
is required to preserve a coherent public contract or to close a proven behavioral defect. The
protected `review/` and `TestResults/` trees remain outside the iteration.

### Iteration 103 completion

The complete 28-file Mediator owner was read and reviewed manually without generating source or
comments. Mediator remains an independently packaged sibling of Core. Its explicit
`ViciOne.ServiceBus` root namespace now makes `Advanced/`, `Configuration/`, `DependencyInjection/`,
and `Mediator/` express the namespaces and responsibilities they own; contexts and runtime
implementation are nested below `Mediator/`. The standard Microsoft dependency-injection extension
keeps its conventional `Microsoft.Extensions.DependencyInjection` namespace and its physical entry
point is tested as the documented exception to the repository namespace-path rule. Empty legacy
directories were removed.

The public Greenfield API now rejects a null explicit base address and requires the registration
callback that declares mandatory message limits. Both message-limit configuration forms use the
same fluent return convention. Red-first tests prove these contracts before service-collection side
effects, cover every service-collection and configuration null boundary, verify request-extension
arguments, and execute the actual runtime-object pipe overloads and all scoped/runtime/client-context
connector forms. Two permanent architecture requirements protect interface naming and
namespace-relative layout. The focused Mediator profile passes 91/91.

The final Engineering Release build passes all 77 projects with zero warnings and errors. Both full
Roslyn format gates make no changes. All 23 native hermetic hosts pass 6,260/6,260 with no failures
or skips, including 303 architecture and 3,291 Core-host tests. One earlier Core run reported a
single transient failure before its output could be retained; five consecutive complete Core runs,
including the accepted final matrix, then passed 3,291/3,291. Native MTP hosts are executed directly:
forwarding the VSTest `--logger` option through solution-level `dotnet test` selected no tests and is
not an authoritative result.

Fresh Core-host coverage is 75.4858% line and 68.1517% branch across the host's reachability closure.
Mediator is 90.7182% line and 77.5974% branch across 268 methods with complexity 322 and no CRAP score
above 30. The coverage artifact is
`/private/tmp/vsb-iteration103-mediator-gapcheck.cobertura.xml`, SHA-256
`61f6f26fee246a7dfacd7388f78e56a65f3889d3816458cd3facef63af084cc7`. Package validation passes in
both explicit update and immutable comparison modes: 18 journeys, 31 fresh packages, three isolated
provider-testing consumers, and 30 runtime APIs. The intentionally changed 19,030-line packed API
contract has SHA-256 `9f0d543184d729768ba0606420ca05d005c6e1bd1961bfeda472600d18985345`.

Requirements JSON, Git whitespace, C# preprocessor, dummy-marker, old-identity, and empty-directory
checks pass. The sole textual `NotImplementedException` is the executable policy that classifies
that application failure as non-retryable, not a dummy implementation. The protected `review/` and
`TestResults/` trees remain unchanged and unstaged. The overall A+ goal continues with the remaining
complete source owners and the final repository-wide audit.

## Iteration 104 Core runtime, operations, and observability plan

Iteration 104 starts from remotely verified commit
`f9ea9fd0c8338eb344f6683119ae56250a851933` and annotated tag
`servicebus-a-plus-remediation-iteration-103-2026-09-14`. It reviews the 30 production files and
3,531 lines that jointly own the Core runtime lifecycle, generic host integration, health and probe
operations, logging, metrics, tracing, and telemetry. This is one coherent runtime-observability
boundary rather than five artificially small directory passes. Every file and comment is read
manually; no source or comment generator is used.

| Requirement | Planned evidence |
| --- | --- |
| Make lifecycle ownership deterministic | start, ready, stop, timeout, cancellation, fault, repeated-call, and partial-start cleanup paths have exact behavior and disposal evidence |
| Keep observability side-effect safe | hostile loggers, listeners, meters, health checks, and telemetry callbacks cannot change messaging or lifecycle outcomes |
| Preserve diagnostic fidelity | probes, health reports, activities, metrics, headers, and durable-send outcomes expose exact identities and failure details without mutable aliases |
| Modernize the Greenfield surface | every public member, parameter, default, name, async contract, and capability boundary is reviewed without compatibility-only API |
| Align navigation and comments | filenames, types, namespaces, folders, and manually verified comments match the final responsibility model |
| Demonstrate test strength | red-first regression tests, controlled counterchanges for substantive fixes, focused and full tests, coverage/CRAP, build, format, package/API, requirements, and hygiene gates |

Cross-owner changes are limited to proven consumers needed to preserve a coherent runtime contract.
The protected `review/` and `TestResults/` trees remain outside the iteration.

### Iteration 104 completion

The complete 30-file, 3,531-line Core runtime, hosting, operations, logging, monitoring, and
telemetry owner was read and reviewed manually, including every source comment. `ViciOne.ServiceBus`
remains the Core assembly rather than an umbrella directory: separately delivered capability and
provider assemblies remain siblings under `src`, while every reviewed Core folder, namespace,
filename, and primary type follows its responsibility relative to that assembly.

Tracing creation is no longer coupled to the ambient logging context. The activity implementation
is now named `MessageActivity`, metric ownership is expressed by `LogContextMetricsExtensions` and
`LogContextMetricsState`, and Saga and Courier tracing have dedicated capability-local helpers.
All affected callers use explicit `TryStart...` names. A permanent syntax-aware architecture rule
rejects any future `StartedActivity` initialization gated by `LogContext.Current`. Durable and
payload metrics now use precise OpenTelemetry annotation units for decisions, requests, attempts,
completions, and messages, with exact instrument tests and matching documentation.

The review also closed two unrelated defects encountered through the consumer closure. Creating a
probe scope over an existing scalar now fails without replacing the scalar. All four public saga
message-filter variants validate both pipeline arguments before invoking the saga or creating
observability side effects, and their previously copied consumer/send comments now describe their
actual saga behavior. Red-first architecture, metric, naming, tracing, and saga tests plus a killed
probe counterchange demonstrate that the regressions are observable. Three deterministic fake-time
tests additionally prove the default readiness timeout and both failing and timed-out startup
cleanup paths without wall-clock delay or coverage-only assertions.

The complete Engineering Release build passes with zero warnings and errors. Both full format
gates make no changes. All 23 current native Unit/Architecture hosts pass 6,267/6,267 with no
failure or skip, including 304 architecture and 3,297 Core-host tests; removed legacy hosts are not
included. The 49 reviewed tests contain no assertion-free, trivial, self-referential, skipped,
wall-clock, or swallowed-exception cases. Fresh Core-host coverage is 75.5275% line and 68.1951%
branch across its reachability closure. The completed owner is 94.1785% line and 85.6209% branch
across 242 methods, with no CRAP score above 30; `StartCoreAsync` improved to 98.08% line coverage
and CRAP 26. The accepted coverage artifact is
`/private/tmp/vsb-iteration104-core-final.cobertura.xml`, SHA-256
`3033f05dbc6f5d44911dd43e055eb6b145edd4c8ad29ee49e4100bdec49e6e0b`.

Package validation passes 18 journeys, 31 fresh packages, three isolated provider-testing
consumers, and all 30 runtime package APIs. The unchanged 19,030-line packed contract has SHA-256
`9f0d543184d729768ba0606420ca05d005c6e1bd1961bfeda472600d18985345`.
Requirements JSON, Git whitespace, C# preprocessor, dummy-marker, obsolete observability-identity,
activity-gating, and empty-directory checks pass. The sole textual `NotImplementedException` is the
executable non-retry classification policy. The protected `review/` and `TestResults/` trees remain
unchanged and unstaged. The overall A+ goal continues with the remaining complete source owners and
the final repository-wide completion audit.

## Iteration 105 Core transactions and message-journal plan

Iteration 105 starts from remotely verified commit
`d8cb018a4980ec140cf10a4753da1507aa526165` and annotated tag
`servicebus-a-plus-remediation-iteration-104-2026-09-14`. It treats the 29 production files and
1,709 lines in `Transactions/` and `MessageJournal/` as one coherent delivery-integrity owner:
ambient and managed transaction lifetime, deferred and buffered publish/send behavior, journal
capture policy, immutable entry projection, store limits, observer isolation, and telemetry. Every
file and comment is read manually before changes; no source or comment generator is used.

| Requirement | Planned evidence |
| --- | --- |
| Preserve transactional semantics | commit, rollback, enlistment, buffering, deferred endpoint, cancellation, ordering, and failure paths retain exact ownership |
| Preserve journal correctness | policy, classification, projection, limits, metadata, clocks, immutable snapshots, observer behavior, and store failures have exact assertions |
| Modernize the Greenfield surface | every public type, member, parameter, name, async contract, null boundary, and default is reviewed without compatibility-only API |
| Align navigation and comments | project-relative namespaces, directories, filenames, primary types, and every manually verified comment express the final responsibilities |
| Demonstrate test strength | source-to-test mapping, pseudo-mutation analysis, red-first regressions, controlled counterchanges, focused/full tests, coverage/CRAP, format, package/API, requirements, and hygiene gates |

The provider-specific journal stores remain in their separately delivered Persistence assemblies;
this iteration owns only the provider-neutral Core contract and runtime. The protected `review/`
and `TestResults/` trees remain outside the iteration.

### Iteration 105 completion

The complete 29-file, 1,709-line transactions and provider-neutral message-journal owner was read
and reviewed manually, including every source comment. The source layout correctly represents
assembly ownership: `src/ViciOne.ServiceBus` is the Core assembly, independently delivered
assemblies remain sibling projects under `src`, and Persistence, Scheduling, and Transports are
integration/provider families rather than folders inside Core. Within the reviewed Core owner,
folders, filenames, namespaces, and primary types align with their final responsibilities.

Message-journal telemetry now spans policy projection and optional storage instead of describing an
already completed write. Stable public activity, metric, and attribute names expose low-cardinality
operation, outcome, result, and failure-phase dimensions; activities inherit the ambient parent,
carry sampling-time identity, terminate with an exact status, and cannot let hostile start or stop
listeners affect journal or message semantics. Metrics have precise units and descriptions. Exact
tests cover every terminal failure reason, elapsed-time and clock failures, filtering, storage,
oversize rejection, content-size accounting, listener isolation, and real in-memory send, publish,
and consume envelopes, including faults and preserved scheduled, lifetime, header, and exception
metadata.

Deferred bus boundaries now reject invalid observer, pipe, endpoint-definition, queue-name, and
probe arguments locally. A strict forwarding spy proves every remaining member preserves arguments,
handles, endpoints, address, and topology; buffered send contexts and cancellation/failure recovery
have end-to-end assertions. The publish endpoint helper no longer advertises an interface it cannot
coherently implement or carry a compatibility-only observer indirection. Completed ambient
transactions reject enlistment without retaining the action. Twelve red-first tests and four killed
controlled counterchanges prove sampling tags, local receive-observer validation, buffered-action
restoration, and faulted send classification are behaviorally observable.

A fresh dependency-graph build exposed stale locked closures after the internal-access test assembly
gained the Sagas reference. All eleven consuming lock files were regenerated with force evaluation;
normal locked restores for Product, Engineering, and Unit graphs then pass, and all lock files and
requirements manifests parse successfully. The Engineering Release build passes all 77 projects
with zero warnings or errors, and the product pack plus package-only developer-journey gate passes
18 scenarios, 31 fresh packages, three isolated provider-testing consumers, and all 30 runtime APIs.
The intentional public contract change is exactly eight telemetry constants. The resulting
19,038-line packed API has SHA-256
`a613f715b7cacada8b4edc424785e088535c4e79ce6b156eda58f21ee7c4cb37`.

Both full format gates pass without changes. All 23 native Unit/Architecture hosts pass
6,279/6,279 tests with no failure or skip, including 3,309 Core-host tests. The reviewed tests contain
no assertion-free, trivial, self-referential, skipped, swallowed-exception, random, or wall-clock
sleep cases; the two infinite delays are cancellation-controlled blocking test doubles. Fresh
Core-host coverage is 75.6155% line and 68.2642% branch across its reachability closure. The completed
owner covers 666/671 executable lines (99.2548%) and 125/138 branch outcomes (90.5797%) across 156
methods, with no CRAP score above 8. The five unreachable lines are defensive null-event,
double-prepare, and queue-enqueue exception paths. The accepted artifact is
`/private/tmp/vsb-iteration105-core-final.cobertura.xml`, SHA-256
`5d1e09206bfaacb01fc1a79e635d72b4159c19710b31c815b9510ff3fa09967d`.

Git whitespace, C# preprocessor, dummy-marker, old-identity, async-convention, JSON, lock-graph, and
empty-directory checks pass. The initial in-sandbox format attempt failed solely because Roslyn was
denied its local named pipe; rerunning the identical command outside the sandbox passed, preserving
the established diagnostic rule for local .NET build hosts. The protected `review/` and
`TestResults/` trees remain unchanged and unstaged. The overall A+ goal continues with the remaining
complete source owners and the final repository-wide completion audit.

## Iteration 106 Core dependency-injection configuration plan

Iteration 106 starts from remotely verified commit
`affdec42f1ece80d4ec470cb631d26a80b297ae8` and annotated tag
`servicebus-a-plus-remediation-iteration-105-2026-09-14`. It treats the 66 production files and
6,102 lines currently split between `Configuration/DependencyInjection/` and
`DependencyInjection/Configuration/` as one coherent composition owner: public registration
contracts and extensions, container registration, endpoint definitions, bus and rider contexts,
scope-pipeline observers, health options, and transport-factory composition. Every file and comment
is read manually before changes; no source or comment generator is used.

| Requirement | Planned evidence |
| --- | --- |
| Make composition deterministic | registration identity, replacement, duplicate, order, scope, endpoint, rider, observer, health, and validation paths have exact behavior tests |
| Modernize the Greenfield surface | every public type, member, parameter, default, name, async contract, and extension location is reviewed without compatibility-only API |
| Resolve the split source layout | folder and namespace ownership is decided from final responsibilities and assembly/API boundaries, with moves validated by source-navigation and package gates |
| Keep DI lifetime-safe | root/scoped ownership, disposal, cancellation, callback, and failure paths cannot leak or resolve from the wrong provider |
| Correct every comment manually | XML and implementation comments describe only the final code and current behavior after the containing file has been understood |
| Demonstrate test strength | source-to-test mapping, pseudo-mutation analysis, red-first regressions, controlled counterchanges, focused/full tests, coverage/CRAP, format, package/API, requirements, and hygiene gates |

Cross-owner edits are limited to proven consumers and architecture rules required to preserve a
coherent composition contract. The protected `review/` and `TestResults/` trees remain outside the
iteration.

### Iteration 106 completion

Iteration 106 completes the 66-file Core dependency-injection configuration owner after a manual
read of every production file and comment. The physical boundary is now explicit:
`src/ViciOne.ServiceBus` owns the Core assembly, independent deliverable assemblies remain sibling
projects under `src`, and external provider families remain grouped under `Persistence`,
`Scheduling`, and `Transports`. Inside Core, all registration configuration now has one owner at
`Configuration/DependencyInjection`; the advanced public facade resides at
`Advanced/Registration`, while `DependencyInjection` retains only runtime container concerns.

The iteration removes a duplicated 678-line handler extension implementation, the no-op
`AddHandler<T>()` overload, the empty transactional-outbox compatibility interface, the removal API
that could not safely undo registration, and throwing endpoint-setting placeholders. It corrects
request-timeout inheritance, rider completion, execute/compensate filter selection, owner-specific
registration identity, factory null boundaries, open-generic consumer rejection, endpoint
definition composition, transport-specification failure preservation, and ambiguous or missing
consumer-kind ownership. `BusRegistrationContext.ConfigureEndpoints` is decomposed into explicit
registration and endpoint planning phases so its behavior is reviewable and its CRAP score is
bounded without feature loss.

The new dependency-injection contract suite has 33 test methods and 34 cases with direct state,
identity, lifetime, ordering, exception, topology, callback, and asynchronous failure assertions.
The handler guard test exercises all 16 public overloads, the tenant test adds the typed execute
filter path, and two permanent architecture tests enforce physical ownership and the absence of
empty compatibility API. A full-run-only observation race in the in-memory scheduled-publish test
was corrected by asserting the actual consume snapshot at both time boundaries instead of treating
every completed observation task as delivery. The test passes in isolation, under parallel
repetition, and in the final full run.

One red-first test exposed the accepted open-generic consumer defect. Six isolated controlled
counterchanges were killed and restored, including timeout inheritance, idempotent completion,
filter selection, endpoint identity, early specification validation, and service-instance owner
cardinality. Changed-test assertion and smell review finds no assertion-free, trivial,
self-referential, skipped, random, wall-clock-sleep, or swallowed-exception case.

The final Engineering Release build passes all 77 projects with zero warnings and errors. Both
complete format gates make no changes. All 23 Unit/Architecture hosts pass 6,317/6,317 tests with no
failure or skip, including 3,345 Core-host tests. Fresh owner coverage is 82.7847% line and 76.4354%
branch across 539 methods; no method exceeds CRAP 30 and the maximum is 29.0179. The accepted raw
artifact is `/private/tmp/vsb-iteration106-core-risk3.cobertura.xml`, SHA-256
`f61d55f3c854c5aca80d392b9db98721d8a54fc746ac3b8d06e797bf8c25053c`.

Package validation passes 18 developer journeys, 31 freshly packed packages, three isolated
provider-testing consumers, and all 30 runtime package APIs. The intentional 18,879-line packed API
contract has SHA-256 `09218528f7e3f0b9c54ea3142587fa165c28ad017e590a7b042b6efcea9b076e`.
Requirements JSON, Git whitespace, source directives, dummy markers, obsolete-path references,
empty directories, and protected-tree checks pass. The overall A+ goal remains active for the
remaining complete source owners and final repository-wide audit.

## Iteration 102 Courier interface contract and navigation plan

Iteration 102 starts from remotely verified commit
`f3c660c3641c23d756a09f54b7869ad9213adcd6` and annotated tag
`servicebus-a-plus-remediation-iteration-101-2026-09-13`. The complete 135-file Courier owner was
manually read and behaviorally remediated in Iteration 87. This coherent follow-up rereads every
remaining unprefixed interface, its consumers and comments, then validates the complete project's
namespace/path model.

| Requirement | Planned evidence |
| --- | --- |
| Apply one .NET interface convention | all public and internal Courier interfaces begin with `I` followed by an uppercase letter |
| Preserve every routing-slip feature | contract members, inheritance, attributes, immutable collections, serialization, event routing, execution, compensation, and correlation remain equivalent |
| Preserve consumer usability | all source, tests, samples, reflection identities, message mappings, package journeys, and packed API references use the new identities |
| Align source navigation | retain Courier as an independent sibling capability; map every file to its namespace relative to an explicit `ViciOne.ServiceBus` root namespace |
| Keep comments accurate | reread and manually correct affected declarations and references; do not generate documentation |
| Reject regressions | red-first naming and folder rules, focused tests, isolated counterchanges, full build/test/coverage/format/package and hygiene gates |

Symbol-aware rename support may update references only after manual declaration classification. It
must not generate source behavior or comments. The permanent Greenfield API retains no legacy alias.

### Iteration 102 completion

Iteration 102 completes the Courier Greenfield interface and source-navigation normalization. All
16 formerly unprefixed Courier interfaces now use the .NET `I` convention, every corresponding
filename and consumer uses the same identity, and no compatibility alias remains. Courier stays an
independent capability project beside Core; inside it, the explicit `ViciOne.ServiceBus` root
namespace makes `Advanced/`, `Configuration/`, `Context/`, `Courier/`, `DependencyInjection/`,
`Logging/`, `Middleware/`, and `Transports/` reflect the declared namespace branches. The three
root files that declare `ViciOne.ServiceBus` exceptions remain correctly at the project root.

The manual registration review removed unused overload chains, decomposed activity discovery by
responsibility, and closed a real API boundary: execute-only registration now rejects a
compensatable activity through both generic and runtime-type entry points. A controlled mutation
of both guards produced exactly two failing cases and the restored implementation passes all 13
registration cases. Execute and compensate hosts now separate lifecycle notification from result
evaluation; new contract evidence covers their construction, arguments, and probe metadata.

The final Engineering Release build passes all 77 projects with zero warnings and errors. Both
format gates pass without changing any of 5,806 Engineering or 5,384 Unit files. The 23 native
Unit/Architecture hosts pass 6,250/6,250 tests with no failure or skip, including 301 architecture
and 3,283 Core-host cases. Fresh Core-host coverage is 78.3472% line and 70.6344% branch; Courier is
88.6212% line and 75.3304% branch, and none of its 617 methods exceeds CRAP 30. Package validation
passes 18 journeys, 31 freshly packed packages, three isolated provider-testing consumers, and all
30 runtime APIs. The intentional 19,030-line packed contract has SHA-256
`b81db7838a57f4205d2c10687643a8ce8853f85c6de4a96b6a07f843631b7d51`.

The protected `review/` and `TestResults/` trees remain unchanged and unstaged. The overall A+ goal
continues with the remaining complete source owners and the final repository-wide audit.

## Iteration 109 Core Caching plan

Iteration 109 reviews the complete `src/ViciOne.ServiceBus/Caching` owner: 17 production files and
1,691 lines. Every file and comment will be read manually before any product edit. No source,
comment, or test generator is permitted; static analysis and coverage remain read-only completeness
aids.

Caching is an internal Core capability and remains beneath `src/ViciOne.ServiceBus/Caching`.
`Implementation/` is accepted only where it represents a coherent non-public namespace and every
path, filename, namespace, and primary type agrees. The review covers key identity, concurrent
creation, expiration modes, disposal ownership, observer isolation, statistics, cancellation,
failure fan-out, and all public parameters.

### Iteration 109 requirement-to-test map

| Requirement | Planned evidence |
|---|---|
| Every Caching source file and comment is necessary, current, and manually understood | complete 17-file reread and final inventory |
| Paths, namespaces, filenames, and types express one Core owner | source-navigation architecture rules plus manual ownership map |
| Concurrent callers create at most one owned resource per effective key | deterministic contention, shared-result, cancellation, and factory-failure evidence |
| Expiration modes cannot dispose an active resource or retain an expired idle resource | exact virtual-time boundaries and usage-state transitions |
| Removal and cache disposal release each resource exactly once | direct lifecycle and idempotency assertions across success and failure paths |
| Observer failures cannot corrupt cache state or hide primary failures | exception-order and state-invariant tests |
| Public options, statistics, factories, and index contracts are minimal and Greenfield coherent | API review, direct parameter tests, packed API comparison |
| No feature is lost | focused baseline, full Core coverage host, all hermetic hosts, Engineering build, package/API and hygiene gates |

### Iteration 109 validation

1. Record the unchanged focused baseline and source-to-test map.
2. Read all 17 production files and comments before changing product code.
3. Rank concrete semantic and API findings with fresh coverage and CRAP evidence.
4. Add red-first tests only for independently justified contracts, then make the smallest coherent
   production correction.
5. Kill meaningful one-cause counterchanges and restore the accepted implementation after each run.
6. Audit every changed test for assertions, determinism, failure sensitivity, and requirement
   projection.
7. Run focused coverage, all 77 Engineering projects, all 23 Unit/Architecture hosts, both format
   gates, package/API verification, source hygiene, and Git checks.
8. Commit, annotate Iteration 109, push without force, and verify branch and peeled tag hashes.

### Iteration 109 completion

All 17 Caching production files and the final 1,701 lines were read manually, including every
comment. `Caching/` remains an internal Core capability, while `Caching/Implementation` contains
only non-public helpers in its matching namespace. Independent assemblies remain sibling projects
under `src`; external providers remain grouped under `Persistence/`, `Scheduling/`, and
`Transports/`. The build, architecture, package, and public-surface gates confirm those boundaries.

A custom usage-event add accessor could register the cache callback and then throw, leaving the
callback retained without cache ownership metadata. Admission now attempts a compensating
unsubscribe and isolates both the original subscription failure and any compensation failure from
committed cache state. Direct tests also close pending-capacity backpressure and caller-cancellation
ownership for `AddAsync`, synchronous-only `IDisposable` release, null index keys, the empty hit
ratio, and the externally observable canceled state of clear-invalidated creation.

Five new tests and one strengthened lifecycle assertion pass. Three isolated one-cause
counterchanges were killed and fully restored: removal of subscription compensation, removal of
caller cancellation from the pending-capacity wait, and removal of synchronous disposal. The final
focused suite passes 105/105. Caching coverage is 94.31% line (646/685) and 90.23% branch (231/256)
over 97 methods, with no CRAP score above 30. The accepted artifact is
`/private/tmp/vsb-iteration109-caching-final.cobertura.xml`, SHA-256
`16ea8775fb8206dcdeb4895df17568f5324391e8804363fd2c6cd70802741e20`.

Both format gates pass. The final Engineering build passes all 77 projects with zero warnings and
errors. All 23 hermetic hosts pass 6,369/6,369 without failure or skip, and the final rebuilt Core
host passes 3,396/3,396. Package/API verification passes 18 journeys, 31 fresh packages, three
isolated provider consumers, and all 30 runtime APIs; the unchanged 18,879-line contract SHA-256 is
`ab7469f985f1e269c5cceb803c06cfdd27cfe19f9b4ca51eded8f6c97857f12f`.

Two redundant compiled `#nullable enable` directives were removed because nullable analysis is
already centrally enabled. The only remaining directive-shaped text is an intentional raw-string
Roslyn fixture that proves Release symbol evaluation; production source contains no directives.
Dummy, compatibility identity, SDK pinning, empty-directory, requirements, formatting, and Git
whitespace checks pass. Protected `review/` and `TestResults/` remain unchanged and unstaged. The
overall A+ goal continues with the next complete source owner and the final repository-wide audit.

## Iteration 101 JobService API and navigation plan

Iteration 101 starts from remotely verified commit
`32d689afab6f560723ffacba9e6c041a3b6d6120` and annotated tag
`servicebus-a-plus-remediation-iteration-100-2026-09-13`. The complete 170-file JobService owner was
manually read and behaviorally remediated in Iteration 93. This coherent follow-up rereads all 45
remaining unprefixed interface declarations, their comments, complete consumer closure, filenames,
and the project's namespace/path model.

| Requirement | Planned evidence |
| --- | --- |
| Apply one .NET interface convention | all public and internal JobService interfaces begin with `I` followed by an uppercase letter |
| Preserve every JobService feature | generic arity, variance, inheritance, attributes, members, message initialization, correlation, state-machine behavior, and serializers remain equivalent |
| Preserve consumer usability | all source, tests, samples, benchmarks, reflection identities, messages, package journeys, and packed API references use the new identities |
| Align source navigation | keep JobService as an independent sibling project; map its source folders to existing namespaces relative to an explicit `ViciOne.ServiceBus` root namespace |
| Keep comments accurate | reread and manually correct every affected declaration and reference comment; do not generate documentation |
| Reject regressions | red-first interface and namespace/folder rules, focused builds/tests, isolated counterchanges, full build/test/coverage/format/package and hygiene gates |

Semantic rename tooling may update symbol references only after each declaration has been manually
classified. It must not generate behavior or documentation, and every resulting declaration,
filename, public contract, comment reference, and consumer diff will be inspected. No compatibility
alias is retained in the permanent Greenfield fork.

### Iteration 101 completion

The complete JobService owner remains an independent sibling capability project, while all 170
production files now mirror their declared namespaces beneath the explicit `ViciOne.ServiceBus`
root namespace. All 45 JobService interfaces use the .NET `I` prefix, their filenames match their
types, and no legacy alias remains. The two JobService-specific exceptions moved from the project
root into the `ViciOne.ServiceBus.JobService` namespace; the root now contains infrastructure only.

Two red-first architecture requirements protect interface naming and namespace-relative folder
navigation. Existing root-layout and public-documentation rules exposed and closed two secondary
gaps during the full-host run. The assertion audit finds two meaningful collection assertions and
no empty, trivial, self-referential, skipped, or timing-dependent test. Four substantive observed
counterchanges were killed; semantic-only style transformations required no artificial mutation.

The final Engineering build passes all 77 projects with zero warnings and errors. The complete
Unit/Architecture solution passes 6,243/6,243 with no failures or skips, including 299 architecture
cases. Both full format gates, JSON, whitespace, source-hygiene, API-identity, and directory checks
pass. Fresh coverage is 75.3255% line and 68.0424% branch overall; JobService is 95.6189% line and
89.7257% branch. Package validation passes 18 journeys, 31 fresh packages, three isolated provider
testing consumers, and all 30 runtime APIs. The 19,030-line contract has SHA-256
`f12d21461b1d4403c5ebed180f1c00d43a9a24b672745e0d3739452600091423`.

The overall A+ goal remains active for the remaining complete source owners and the final
repository-wide completion audit.

## Iteration 100 Futures interface contract plan

Iteration 100 starts from remotely verified commit
`045284d49168fac7f9ab0068453d81473bb2b150` and annotated tag
`servicebus-a-plus-remediation-iteration-99-2026-09-13`. The complete Futures owner was manually
read and remediated in Iteration 88. This bounded follow-up applies the subsequently adopted .NET
interface naming rule to its sole remaining violation and rereads the declaration, behavior, direct
consumer, tests, comments, filename, and packed contract.

| Requirement | Planned evidence |
| --- | --- |
| Apply the .NET interface prefix | red-first owner rule reports the exact remaining interface and stays green after correction |
| Preserve the durable query feature | the state-machine event retains correlation and all existing result-request behavior |
| Preserve discoverability | the filename, type name, generic parameter, constraint, XML documentation, and consumer identity remain aligned |
| Close the complete consumer surface | source, tests, package API, and isolated developer journeys compile against the new identity |
| Demonstrate safety | focused architecture/Futures tests, full build and Unit/Architecture suite, API/package, format, and hygiene gates |

No source or documentation generator will be used. This is an intentional Greenfield breaking
rename, not a compatibility alias; both old identities must be absent at completion.

### Iteration 100 completion

The remaining Futures interface is normalized from `Get<TFuture>` to `IGet<TFuture>` without a
compatibility alias, and its correlated message inheritance and state-machine event contract are
preserved. The project remains an independent sibling of Core; its existing namespaces are now
reflected by `Configuration/` and `Futures/` source branches relative to the explicit
`ViciOne.ServiceBus` root namespace. A permanent red-first interface rule and a red-first
namespace/folder rule protect both decisions.

A full-suite observer race and one architecture test path coupled to the prior layout were exposed,
fixed, and independently rerun. The final Engineering build, 6,241-test Unit/Architecture solution,
format gates, coverage, package journeys, public API comparison, and hygiene checks pass. Futures
coverage is 90.4990% line and 85.4839% branch. The packed 19,030-line contract has SHA-256
`a96d93cc091d97174baceb59fe5230228734c2b98a676431521e70a329cce9fa`.

The overall A+ goal remains active for the remaining complete source owners and the final
repository-wide completion audit.

## Iteration 107 Core Advanced API and ownership plan

Iteration 107 reviews the complete `src/ViciOne.ServiceBus/Advanced` owner manually. The review
covers every production file and comment, the Core project boundary, all exported contracts and
parameters, runtime behavior, asynchronous naming, cancellation, disposal, concurrency, source
paths, filenames, namespaces, directly owning tests, requirement projections, and package API.
No source or comment generator is permitted.

The physical ownership rule is explicit. `src/ViciOne.ServiceBus` is the Core assembly directory,
not an umbrella for sibling packages. Independent capability assemblies remain direct children of
`src`; external persistence, scheduling, and transport integrations remain grouped by provider
family. Within Core, `Advanced/`, `Advanced/Middleware`, `Advanced/Registration`, and
`Advanced/Serialization` must express real API and namespace ownership rather than historical or
visual nesting.

### Iteration 107 requirement-to-test map

| Requirement | Evidence |
|---|---|
| Every Advanced source file and comment is current, necessary, and manually understood | complete file inventory, manual review record, final reread |
| Public Advanced APIs are intentional expert extension points and expose no replaceable implementation detail | exact public-surface review, packed API comparison, developer-consumer compilation |
| Paths, namespaces, filenames, and types express one coherent Core owner | architecture rules plus complete compile-ownership and source-navigation checks |
| Every retained public operation and parameter has direct behavioral evidence | source-to-test map, focused behavior tests, requirement projections, gap audit |
| Async names, behavior, cancellation, disposal, and concurrency are bidirectionally correct | repository async gate plus deterministic focused lifecycle and cancellation tests |
| Findings are causally test-protected | red-first tests where applicable and isolated one-cause counterchanges restored byte-for-byte |
| No feature is lost | full build, full Unit/Architecture profile, package journeys, provider consumers, and runtime API comparison |

### Iteration 107 validation

1. Establish the unchanged focused baseline and complete semantic source-to-test map.
2. Read and classify all 80 Advanced source files and every comment before editing product code.
3. Implement only confirmed Greenfield API, behavior, documentation, or ownership corrections.
4. Build and execute focused tests after every coherent correction group.
5. Audit assertions, test smells, source gaps, line/branch coverage, CRAP risk, and meaningful
   one-cause mutations for the final owner state.
6. Run the complete serial Release Engineering build, all hermetic Unit/Architecture hosts, both
   format gates, requirements, source-hygiene, package/API, empty-directory, and Git checks.
7. Freeze the candidate commit, perform the required read-only red-team audit against that exact
   commit, remediate any finding, then commit, annotate, push normally, and verify branch and tag
   hashes remotely.

### Iteration 107 completion

All 80 Advanced production files and 5,803 lines, including every source comment, were read and
classified manually. The Core assembly remains at `src/ViciOne.ServiceBus`; it is not a container
for sibling projects. Independent capability assemblies remain direct `src` children, provider
integrations remain grouped beneath `Persistence/`, `Scheduling/`, and `Transports/`, and the four
Advanced directory branches correctly mirror their API and namespace owners inside Core.

The public transaction contract is now `ITransactionContext`, without a compatibility alias. Its
filename, consumers, documentation, architecture rule, and packed identity agree. Dispatcher
nullable-flow metadata, caller cancellation identity, supervisor cancellation propagation,
established-log-context metrics binding, abstract JSON mappings, process-wide convention
documentation, and diagnostic Unicode handling were corrected. Redactor complexity was separated
into bounded-length, detection, and sanitization responsibilities without changing its contract.

Fifty changed or added test methods were reviewed individually. Every method has causal assertions;
there are no assertion-free, self-referential, swallowed-exception, skipped, random, sleeping, or
wall-clock-dependent cases. Nine meaningful single-cause counterchanges were executed separately;
all nine were killed by their owning tests and fully reverted. The supervisor counterchange exposed
a real cancellation race, which was corrected and then passed 20 isolated repetitions.

Fresh Advanced coverage passes 3,382 tests and records 98.7% line coverage (1,639/1,661) and 91.1%
branch coverage (574/630) over 373 methods, with no CRAP score above 30. The accepted artifact is
`/private/tmp/vsb-iteration107-final.cobertura.xml`, SHA-256
`0b1b7a780345c2727bcdabad6f2236e8008b008d7c6e9ef10b4cb64794a4ab69`.

The final Engineering build passes all 77 projects with zero warnings or errors, both full format
gates pass, and all 23 native hermetic hosts pass 6,355/6,355 without failure or skip. Package/API
verification passes twice with 18 developer journeys, 31 fresh packages, three isolated provider
testing consumers, and 30 runtime assemblies. The intentional 18,879-line packed contract SHA-256
is `ab7469f985f1e269c5cceb803c06cfdd27cfe19f9b4ca51eded8f6c97857f12f`.

Requirements, bidirectional async naming, source comments, preprocessor directives, dummy markers,
MassTransit identities, SDK pinning, empty directories, formatting, and Git whitespace are clean.
The protected `review/` and `TestResults/` trees remain unchanged and unstaged. The overall A+ goal
continues with the remaining complete source owners and the final whole-repository audit.

## Iteration 108 Core Batching runtime plan

Iteration 108 reviews the complete `src/ViciOne.ServiceBus/Batching` owner: eight production files
and 1,075 lines across `Batching.Contexts` and `Batching.Runtime`. Every file and comment is read
manually. Source and comment generation remain prohibited; static pairing and coverage tooling are
read-only completeness aids.

The project-boundary decision from Iteration 107 remains unchanged. Batching is an internal Core
capability and therefore belongs inside `src/ViciOne.ServiceBus/Batching`; its `Contexts/` and
`Runtime/` branches mirror the declared namespaces. It is not an independent provider adapter and
must not become a sibling project.

### Iteration 108 requirement-to-test map

| Requirement | Planned evidence |
|---|---|
| A failed batch admission must never remain buffered or be delivered later | deterministic timer-start failure test asserting exact admission failure, terminal state, cleanup, and absence of delivery |
| Every accepted message pipeline must receive the terminal failure selected by its batch | multi-message failure test asserting exception identity for earlier and failing admissions |
| Equal transport or timestamp ordering keys must have a deterministic admission-order tie break | ordered batch assertion with equal primary keys plus a controlled counterchange |
| Every ordering fallback source must be intentional | separate transport sequence, sent time, receive time, and `TimeProvider` cases |
| Cancellation callbacks must not deadlock the serialized collector under bounded backpressure | deterministic saturated-queue ownership test or a documented proof that no unsafe synchronous path remains |
| Collector disposal must stop admissions, await admitted work, flush once, and drain both executors | direct lifetime state-transition assertions plus existing end-to-end collector disposal tests |
| All retained code and comments must match their behavior and owner | final eight-file reread, architecture/format/hygiene gates, source-to-test map |
| No batch feature may be lost | 70-test focused baseline, full Core host, all hermetic hosts, Engineering build, package/API comparison |

### Iteration 108 validation

1. Preserve the 70/70 focused baseline and record the Roslyn source-to-test pairing.
2. Use fresh owner coverage and CRAP analysis to rank semantic gaps rather than chase percentages.
3. Write red-first tests only for independently justified lifecycle, ordering, or failure contracts.
4. Correct proven production defects and manually update affected comments.
5. Kill meaningful one-cause counterchanges and restore the accepted implementation after each run.
6. Audit every changed test for assertion quality, mutation sensitivity, determinism, and requirement
   projection.
7. Run focused coverage, the serial 77-project Engineering build, all 23 Unit/Architecture hosts,
   both format gates, package/API verification, source hygiene, and Git checks.
8. Commit, annotate Iteration 108, push without force, and verify branch and peeled tag hashes.

### Iteration 108 completion

All eight Batching production files and the final 1,150 source lines were read manually, including
every comment. `Batching/Contexts` and `Batching/Runtime` correctly express an internal Core
capability beneath `src/ViciOne.ServiceBus`; no project move or compatibility alias is needed.

Timer scheduling failures now terminate admission and every owned pipeline without retaining a
later-deliverable message. Distinct cleanup failures are preserved. Timer and cancellation
callbacks no longer synchronously enqueue behind their own bounded collector worker, equal ordering
keys preserve admission order, and all timestamp fallbacks are explicit. Direct lifetime tests
prove drain, flush-once, both-executor shutdown, and failure identity.

Nine permanent requirement cases and six killed counterchanges protect the corrected contracts.
The final focused suite passes 79/79. Fresh final Core coverage passes 3,391/3,391 and records
Batching at 96.2% line and 91.6% branch coverage over 80 methods with no CRAP score above 30. The
accepted artifact SHA-256 is
`a83efb60a9d774fa9879689c7fbece53f4eddc011df5bb7606e3ac6b9ee79b4b`.

The serial 77-project Engineering build has zero warnings and errors, both format gates pass, and
all 23 hermetic hosts pass 6,364/6,364 with no skip. The final run also includes the corrected
bidirectional Async identity and a causally stabilized scheduled-publish harness timeout policy.
Package/API verification passes 18 journeys, 31 fresh packages, three isolated provider consumers,
and 30 runtime APIs; the unchanged 18,879-line contract SHA-256 is
`ab7469f985f1e269c5cceb803c06cfdd27cfe19f9b4ca51eded8f6c97857f12f`.

Source hygiene, requirements, formatting, API identity, empty directories, and Git whitespace pass.
The protected `review/` and `TestResults/` trees remain unchanged and unstaged. The overall A+ goal
continues with the remaining complete source owners and the final repository-wide audit.

## Iteration 110 Core Clients plan

Iteration 110 reviews all 17 production files and 1,805 lines under
`src/ViciOne.ServiceBus/Clients`. Every source file and comment is read manually before any product
edit. Source, comment, and test generation remain prohibited; static analysis and coverage are
read-only completeness aids.

Clients is an internal Core capability and remains inside `src/ViciOne.ServiceBus/Clients`.
`Contexts/`, `Endpoints/`, and `Requests/` are accepted only where paths, namespaces, filenames,
primary types, and ownership match. The review covers factory lifetime, endpoint selection,
request/response matching, cancellation and timeout identity, temporary endpoint cleanup,
multi-response completion, callback isolation, metadata propagation, and every public parameter.

### Iteration 110 requirement-to-test map

| Requirement | Planned evidence |
|---|---|
| Every Clients source file and comment is necessary, current, and manually understood | complete 17-file reread and final inventory |
| Client folders and namespaces express one Core capability | manual ownership map plus architecture and packed-surface gates |
| Request completion selects exactly one terminal outcome | deterministic response, fault, timeout, cancellation, and stop-race tests |
| Multi-response handlers preserve type and identity | exact accepted, rejected, late, and duplicate response assertions |
| Factories and endpoints preserve caller configuration | complete overload, address, timeout, callback, and metadata evidence |
| Client-owned connections and temporary endpoints are released exactly once | explicit lifecycle, concurrent disposal, and failure-path assertions |
| Public members and parameters remain minimal Greenfield API | contract review, invalid-input matrix, async naming, and package comparison |
| No feature is lost | focused baseline, owner coverage/CRAP, full Core and hermetic profiles, build, format, package/API and hygiene gates |

### Iteration 110 validation

1. Record the unchanged focused baseline and current source-to-test ownership map.
2. Read every production file and comment before editing product code.
3. Compare state transitions and public parameters with existing tests and fresh owner risk data.
4. Add red-first tests only for independently justified behavior, ownership, or API findings.
5. Make the smallest coherent correction and update affected comments manually.
6. Kill meaningful one-cause counterchanges, restoring the accepted implementation after each run.
7. Audit all changed tests for assertion depth, determinism, bounded waits, and requirement identity.
8. Run focused coverage, both format gates, all Engineering projects and hermetic hosts, package/API,
   requirements, source hygiene, and Git checks.
9. Commit, annotate Iteration 110, push without force, and verify branch and peeled tag hashes.

### Iteration 110 completion

All 17 Clients production files and their final comments were manually read. The accepted ownership
model remains explicit: `src/ViciOne.ServiceBus` is the Core project, not an umbrella; Clients is a
Core capability below it, independent assemblies remain sibling projects, and external providers
remain grouped under `Persistence/`, `Scheduling/`, and `Transports/`.

Six request lifecycle defects are corrected: terminal fault/response races, multiple successful
response branches, send-before-fault-observer ordering, null fault and response connection handles,
and null sent messages. Factory disposal, every direct and scoped request creation shape, advanced
initialized multi-response calls, deadline-derived transport lifetime, and duplicate send-pipeline
timer ownership now have direct evidence.

Thirteen permanent requirement projections add 30 focused cases, taking Clients from 93/93 to
123/123. Seven controlled counterchanges were killed and restored, and three provider-result
contracts were independently red before correction. Fresh Clients coverage is 98.9831% line and
89.6739% branch over 137 methods, with zero CRAP scores above 30. The accepted Core artifact passes
3,426/3,426 tests and has SHA-256
`3fe785da97559080e7eef14bc4dab1f155ae8ce155c15b8423af847655d83ce6`.

Both format gates pass. The 77-project Release build has zero warnings and errors. All 23 hermetic
hosts pass 6,399/6,399 without a skip. Package/API verification passes 18 journeys, 31 packages,
three isolated provider-testing consumers, and all 30 runtime API assemblies; the unchanged
18,879-line API SHA-256 is
`ab7469f985f1e269c5cceb803c06cfdd27cfe19f9b4ca51eded8f6c97857f12f`.

Requirements, bidirectional Async naming, file/folder/namespace ownership, source hygiene, empty
directories, and Git whitespace pass. Protected trees remain unchanged and unstaged. The overall
A+ goal continues with the remaining complete source owners and final whole-repository audit.

## Iteration 111 Core Consumers plan

Iteration 111 reviews all nine production files and 448 lines under
`src/ViciOne.ServiceBus/Consumers`. All source and comments are read manually. No source, test, or
comment generator is permitted; static pairing and coverage remain read-only completeness aids.

Consumers is an internal Core capability and remains below `src/ViciOne.ServiceBus/Consumers`.
Its `Contexts/`, `Conventions/`, and `Metadata/` branches match concrete namespace ownership. The
review covers per-delivery consumer creation and release, caller-owned instances, cleanup failure
identity, context and payload projection, process-wide convention snapshots, versioned metadata,
registration exclusion, runtime-type classification, probes, every public parameter, and
asynchronous naming.

### Iteration 111 requirement-to-test map

| Requirement | Planned evidence |
|---|---|
| Owned consumer factories must release exactly once without losing operation or cleanup failures | deterministic success, pipeline failure, disposal failure, and dual-failure identity cases |
| Caller-owned instances must never be released by the factory | success and failure lifetime assertions |
| Object and delegate factories must reject null or incompatible results before dispatch | exact type, message, dispatch-count, and boundary assertions |
| Consumer contexts must preserve message, consumer, inherited payloads, and local isolation | proxy and scoped-context identity tests including invalid inputs |
| Convention snapshots must be immutable, ordered, versioned, and stable when no mutation occurs | duplicate registration, successful/missing removal, concurrent snapshot identity, and precedence tests |
| Registration metadata must classify core consumers, definitions, excluded capabilities, and unrelated types exactly | direct positive and negative type matrix |
| Every file, type, namespace, comment, and parameter must express one Core owner | manual reread, architecture, async, format, API, and hygiene gates |
| No feature may be lost | 119-case baseline, focused and Core coverage, full hermetic suite, Engineering build, package/API comparison |

### Iteration 111 validation

1. Preserve the 119/119 Consumer namespace baseline and record direct source-to-test ownership.
2. Compare every state and failure path with existing tests and the fresh Core coverage artifact.
3. Add red-first tests for independently justified lifetime or metadata gaps.
4. Make the smallest coherent implementation correction and manually update affected comments.
5. Kill meaningful one-cause counterchanges and restore the accepted implementation each time.
6. Audit all changed tests for exact, causal assertions, determinism, bounded waits, and requirement
   identity.
7. Run focused coverage, both format gates, the 77-project build, all 23 hermetic hosts, package/API,
   requirements, source hygiene, empty-directory, and Git checks.
8. Commit, annotate Iteration 111, push without force, and verify remote branch and peeled tag hashes.

### Iteration 111 completion

All ten final Consumers production files and 539 lines, including every final comment, were read
manually. Consumers remains a Core capability under `src/ViciOne.ServiceBus/Consumers`; its three
subfolders continue to match namespace and runtime ownership. Independent assemblies remain sibling
projects and external providers remain grouped under `Persistence/`, `Scheduling/`, and
`Transports/`.

Owned factory release now preserves exact operation and cleanup failures, singly or together. Every
invalid custom convention-provider result fails at its owning boundary, and ordered metadata
replacement, no-op version identity, registration exclusion, probe identity, and context boundaries
have direct evidence. Eight requirement projections add 17 focused cases, taking Consumers from
119/119 to 136/136. Seven cases were red before correction, and three controlled counterchanges were
killed and restored.

Fresh Consumers coverage is 100% line and branch over 34 methods, with zero CRAP scores above 30.
The complete Core artifact passes 3,443/3,443 and has SHA-256
`91dde737706e9d409aac01328c607314b0b57ee6c0decf31458188be462abeff`.

Both format gates pass. The 77-project Release build has zero warnings and errors. All 23 hermetic
hosts pass 6,416/6,416 without a skip. Package/API verification passes 18 journeys, 31 packages,
three isolated provider-testing consumers, and all 30 runtime APIs; the unchanged 18,879-line API
SHA-256 is `ab7469f985f1e269c5cceb803c06cfdd27cfe19f9b4ca51eded8f6c97857f12f`.

Requirements, bidirectional Async naming, source and project architecture, directives, dummy and
legacy markers, SDK pinning, empty directories, formatting, and Git whitespace are clean. Protected
trees remain unchanged and unstaged. The overall A+ goal continues with the remaining complete
source owners and final repository-wide audit.

## Iteration 112 Core Context plan

Iteration 112 reviews all 15 production files and 2,191 lines below
`src/ViciOne.ServiceBus/Context`. All source and comments are read manually. No source, test, or
comment generator is permitted; coverage and source-to-test pairing are read-only completeness aids.

Context remains an internal Core capability. `Activities/` and `Consumption/` are accepted as
physical responsibility groups whose public types intentionally share the concise
`ViciOne.ServiceBus.Context` namespace. The review covers message and metadata projection, local
payload precedence and isolation, response/send/publish task ownership, endpoint resolution,
deserializer completion, unavailable-context failure identity, observer and fault notification,
activity results, every public parameter, and asynchronous naming.

### Iteration 112 requirement-to-test map

| Requirement | Planned evidence |
|---|---|
| Every context file, type, namespace, folder, and comment has one clear Core owner | complete manual reread plus architecture and API gates |
| A typed message view preserves its explicit message and authoritative source contracts | exact and assignable materialization identity tests |
| Response operations become consume-owned before asynchronous preparation can escape | deterministic blocked-resolution and task-identity tests |
| Proxies and scopes preserve metadata, cancellation, observers, messages, and payload precedence | direct forwarding and local/source payload matrices |
| Deserialization completion owns every admitted task and exact cancellation outcome | pending, success, failure, cancellation, and null-boundary tests |
| Response and fault notification preserve routing, duration, type, exception, and token identity | direct positive, cancellation, and fault-generation tests |
| Invalid collaborators and provider results fail at their owning boundary | constructor, method, null-task, and null-result cases |
| No feature may be lost | 35-case baseline, owner coverage/CRAP, full Core and hermetic profiles, build, format, package/API comparison |

### Iteration 112 validation

1. Preserve the 35/35 Context namespace baseline and direct source-to-test ownership map.
2. Compare all state, forwarding, failure, and lifetime paths with existing tests and fresh coverage.
3. Add red-first tests only for independently justified behavior or boundary defects.
4. Apply the smallest coherent correction and update every affected source comment manually.
5. Kill meaningful isolated counterchanges and restore the accepted implementation after each run.
6. Audit changed tests for exact causal assertions, determinism, bounded waits, and requirement identity.
7. Run focused and full Core coverage, format, the 77-project build, all 23 hermetic hosts,
   package/API, requirements, async/source architecture, hygiene, empty-directory, and Git gates.
8. Commit, annotate Iteration 112, push without force, and verify remote branch and peeled tag hashes.

### Iteration 112 completion

All 15 final Context production files and 2,181 source lines, including every source comment, were
read manually. `Activities/` and `Consumption/` remain coherent physical groups inside the Core
project and intentionally share `ViciOne.ServiceBus.Context`; independent assemblies remain `src`
siblings and external providers remain grouped under `Persistence/`, `Scheduling/`, and
`Transports/`.

Initialized responses now become consume-owned before endpoint resolution, all response shapes own
each distinct task exactly once, invalid endpoint-provider and proxy lookup results fail at their
owning boundaries, and projection, payload, notification, deserialization, scope, and parameter
contracts have direct evidence. The focused profile grows from 35 to 96 cases. Original red evidence
confirmed the four defect families, and three isolated counterchanges were killed and restored.

Fresh Context coverage is 100% executable lines (527/527) and 93.75% branches (120/128), across 294
methods with no CRAP score above 30. The accepted focused artifact SHA-256 is
`f8d8c82b050dc8003ca7411080c64299a05a991cc8df689189b6a31f04e5cd92`; the complete Core coverage
run passes 3,504/3,504 with SHA-256
`51ad6d890e9c31ce7652c931f77fefbae7c0c0aeef58edeef33a44729d247820`.

Both format gates pass. The 77-project Release build has zero warnings and errors. All 23 hermetic
hosts pass 6,477/6,477 with no skip, including bidirectional Async naming, comments, directives,
Greenfield API, and source-file/folder ownership. Package/API verification passes 18 journeys, 31
fresh packages, three isolated provider consumers, and all 30 runtime APIs; the unchanged
18,879-line API SHA-256 is
`ab7469f985f1e269c5cceb803c06cfdd27cfe19f9b4ca51eded8f6c97857f12f`.

Requirements, dummy and compatibility markers, SDK pinning, empty directories, formatting, and Git
whitespace are clean. The explicit `net10.0` target is the platform contract. The repository
`global.json` selects Microsoft Testing Platform only; it contains no `sdk` block or CLI SDK version
pin. Protected trees remain unchanged and unstaged. The overall A+ goal
continues with the remaining complete source owners and the final repository-wide audit.

## Iteration 113 Core InMemoryTransport plan

Iteration 113 reviews the complete 66-file, 3,846-line built-in InMemory owner: the 56 implementation
files below `src/ViciOne.ServiceBus/InMemoryTransport`, the nine public provider-contract files below
`src/ViciOne.ServiceBus/Providers/Transports/InMemory`, and the public selection entry point below
`src/ViciOne.ServiceBus/Configuration/InMemoryTransport`. Every file and comment has been read
manually before a product edit. No source, test, or comment generator is permitted; coverage and
source-to-test pairing remain read-only completeness aids.

InMemoryTransport remains a built-in Core capability. Its five internal folders express addressing,
configuration, process-local durable-send completion, runtime delivery, and topology ownership.
External transport integrations remain independent projects beneath `src/Transports`.

### Iteration 113 requirement-to-test map

| Requirement | Planned evidence |
|---|---|
| Host and endpoint addresses are canonical and cannot cross a provider boundary | complete component, query, virtual-host, routing-type, equality, and invalid-input matrices |
| Topology mutation declares and binds each intended entity exactly once | direct publish/consume specification, implemented-contract, exclusion, and validation assertions |
| Configuration materializes isolated endpoints without losing shared policies | root/child configuration identity, callback order, endpoint definition, observer, and validation tests |
| Send and receive envelopes preserve immutable body, headers, routing, delay, and durable state | exact ownership, copy, round-trip, unsupported-value, and mutation-isolation tests |
| Runtime start, delivery, stop, and dynamic endpoints have one lifecycle owner | deterministic concurrency, cancellation, fault, disconnect, drain, and retry evidence |
| Logical delays are ordered, bounded, cancellable, and disposed exactly once | fake-time boundary and registration-race tests without sleeping |
| Durable dispatch retires an intent only after successful logical consumption | contract resolution, send acceptance, consumer completion, failure, cancellation, and invalid-input tests |
| Every collaborator and provider result fails at its owning boundary | constructor, method, null-task, null-result, wrong-type, and pre-cancellation cases |
| No feature is lost | 80-case baseline, focused and Core coverage, full hermetic suite, build, format, package/API, requirements, and hygiene gates |

### Iteration 113 validation

1. Preserve the 80/80 focused baseline and record source-to-test ownership and fresh risk data.
2. Compare every address, topology, configuration, send, receive, delay, lifecycle, and durable path
   with existing direct tests.
3. Add red-first tests only for independently justified behavior or boundary defects.
4. Apply the smallest coherent correction and manually update every affected comment.
5. Kill meaningful isolated counterchanges and restore the accepted implementation after each run.
6. Audit changed tests for exact causal assertions, determinism, bounded synchronization, and
   requirement identity.
7. Run focused and complete Core coverage, both format gates, the serial 77-project build, all 23
   hermetic hosts, package/API, requirements, async/source architecture, hygiene, empty-directory,
   and Git gates.
8. Commit, annotate Iteration 113, push without force, and verify remote branch and peeled tag hashes.

### Iteration 113 completion

All 66 final production files and 3,846 physical lines in the complete built-in InMemory owner were
read manually, including every source comment. The implementation remains a process-local Core
capability under `src/ViciOne.ServiceBus/InMemoryTransport`; its public provider contracts and
selection API remain under the matching Core namespaces. Independently packaged integrations stay
as sibling projects grouped beneath `Persistence`, `Scheduling`, and `Transports`. No product C#
file belongs directly under the repository `src` root.

Canonical address ownership now rejects ambiguous raw multi-segment virtual hosts and unsupported
short-address URI components while preserving escaped host and entity identities. Endpoint address
materialization uses the final configured host, publish topology rejects undefined exchange types
at assignment, and moved messages retain their complete MIME content type. Durable dispatch rejects
missing messages, unknown or null contract resolutions, null endpoint tasks, and null endpoints at
their owning boundary; pre-cancellation preserves the caller token and skips collaborators.

Eighteen new requirement projections add 24 focused cases. Direct tests also close custom-address
and typed-bus delay-provider ownership, provider-neutral endpoint callbacks, public receive binding,
runtime fabric ownership, namespace discovery, zero logical delay, all new entry-point parameters,
and dead-letter metadata. The focused profile grows from 80 to 104 cases. Seven simultaneous
counterchanges produced exactly 15 expected failures while 34 unrelated cases remained green; the
accepted implementation was restored and rebuilt. Changed tests contain causal assertions, bounded
waits, deterministic logical time, and no sleeps, random input, skips, assertion-free paths, or
wall-clock assumptions.

Final focused coverage passes 104/104 and records 90.4889% line (1,018/1,125) and 76.0101% branch
(301/396) reachability across 272 compiler method records, with maximum CRAP 18 and none above 30.
Its artifact is `/private/tmp/vsb-iteration113-inmemory-final3.cobertura.xml`, SHA-256
`5388c5139c95c229dc00315fa7a8ca902085fdf75bc36537446a3c3409176de8`. Complete Core coverage passes
3,528/3,528; the InMemory owner reaches 96.4444% line (1,085/1,125) and 78.7879% branch (312/396).
The complete artifact is `/private/tmp/vsb-iteration113-core-final3.cobertura.xml`, SHA-256
`b0f878be0ebb78f4ad4c48126e78fde891ef751fc8996a59b634a8d1302ed7a9`.

Both format gates pass. The serial Engineering Release build passes 77 projects with zero warnings
and errors. All 23 hermetic hosts pass 6,501/6,501 with no failure or skip, and the separate
architecture host passes 307/307. Package/API verification passes 18 journeys, 31 fresh packages,
three isolated provider-testing consumers, and all 30 runtime APIs; the unchanged 18,879-line API
SHA-256 is `ab7469f985f1e269c5cceb803c06cfdd27cfe19f9b4ca51eded8f6c97857f12f`.

Requirements, bidirectional Async naming, Greenfield API rules, comments, directives, filenames,
folders, namespaces, dummy and legacy markers, SDK-version pinning, empty directories, formatting,
and Git whitespace pass. `global.json` contains only the Microsoft Testing Platform runner selection
and no `sdk` version. The sole source `NotImplementedException` reference is the intentional
non-retryable exception classification case. Protected `review/` and `TestResults/` remain unchanged
and unstaged. The overall A+ goal continues with the remaining complete source owners and final
repository-wide audit.

## Iteration 114 Core Initializers plan

Iteration 114 reviews the complete 87-file, 7,571-line Core Initializers owner below
`src/ViciOne.ServiceBus/Initializers` and its 46-file, 7,384-line direct test owner. Every production
and test file and every source comment is read manually before correction. No source, test, comment,
or structural generator is permitted; coverage and static scans only route and verify the manual
review.

The directory model follows assembly ownership. `src/ViciOne.ServiceBus` is the project root of the
Core assembly, not a solution-wide container. Independent assemblies therefore remain sibling
projects directly beneath `src`, while optional provider families remain grouped beneath
`Persistence`, `Scheduling`, and `Transports`. Initializers are a Core runtime capability and belong
inside the Core project. No product C# file belongs directly in the repository `src` root.

### Iteration 114 requirement-to-test map

| Requirement | Planned evidence |
|---|---|
| Every property and header initializer returns a valid task | direct null-task ownership tests with exact failure text and downstream call counts |
| Initializer failures and cancellation retain their exact identity | existing synchronous, asynchronous, completed-task, pending-task, and token matrices |
| Convention entry points require valid property metadata | exact parameter-name assertions for all three convention entry points |
| Converter discovery preserves enum, nullable, named-value, and registered-converter semantics | complete converter matrices plus focused and Core regression suites |
| Converter resolution remains understandable and below the accepted risk threshold | manual decomposition and fresh per-method CRAP analysis |
| Comments describe only current behavior | manual comment review and correction after understanding each implementation |
| Folder and file ownership match types, namespaces, and assemblies | manual 87-file structural review plus repository architecture gates |
| No feature or public API is lost | focused and Core coverage, serial build, all hermetic hosts, package consumers, and packed API baseline |

### Iteration 114 validation

1. Preserve the correct 178/178 focused Initializers baseline and record fresh coverage and risk.
2. Review every implementation and direct test against its conversions, conventions, ownership,
   cancellation, concurrency, and failure boundaries.
3. Add red-first tests only for independently justified contract gaps.
4. Apply the smallest coherent correction and manually update affected comments and requirement
   projections.
5. Kill a meaningful isolated counterchange and restore the accepted source byte-for-byte.
6. Re-run focused and complete Core coverage and bind their artifacts to the final source names.
7. Run both format gates, the serial 77-project build, all 23 hermetic hosts, package/API validation,
   bidirectional Async naming, requirements, structure, directives, comments, hygiene, empty-folder,
   and Git gates.
8. Commit, annotate Iteration 114, push without force, and verify remote branch and peeled tag hashes.

### Iteration 114 completion

All 87 final production files and 7,593 physical lines in Core Initializers and all 46 direct test
files and 7,452 test lines were read manually. Their files, namespaces, folders, and assembly
ownership are coherent. Initializers remain inside the Core project; independent assemblies remain
siblings under `src`, and optional integrations remain grouped by provider family. No product C#
file is stored directly in the repository `src` root.

Property and header initializer null tasks now fail at the owning boundary with explicit,
deterministic contract errors; a header failure prevents the downstream send pipe. Convention entry
points have direct required-metadata coverage. Converter discovery was decomposed by cache lookup,
direct conversion, nullable-result conversion, and nullable-source conversion without changing its
ordering or supported semantics. The DateTime converter summary now accurately includes invariant
text and signed Unix-millisecond conversions instead of claiming every output is a UTC instant.

The two new contract cases failed red against the original implementation while 19 related cases
passed. Replacing the owned null-task rejection with `Task.CompletedTask` caused exactly the same two
cases to fail while the same 19 unrelated cases remained green; the accepted source was restored.
The bidirectional Async gate then identified the newly introduced task-returning private helper, and
its name and call sites were corrected before the definitive validation.

Final focused coverage passes 181/181 and reaches 97.6589% line (2,336/2,392) and 90.3448% branch
(1,048/1,160) coverage across 494 compiler method records. The artifact is
`/private/tmp/vsb-iteration114-initializers-final3.cobertura.xml`, SHA-256
`621c6186e8f9e4412d4bdfa2a33395a710cf697f5aee62947790454be4233509`. Complete Core coverage passes
3,531/3,531, records 77.0558% repository line (48,905/63,467) and 69.7433% branch
(17,032/24,421) coverage, and raises the Initializers owner to 97.7007% line (2,337/2,392) and
90.4310% branch (1,049/1,160). Its artifact is
`/private/tmp/vsb-iteration114-core-final3.cobertura.xml`, SHA-256
`0402d07f5a2dfe26c6e63875e835575611ea4e7d55d552b13e0089b33e1e4b40`. Maximum owner CRAP is 28,
with no score above 30.

Both format gates pass. The final serial Engineering Release build passes all 77 projects with zero
warnings and errors. All 23 hermetic Unit and Architecture hosts pass 6,504/6,504 with no failure or
skip. Package/API verification passes 18 journeys, 31 freshly packed packages, three isolated
provider-testing consumers, and all 30 runtime APIs; the unchanged 18,879-line API SHA-256 is
`ab7469f985f1e269c5cceb803c06cfdd27cfe19f9b4ca51eded8f6c97857f12f`.

Requirements, bidirectional Async naming, comments, directives, filenames, folders, namespaces,
dummy and legacy markers, SDK pinning, empty directories, formatting, and Git whitespace pass.
`global.json` contains no SDK version. The sole source `NotImplementedException` reference is the
intentional non-retryable exception classification case. Protected `review/` and `TestResults/`
remain unchanged and unstaged. The overall A+ goal continues with the remaining complete source
owners and final repository-wide audit.

## Iteration 115 Core Reflection and JobService span ownership plan

Iteration 115 reviews the complete 12-file, 938-line Core owner below `Internals/Reflection` and
`Internals/Extensions`, its direct reflection tests, and the only runtime consumer of the generic
span splitter. Every production file, direct test, and source comment is read manually. No source,
test, comment, or structure generator is permitted.

The repository hierarchy follows assembly ownership. `src/ViciOne.ServiceBus` is the Core project
root, not an umbrella for all ServiceBus projects. Independent assemblies remain direct siblings
under `src`; optional integrations remain grouped beneath `Persistence`, `Scheduling`, and
`Transports`. A helper used only by JobService belongs in that assembly rather than in Core.

### Iteration 115 requirement-to-test map

| Requirement | Planned evidence |
|---|---|
| Dynamic message and bus implementations reject unsupported contracts before emission | exact null, concrete, open-generic, behavior, property-shape, and bus-marker tests |
| Emitted types are stable and safe under concurrent use | concrete type identity, collectible assembly, constructor/property shape, and parallel cache tests |
| Runtime property access accepts only compatible instance metadata | unrelated, indexed, static, missing-accessor, type-mismatch, and null-instance tests |
| Property caches have one deterministic case-insensitive contract | name boundaries, missing/optional lookup, metadata ownership, identity, and executable access tests |
| Cron list tokenization preserves empty and trailing entries for validation | exact double-comma, separated-comma, and trailing-comma failures |
| Files, namespaces, folders, and assemblies have explicit owners | repository architecture rule for all final Reflection files and the JobService span splitter |
| No feature or public API is lost | focused and Core coverage, serial build, all hermetic hosts, package consumers, and packed API baseline |

### Iteration 115 validation

1. Preserve the correct 23/23 direct Reflection baseline and record fresh owner coverage and risk.
2. Read all implementation and direct-test paths against emission, caching, metadata ownership,
   concurrency, reflection fallback, failure identity, and Cron tokenization.
3. Add red-first tests only for independently justified boundary or behavior defects.
4. Remove dead abstractions and move single-consumer behavior to its owning assembly without
   changing the public API.
5. Kill a bounded group of meaningful counterchanges and restore every accepted source byte-for-byte.
6. Audit all changed tests for causal assertions, determinism, isolation, and test anti-patterns.
7. Run focused and complete Core coverage, both format gates, the serial Engineering build, all 23
   hermetic hosts, package/API, requirements, Async/source architecture, hygiene, empty-folder, and
   Git gates.
8. Commit, annotate Iteration 115, push without force, and verify remote branch and peeled tag hashes.

### Iteration 115 completion

All 12 original Core files and 938 source lines, all final production files, the direct tests, and
every affected source comment were read manually. The final Reflection owner contains eight files
and 817 lines beneath `src/ViciOne.ServiceBus/Internals/Reflection`. Its namespace is now explicit.
The sole live span-split consumer owns an 88-line implementation under
`src/ViciOne.ServiceBus.JobService/JobService/Scheduling`; the two unused trim helpers and three
redundant one-implementation cache/builder interfaces are removed. No public API is changed.

Dynamic bus construction has direct valid and invalid contract coverage and serializes emission on
each collectible module. Runtime property access rejects unrelated, indexed, static, missing,
mismatched, and null inputs at the owning boundary. Read and write caches share deterministic
case-insensitive lookup semantics and validate `PropertyInfo` ownership. Span splitting has an
explicit completion state, so trailing empty Cron tokens are retained and rejected correctly.

The original implementation produced seven failures among 37 Reflection cases and two failures
among 136 Cron parsing cases; the original structure produced one failure among 23 architecture
cases. The final focused profiles pass 42/42, 136/136, and 23/23 respectively. Four simultaneous
counterchanges produced exactly four failures among 3,552 Core tests: both trailing-list cases, the
null runtime-instance case, and unrelated property metadata. Every mutation was restored to its
recorded SHA-256 before final validation.

Focused Reflection coverage reaches 95.7393% line (382/399) and 92.8571% branch (169/182) coverage
across 80 compiler method records, with maximum CRAP 30 and none above 30. The artifact is
`/private/tmp/vsb-iteration115-internals-final1.cobertura.xml`, SHA-256
`c7d1107f0d9a8b61077b69916e662122bce10238d8fc4beb93c45e40496b7c81`. Focused Cron coverage gives
the new span splitter 100% line (29/29) and branch (6/6) coverage; its artifact SHA-256 is
`3b780494b7ee9c1d133696bd20b257c0f9cd7396fe5ed4c3a14939f9fc99980c`.

Complete Core coverage passes 3,552/3,552 and records 77.1131% repository line (48,956/63,486) and
69.8889% branch (17,055/24,403) coverage. In that run, executable Reflection sources reach 96.4194%
line (377/391) and 96.7033% branch (176/182), while the span splitter remains at 100% line and branch.
The complete artifact is `/private/tmp/vsb-iteration115-core-final.cobertura.xml`, SHA-256
`48eea3d2bbf0e7942d565b84557264105f2fae0a4243051a7ddb9db522bbccd8`.

Both format gates pass. The serial 77-project Engineering Release build has zero warnings and
errors. All 23 hermetic Unit and Architecture hosts pass 6,526/6,526 with no failure or skip.
Package/API validation passes 18 journeys, 31 fresh packages, three isolated provider-testing
consumers, and all 30 runtime APIs; the unchanged 18,879-line API SHA-256 is
`ab7469f985f1e269c5cceb803c06cfdd27cfe19f9b4ca51eded8f6c97857f12f`.

The 16 new or changed test methods contain causal assertions and no sleeps, wall-clock or random
input, skips, broad catches, swallowed failures, shared mutable fixtures, assertion-free paths, or
coverage-only assertions. Requirements are unique and complete. Bidirectional Async naming,
comments, directives, filenames, folders, namespaces, dummy and legacy markers, SDK pinning, empty
directories, formatting, and Git whitespace pass. Protected `review/` and `TestResults/` remain
unchanged and unstaged. The overall A+ goal continues with the remaining complete source owners and
the final repository-wide audit.

## Iteration 116 Core Logging plan and completion

Iteration 116 reviews all 13 Core Logging production files and every source comment manually. The
owner starts with 1,373 physical source lines and finishes with 1,341 after dead internal tracing
machinery is removed. Its `Diagnostics/`, `Internal/`, and `Monitoring/` folders match runtime
responsibilities inside the Core assembly. The related Azure Service Bus header projection remains
in its independent transport package under `src/Transports`; no C# product file is stored directly
in the repository `src` root.

The review covers structured log values and exact exception identity, caller-owned logger lifetime,
distributed trace extraction and propagation, receive-parent modes, persistent outbox continuity,
message-body metrics, listener-failure isolation, and the Azure diagnostic-header compatibility
boundary. Every new behavior is represented in the durable requirement projection. No generator is
used for source, tests, comments, or structure.

Red-first tests expose two defects: an extracted transport parent loses its remote identity when an
unrelated ambient activity exists, and the public Azure header provider accepts a null SDK message.
The accepted implementation corrects both at their owning boundary. An unused transport-tag
parameter and its dead helper are removed; redundant manual trace-state copying is also removed
after end-to-end evidence confirms that `ActivitySource` inherits W3C trace state correctly.

Five simultaneous controlled counterchanges alter exception forwarding, exact body length, the
Link parent mode, remote-parent identity, and persistent-outbox delivery kind. Exactly five causal
tests fail while 16 related tests remain green; the accepted sources are then restored. Focused
Logging and Monitoring profiles pass 21/21 and 45/45. All 10 new or changed test methods, comprising
12 executed cases, pass manual anti-pattern review with exact assertions and no sleep, wall-clock
dependency, random input, skip, broad catch, swallowed failure, shared mutable fixture, or
coverage-only assertion.

The final Core coverage run passes 3,560/3,560. Core Logging reaches 95.7211% line (604/631) and
100% branch (133/133) coverage across 116 compiler method records, with maximum CRAP 28 and none
above 30. Overall loaded product reachability is 77.1372% line (48,959/63,470) and 69.9438% branch
(17,060/24,391). The accepted artifact is
`/private/tmp/vsb-iteration116-core-final2/core.cobertura.xml`, SHA-256
`c896dc9d95dc7f073237c80630d387c22267da46b598d1ba1bb27bf40e830cd6`.

Both format gates pass. The serial Engineering Release build passes all 77 projects with zero
warnings and errors. The canonical serialized run passes all 6,536 tests across 23 hermetic hosts
with no failure or skip. A deliberately non-canonical parallel host run exposed one Quartz harness
timeout under CPU contention; the exact case passed in isolation and the documented
`--max-parallel-test-modules 1` run passed without weakening any deadline. Package/API validation
passes 18 journeys, 31 fresh packages, three isolated provider-testing consumers, and all 30
runtime APIs. The public API remains 18,879 lines with SHA-256
`ab7469f985f1e269c5cceb803c06cfdd27cfe19f9b4ca51eded8f6c97857f12f`.

A final targeted architecture run passes 53/53 bidirectional Async and source-file naming cases.
Requirements, comments, directives, dummy and legacy markers, SDK pinning, empty directories,
formatting, and Git whitespace pass. The sole source `NotImplementedException` reference is the
intentional non-retryable exception-classification case. Protected `review/` and `TestResults/`
remain unchanged and unstaged. The overall A+ goal continues with the remaining complete source
owners and the final repository-wide audit.

## Iteration 117 Core Events plan and completion

Iteration 117 reviews all 12 Core Events production files, their 642 initial and 639 final physical
source lines, all direct event tests, the readiness driver, every product call site, and every source
comment manually. `Faults/`, `Readiness/`, and `Receiving/` remain cohesive responsibilities inside
the Core assembly. `src/ViciOne.ServiceBus` is the Core project rather than an umbrella directory;
independent assemblies remain direct `src` siblings and provider families remain grouped below
`Persistence`, `Scheduling`, and `Transports`. No product C# file is stored directly in the repository
`src` root.

The review covers typed and receive fault identity, payload, host, content type, deterministic time,
message-type snapshots, bounded and serialization-safe diagnostics, hostile exception metadata,
nested aggregate projection, readiness identity, lifecycle addresses, terminal state, endpoint
identity, and immutable final delivery metrics. Red-first tests expose that empty aggregate failures
produce no diagnostic, nested aggregates lose sibling leaves, and completed transports accept
negative or internally impossible metrics. One shared bounded aggregate projector now preserves an
empty aggregate and flattens non-empty aggregates into at most sixteen leaf snapshots. Completed
transport events capture their counters once and reject invalid snapshots at construction.

The original focused profile passes 31/31. The aggregate additions first produce exactly four
failures among 41 cases; after their correction the metrics boundary adds exactly three failures
among 44 cases. The final focused profile passes 45/45. Four simultaneous controlled
counterchanges produce eight causal failures among 44 cases, and a separately isolated receive
address counterchange fails its exact projection test. Every accepted source is restored to its
recorded SHA-256 before final validation.

Focused Events coverage rises from 94.7368% line (180/190) and 95.2381% branch (120/126) to
98.9637% line (191/193) and 98.3607% branch (120/122), with maximum CRAP 20 and no method above 30.
The focused artifact is `/private/tmp/vsb-iteration117-events-final2/events-final2.cobertura.xml`,
SHA-256 `37fbd434844348edd1737854abf5af6cb34fa5a5b7a7fb7ae54ce7f8d3beba25`.
Complete Core coverage passes 3,574/3,574 and executes all 193 owner lines; owner branch coverage is
120/122. The two remaining branches are defensive null fallbacks that cannot occur for an actual
boxed enum or instantiated runtime type. The complete artifact is
`/private/tmp/vsb-iteration117-core-final2/core-final2.cobertura.xml`, SHA-256
`c7142a4e18e6b9de70eabd8d1fb6c0b3525dc307821bdb5292fac8aebaf574ed`.

Both format gates pass. The serial 77-project Engineering Release build has zero warnings and
errors. All 23 canonical hermetic hosts pass 6,550/6,550 with no failure or skip. Package/API
validation passes 18 developer journeys, 31 fresh packages, three isolated provider-testing
consumers, and all 30 runtime APIs; the unchanged 18,879-line public API SHA-256 is
`ab7469f985f1e269c5cceb803c06cfdd27cfe19f9b4ca51eded8f6c97857f12f`. The isolated semantic Async
review passes 30/30 after scanning product and test methods bidirectionally.

The 14 new or changed test methods, comprising 16 affected executed cases, pass manual
anti-pattern review: every test has causal assertions and there are no sleeps, wall-clock or random
inputs, skips, broad catches, swallowed failures, shared mutable fixtures, assertion-free paths, or
coverage-only assertions. Requirements, comments, directives, dummy and legacy markers, SDK
pinning, filenames, folders, namespaces, empty directories, formatting, and Git whitespace pass.
Protected `review/` and `TestResults/` remain unchanged and unstaged. The overall A+ goal continues
with the remaining complete source owners and the final repository-wide audit.

## Iteration 118 Core Topology plan

Iteration 118 reviews the complete transport-independent Topology capability: 37 Core files and
1,561 lines plus 62 Abstractions files and 2,369 lines, for 99 production files and 3,930 initial
lines. Its direct tests, runtime consumers, and every source comment are read manually. No generator
may produce or rewrite source, tests, comments, namespaces, filenames, or directories.

The owner is the transport-independent topology engine and remains within the
`src/ViciOne.ServiceBus` Core project. Public topology contracts remain in the independent
`ViciOne.ServiceBus.Abstractions` assembly; provider-specific entity models remain with their
transport assemblies. The review must prove that these dependency and physical boundaries are
real rather than merely visually uniform.

### Iteration 118 requirement-to-test map

| Requirement | Planned evidence |
|---|---|
| Correlation identifiers are selected predictably for interface, property, nullable, and delegate contracts | exact send-context projection, precedence, null, invalid type, and exception behavior |
| Message topology convention selection is deterministic | exact-match, interface/base traversal, cache identity, ordering, replacement, exclusion, and unsupported-contract cases |
| Routing key, partition key, correlation, and serializer conventions attach only their owned behavior | direct configuration plus executable send-pipeline evidence |
| Entity collections have explicit identity, name, identifier, idempotency, conflict, lookup, and enumeration semantics | exact name/key/handle ownership and boundary tests |
| Entity names are valid and bounded without collisions introduced by shortening | boundary-length, deterministic suffix, invalid maximum, and distinct-input evidence |
| Consume and global topology expose one coherent lifecycle and observer contract | message topology identity, bind/deploy/probe behavior, observer notification, and failure propagation |
| Files, namespaces, folders, comments, and assembly dependencies match their owners | full manual read plus repository architecture gates |
| No public feature or package API is lost | focused and Core coverage, mutation evidence, build, all hosts, package journeys, and packed API baseline |

### Iteration 118 validation

1. Record a fresh focused baseline before product edits.
2. Read all 37 production files, direct tests, public contracts, runtime call sites, and comments.
3. Add red-first tests only for independently justified behavioral or boundary defects.
4. Correct each defect at its owning boundary without compatibility-only surface or feature loss.
5. Kill meaningful controlled counterchanges and restore accepted sources byte-for-byte.
6. Audit every changed test for causal assertions, determinism, isolation, and anti-patterns.
7. Run focused and Core coverage and calculate owner CRAP risk.
8. Run both format gates, the serial Engineering build, all hermetic hosts, package/API,
   requirements, Async/source architecture, hygiene, empty-folder, and Git gates.
9. Commit, annotate Iteration 118, push without force, and verify remote hashes.

### Iteration 118 completion

The complete transport-independent Topology capability is manually reviewed and remediated. Its
final physical layout contains 100 C# files and 4,331 lines across six namespace-aligned folders:
Core `Advanced/Topology`, `Configuration/Topology`, and `Topology`, plus the corresponding three
folders in the independent Abstractions project. `src/ViciOne.ServiceBus` remains the Core project,
not an umbrella for other assemblies. Independent assemblies remain direct `src` siblings, while
provider projects remain grouped beneath `Persistence`, `Scheduling`, and `Transports`. The exact
file manifests and namespaces are protected by a repository architecture test.

The public empty message-type marker and four pass-through observable implementations are removed.
Implementation-only correlation, partition, routing, serializer, and topology-convention types are
internal. Required collaborators and factory results fail at their owning boundary; convention
cache publication, root observer ownership, child-builder state, entity identity, entity-name
evaluation, application freeze behavior, and correlation precedence are deterministic. The former
unreachable `JsonElement` consume exclusion and the dummy cache constructor argument are removed.
The serializer extension owner is now `SerializerConventionExtensions`, and every affected
transport override uses the parameter-free factory hook.

Two controlled mutation groups prove the substantive contracts. Four Core counterchanges produce
six exact failures among 35 focused cases; four Abstractions counterchanges produce exactly four
failures among 50 cases. Every source is restored to its recorded SHA-256 before accepted builds.
The final Core host passes 3,611/3,611 and the Abstractions host passes 692/692. The canonical
23-host profile passes 6,638/6,638 without failure or skip.

Core Topology coverage is 97.0149% line (520/536) and 88.5714% branch (124/140), with maximum CRAP
10. Abstractions Topology coverage is 100% line (495/495) and 89.2045% branch (157/176), with
maximum CRAP 8. Combined owner coverage is 98.4481% line (1,015/1,031) and 88.9241% branch
(281/316); no method exceeds CRAP 30. Complete loaded-product coverage in the Core run is 77.3865%
line (49,224/63,608) and 70.1117% branch (17,143/24,451).

All three locked restores pass. Both final format gates make no change. The serial 77-project
Engineering Release build passes with warnings as errors and reports zero warnings and errors.
Package/API verification passes 18 journeys, 31 fresh packages, three isolated provider-testing
consumers, and all 30 runtime APIs. The intentionally reduced public API contains 18,824 lines with
SHA-256 `493a793a915b88ac2ea9b81cb6be8057ecf9beff80535f063aab8c3040d12a4f`.

The final anti-pattern review accounts for 86 new or changed test methods and 90 executed cases.
Every case has causal assertions; no skip, broad catch, swallowed failure, assertion-free path,
shared mutable fixture, mystery dependency, random input, or coverage-only behavior remains. The
two concurrency cases use explicit signals and bounded cancellation safeguards and kill their
respective race counterchanges. The isolated Async/source-layout gate passes 54/54. Requirements,
directives, old Topology names and paths, SDK patch pinning, empty directories, formatting, and Git
whitespace are clean. Protected `review/` and `TestResults/` remain unchanged and unstaged. The
overall A+ goal continues with the remaining complete source owners and the final repository-wide
audit.

## Iteration 119 Core retry and rescue plan

Iteration 119 reviews the complete provider-neutral retry and rescue capability: 78 production
files and 5,430 initial lines spanning public policy and observer abstractions, exception filters,
retry and rescue configuration, runtime middleware, retry-context projections, policy algorithms,
pending-fault coordination, and the Core host retry boundary. The capability is reviewed as one
execution chain rather than as unrelated directory fragments. Every source file and every source
comment is read manually; no generator may create or rewrite source, tests, comments, namespaces,
filenames, or directories.

Public contracts remain in the independently packaged `ViciOne.ServiceBus.Abstractions` project.
Provider-neutral implementations remain in the `ViciOne.ServiceBus` Core project. Saga, Courier,
scheduler, and transport-specific retry integrations remain with their independently delivered
owners and are exercised as consumers where needed; they are not moved into Core.

The accepted pre-change native-MTP baseline passes 116/116 focused Retry, Rescue, and
ExceptionFilter cases without failure or skip.

### Iteration 119 requirement-to-test map

| Requirement | Planned evidence |
|---|---|
| Exception filters compose predictably across type hierarchies and predicates | exact handle, ignore, composite, null, ordering, inheritance, and predicate-failure behavior |
| Retry algorithms produce valid and deterministic schedules | boundary counts, interval sequences, exponential and incremental arithmetic, overflow, jitter, and cancellation behavior |
| Retry contexts preserve message and execution identity | payload, headers, retry count, exception, delay, cancellation token, consumer/activity context, and redelivery projection evidence |
| Retry and rescue filters preserve pipeline semantics | success, handled failure, exhausted failure, observer failure, cancellation, payload restoration, and pending-fault completion paths |
| Configuration APIs fail at their owning boundary | every required configurator, factory, predicate, callback, interval sequence, and policy result has exact validation evidence |
| Technical retry policy is explicit and conservative | every classified failure kind, nested exception form, override, delay, exhaustion, and host startup path has exact tests |
| Navigation and API are Greenfield coherent | full manual comment review plus exact type, filename, namespace, folder, visibility, naming, and packed API checks |
| Tests detect meaningful defects | red-first regressions, controlled counterchanges, focused/full coverage and CRAP, assertion-quality review, full build, package/API, requirements, Async, and hygiene gates |

### Iteration 119 validation

1. Read the complete 78-file production owner, all comments, direct tests, and runtime consumers.
2. Record source-to-test coverage and identify untested branches and shallow assertions before edits.
3. Add deterministic red-first tests only for independently justified defects or missing contracts.
4. Correct each defect at its owning boundary without compatibility-only API or feature loss.
5. Kill meaningful controlled counterchanges and restore every accepted source byte-for-byte.
6. Audit all changed tests for causal assertions, determinism, isolation, and test smells.
7. Measure focused and complete-host coverage and calculate owner CRAP risk.
8. Run locked restores, both format gates, the serial warnings-as-errors build, all canonical hosts,
   requirements, bidirectional Async, source-layout, package/API, directive, dummy, empty-folder,
   and Git-whitespace gates.
9. Commit, annotate Iteration 119, push without force, and verify remote hashes.

### Iteration 119 progress — 2026-09-15

The complete planned retry/rescue source owner and its comments have been read manually. Direct
consumers were also read before changing observer attachment. Core and Abstractions remain sibling
assembly owners; Persistence, Scheduling, and Transports remain integration families under `src`.
The Advanced retry-context extension was moved into its namespace-aligned Abstractions folder.

Confirmed behavior corrections cover current-exception identity, terminal and unhandled-initial
null delays, interval exhaustion, incremental terminal overflow, null policy/context/task guards,
and rescue admission guards. Implementation-only retry and rescue types are internal, while the
cross-assembly `PipeRetryExtensions` execution capability remains public. Observer attachment now
uses an explicit `Attach` operation rather than construction for a discarded result.

Direct untyped consume-policy tests exposed both a null-context exception-contract defect and an
inaccessible DispatchProxy interface in the shared test fixture. Both were corrected. The expanded
consume-policy suite passes 10/10; direct rescue projections pass 4/4; retry-helper tests pass 19/19.
Pre-retry and terminal-await counterchanges each produce exactly one causal failure and are restored
to SHA-256 `89ad6463d637636c2a3dd71f9f6a3a686a66ab77c48c9b7b689bf9a5b2a694b7` before the later
behavior-preserving callback/cancellation refactor.

The accepted pre-Split-remediation Core run passes 3,647/3,647. Its coverage artifact is
`/private/tmp/vsb-iteration119-core-v4/iteration119-core-v4.cobertura.xml`, SHA-256
`8b216b3acb05484631a6348772ca90b7d85e878166f6613e7a45f17f6c672e3a`; CTRF SHA-256 is
`08a401858a38b7901b25e93f9aee3a6b5a077a6c7a268d4c9b8caf89a23ea53c`. The explicitly selected
Core/Abstractions coverage cut contains 79 current paths and 65 instrumented source files: 82.1816%
line and 77.8997% branch. All four rescue projections have 100% line and branch execution.
`ExecuteAsync` is fully executed with CRAP 26; the functionally grouped classifier is fully
executed with maximum CRAP 30. The instrumented Core graph records 77.5322% line and 70.2701%
branch, not complete provider-wide ServiceBus coverage.

The next direct configuration test exposes a related shared-adapter defect: Split specifications
discard inner validation results. The 9-case rescue suite produces exactly one red-first failure.
The shared adapter has now been read completely and corrected, including explicit required-input
guards and manually rewritten comments. Four direct Abstractions tests and three public rescue
configuration tests cover this extension of scope. The complete serial Unit build passes with
zero warnings and errors; the direct rescue and shared Split suites pass 9/9 and 4/4.
The canonical run executes all 23 hosts: 6,681/6,682 pass, with zero skips. Its sole failure is
the bidirectional Async guard, which identifies the missing suffix on a new asynchronous test.
The method and its requirement tuple are corrected manually without weakening the guard; the
corrected build and complete Async scan are rerunning. Final acceptance, final-source
mutations/coverage, repository/package/API gates, and final publication remain pending.
An intermediate Git checkpoint secures the remediation without claiming completed acceptance.
Protected trees remain out of scope and unstaged.

### Iteration 119 internal counterreview remediation

Checkpoint `97c1b364bf37ed48387373f5feee3e23f065fd27` and its annotated checkpoint tag are
verified on origin without force. The corrected complete Async-name scan passes 1/1, and the
checkpoint Core/Abstractions hosts pass 3,650/3,650 and 696/696. These results predate the next
counterreview corrections and are not final-source acceptance.

The separate read-only internal reviewer completely reads all 75 changed production/project paths
and all 13 changed C# test/helper files. It finds five production defects and one visibility-guard
gap; the complete scoped report is in
`evidence/WP-F2-SERVICEBUS-SOURCE-ARCHITECTURE-A-PLUS-01/ITERATION-119/INTERNAL_COUNTERREVIEW.md`.
The review is not represented as an independent external acceptance.

New causal regressions and stronger disposal assertions run against the unchanged product source:
57 cases execute, 39 pass and 18 fail, with no skip. The CTRF report is
`/private/tmp/vsb-iteration119-counterreview-red/counterreview-red.ctrf.json`.
All failures match accepted counterexamples: creation/completion observer faults, pending observer
faults, callback cancellation admission, tick-precision exponential bounds/growth, missing-context
disposal, projection failure disposal, and typed/untyped null callback tasks.

Corrections move creation/completion observation outside business-failure classification, retain
standard using ownership, pass source/policy-linked callback cancellation with original failure-token
identity, preserve positive exponential growth and bounds in ticks, dispose failed consume wrappers,
and reject invalid underlying callback results. The visibility guard includes retry child namespaces
and the actual one-parameter activity filter. The cancellation oracle is strengthened to independent
source/policy token sources rather than relying on fixture-side linking. Final-source tests,
counterchanges, coverage, API/format gates and final iteration publication remain pending.

### Iteration 119 counterchange and assembly-anchor follow-up

The corrected three-class selection passes 59/59 after adding both failed-representation
ownership cases. All nine new method mappings are registered manually. Seven separate runtime
counterchanges kill exactly their owning cases: completion replay 3/35, creation retry 3/35,
ignored callback cancellation 2/35, fractional minimum 1/6, zero growth 1/6, missing consume
error disposal 6/18, and missing null-task diagnostics 4/18. Accepted files are restored byte-for-byte.

The visibility counterchange exposes a further assurance defect: IBus anchors Abstractions,
not Core. An isolated child-namespace mutant survives the old guard. The corrected guard reuses
ProductAssemblyFacts.Core and asserts the exact Core identity; its child-namespace counterchange
now fails 1/1. The activity visibility counterchange is being repeated without overlapping tests,
source restoration, or builds. Dependency-injection and telemetry absence guards are extended
to both actual foundation assemblies after complete manual reads. Foundation/removal catalogue
anchors were already correct. Manual activity comments clarify the actual execution context and
redelivery responsibility. Full final-source gates and final publication remain open.

The restored-source strict Engineering build passes with zero warnings/errors in 4m29.58s.
The corrected-source internal review confirms all local accepted axes but establishes one P1
nested lifecycle ownership defect. Add a five-phase nested observer-failure matrix with exact
failure/effect/disposal and outer-notification oracles, pending barriers, typed/replacement-context
propagation, and operation-lifetime coverage. Mark and propagate only the exact lifecycle failure;
never disable ordinary business classification or fabricate business RetryContext ownership.
Kill a controlled recognition-removal counterchange and rerun the scoped review before the
remaining final-source gates. Secure the current coherent delta in another explicitly intermediate
checkpoint without claiming completed Iteration119.

### Iteration 119 nested lifecycle execution follow-up

The original five-phase nested matrix proves 15 causal failures against the unchanged second
checkpoint and then passes 50/50 with exact-exception operation-lifetime ownership. New independent
projection and same-context/same-exception reuse oracles pass. Extended nested callback/cancellation
admission proves three further failures in 64 cases. Actual typed command dispatch with replacement
initial/retry contexts adds ten passing cases. Two injected-clock timer cancellation oracles prove
retained source-cancelled timers and outer restarts after policy cancellation; 76 cases record
71 passes and five causal failures before the next correction. Preparation now links both tokens
before delay, retains normalized cancellation identity, and marks the entire stage; fault callbacks
also receive lifecycle ownership. Build, execution, separate internal review, controlled ownership
counterchanges and the full repository gates remain open. No source generator is used.

The complete repeated internal review confirms the ordinary in-memory observer/preparation
correction and accepts four adjacent open findings (NN-01 through NN-04). Next coherent packet:
red-first ordinary/activity redelivery composition in both directions; primary/cleanup exact-identity
and combined-failure preservation; factory/classifier/null-output admission without outer replay;
terminal-phase reuse with active-operation business ownership while preserving caller-owned payloads
and post-failure diagnostics. Also exercise awaited multi-observer and policy fault callback failures.
Keep all real business retry budgets, filtering, typed context transitions and redelivery capabilities.
Then repeat scoped review, controlled mutations, both complete formatting/build profiles, fresh
coverage/CRAP, canonical hosts and package/API gates before final iteration publication.
The full intermediate-checkpoint Core host passes 3,709/3,709 with no skips. A recognition-removal
counterchange is being tested separately before restoring the exact accepted source and backup.

The read-only coverage analysis finds an additional author-discovered measurement blind spot:
built-in DebuggerNonUserCode exclusions omit RetryFilter Send/Attempt and redelivery Send state
machines from historical default-profile artifacts. A manually authored tools/ci/coverage.settings.xml
disables built-in attribute exclusions, includes auto-properties and excludes test assemblies.
All future coverage commands must explicitly use --coverage-settings tools/ci/coverage.settings.xml;
verify critical operations are included before calculating complete measured-source CRAP.
Expanded-profile percentages are not directly comparable to the historical denominator.
The separate recognition-removal mutation kills39/76 and the lease-release omission kills exactly
the two later-business reuse variants; both sources are independently restored byte-for-byte.
