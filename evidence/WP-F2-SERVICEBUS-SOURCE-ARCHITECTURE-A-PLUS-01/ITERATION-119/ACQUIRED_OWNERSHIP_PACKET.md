# Acquired retry ownership references

Date: 2026-09-15. Bound order/slice are unchanged. Original autonomous Greenfield
source-architecture goal remains active. This is a corrective checkpoint packet,
not final iteration119, complete source-read coverage or provider/product A+ acceptance.

## Secured input and scope

Input commit `0894a7f3cf59e6dd038ae5a080266fe69cba8997` and annotated tag
`servicebus-a-plus-iteration-119-getter-payload-factory-checkpoint-2026-09-15` are
verified on origin before changes. Protected untracked review/ and TestResults/
remain untouched. No force-push, new dependency or global exception registry.

The author personally reads the complete RetryOperationState, RetryPolicyExecution,
RetryFilter, RedeliveryRetryExecution, ScopePipeContext, IPayloadCache and
ListPayloadCache; all affected test/fixture/helper methods are personally read.
No source/test/comment generator. Scope and empty-cache comments are written manually
from the current functional behavior, without changing those types' behavior.

Independent SDK project siblings directly under src are intentional assembly/package
boundaries. src/ViciOne.ServiceBus is Core, not an umbrella. Keep provider-family
groups Persistence, Scheduling and Transports. Namespace/function/type/file alignment
is reviewed within each project as the complete source-read inventory advances.
These seven reads do not establish completion of that entire inventory.

## Findings and correction

Necessary admission of an independent initial or selected retry projection must be
owned infrastructure, not eligible business work. Enter acquires payload state before
publishing Current; initial and retry admission are protected on the acquired input
operation. Failure preserves exact primary identity, ordered secondary cleanup and
once-only policy disposal without outer replay or fabricated business success.

Mark and post-cleanup Propagate previously repeated user-defined GetOrAddPayload or
TryGetPayload while unwinding. Recognition and terminal lookup repeated callbacks too.
Each invocation now retains context-reference-to-marker associations. Failure marking,
recognition and propagation select directly acquired references without these callbacks.
The actual public diagnostic AddOrUpdatePayload remains necessary and its own escaped
failure is safely marked without entering another payload callback.

Marker state belongs to (marker, operation), not (context, operation). Historical
alias identity must remain available when that alias's own lease ends but another
alias keeps its marker live. Associations are retained until total operation scope
count reaches zero, then actually cleared. Marker entries still end on their own last
scope. Admission and final clearing are serialized with consistent operation → marker
locking; no reverse lock order or custom callback under ownership locks. Children
reuse acquired marker identity but create independent failure/terminal State. Transfer
requires active child and parent entries on the same marker, including aliases not
previously cached on the parent, rather than a process-global exception identity.

The duplicate generic terminal notification paths are a single NotifyTerminalAsync
responsibility. Ordinary await replaces manual completed-task-status branches.
Preserve terminal decision identity, notification order, specific null-fault-task
diagnostics, effective cancellation, business budget and observer-failure ownership.

## Human-written tests and historical oracle supersession

Ownership now has24 methods/106 cases, versus18/91 at input. Fifteen additional cases
cover necessary initial/retry projection admission (4), acquired alias/projected Mark
(4), post-cleanup transfer (2), already-associated child admission (1), real public
diagnostic failure (2), and released alias with live shared marker (2). The alias
helper captures actual association storage and asserts zero after final release,
in addition to zero marker entries and independent fresh child state.

The previous four terminal lookup-failure cases are not deleted or weakened. Their
historical executed red/green/mutation artifacts stay unchanged. A callback-free
lookup legitimately no longer throws the injected callback failure; the stronger
replacement asserts zero lookups, exact business primary/ordered cleanup, one/two
attempts, zero effects, exact terminal decision/events, no outer budget and released
storage. Deliberately retaining a redundant callback would preserve the defect.

Existing RetryFilter33 methods/76 cases and consume policy11 methods/23 cases retain
their assertions. Combined bounded suite:68 methods/205 cases. These counts are not
all-product API/parameter100% assurance or real transport-provider acceptance.

| User requirement / invariant | Evidence |
| --- | --- |
| “features dürfen nicht verloren gehen” | Existing alias/projection, independent/parallel child, cancellation, lifecycle, terminal budgets and ordered cleanup assertions remain mandatory |
| “den kommentar selbst zu schreiben ohne scripte” | Complete affected source read; Scope and empty-cache summaries manually authored; no generator |
| Exact infrastructure ownership | Admission/Mark/transfer/diagnostic identity and once-only cleanup oracles; unchanged-product causal failures below |
| Marker alias lifetime and release | Unarmed old-semantic positive case, armed no-callback case, actual final association Count0 and marker entries0 |

## Executed causal red and initial green

Strict unchanged-product build: zero warnings/errors. First104 run:89 pass, exactly15
new/strengthened cases fail, zero skip. Raw
`/private/tmp/vsb-iteration119-acquired-scope-red/acquired-scope-red.ctrf.json`, SHA-256
`f25720572ebd9f121cb590f509c0b54e574af38a936ad88a2ff0884b29d83a2a`.
Expanded106 unchanged-product run:90 pass,16 fail, zero skip; the armed alias is the
additional failure and unarmed alias establishes legitimate old marker semantics.
Raw `/private/tmp/vsb-iteration119-acquired-scope-alias-red/acquired-scope-alias-red.ctrf.json`,
SHA-256 `360876053ee567466d579dcee6f5496ae3fba426833540ed5ee7960977fbea8a`.
Both compound admission cases are valid positive guards, not expected causal failures.
No accepted red has fixture errors. Missing test namespace CS0246 and explicit nullable
guards CS8604 are corrected separately; neither compiler failure is mutation evidence.

Corrected strict build: zero warnings/errors. Initial ownership106/106, existing
RetryFilter76/76, consume23/23 all pass, zero skip against the same compiled source.
The actual association-storage assertion is strengthened afterward and rebuilt cleanly;
full Core, Abstractions, mutation, coverage/CRAP, format and internal frozen review
completion must be recorded below before local packet acceptance.

## Remaining boundaries

Actual delayed/RabbitMQ provider publishing/topology cancellation is still open and
not replaced by local redelivery recording fixtures. BasePipeContext cache-constructor
null policy and ScopePipeContext nullable runtime-type validation deserve the next
foundation packet; the latter currently dereferences payloadType before cache validation.
Broader current all-host, package/API/journey/isolated-consumer and complete personal
source-read gates remain required for final iteration/goal closure. No external red
team acceptance, whole-product coverage percentage or overall A+ is claimed here.

## Frozen full proof and risk profile

Resource-strengthened strict build: zero warnings/errors. Full Core3,820/3,820 and
freshly built Abstractions696/696 pass, zero skip. Both supported Product/Unit
format verify-only runs complete exit0 without edits; known workspace-load warning
is retained rather than hidden. Frozen internal static counterreview identifies no
new concrete findings in this packet and explicitly releases the source/test freeze.
No executable edit is made until every active proof process is terminal as well.

Raw full Core `/private/tmp/vsb-iteration119-acquired-scope-profile/acquired-scope-profile.ctrf.json`,
SHA-256 `9ed01233c619a3bdc1cda44bf712bc41d62249c3d236a888c23d6b10f3fb8a7a`.
Raw profile XML `/private/tmp/vsb-iteration119-acquired-scope-profile/acquired-scope-profile.cobertura.xml`,
SHA-256 `e069bad445c2997c2b65676ba697bd74956c3d6c9fdeb5f781216ec5ff466b2c`.
Raw Abstractions `/private/tmp/vsb-iteration119-acquired-scope-abstractions/acquired-scope-abstractions.ctrf.json`,
SHA-256 `6c70d1bb1ad813b18108295dd2aea034263ae805bed4cd3ae0864e36f0bcb109`.

Kernel433/449 lines96.4365%,193/234 branches82.4786%,100 emitted methods,0 CRAP>30.
Loaded Core graph49,343/60,934 lines80.9778%,16,854/23,082 branches73.0179%.
Attempt complexity32→22, CRAP32.8438→22.3636 through actual shared responsibility,
not exclusions or touch-only tests. All13 below-threshold methods remain documented
in ACQUIRED_SCOPE_COVERAGE/coverage-analysis.md, with exact denominators and risks.
The next review/test loop must prioritize real null-fault-task/ownership boundaries,
then thin validation/probe paths; no entire-product coverage or100% API correctness.

## Independent compilable counterchanges

Omitting actual association clearing kills exactly the two released-alias cases:
104 pass,2 fail,0 skip; each observes3 retained entries instead of0. The counterchange
build succeeds with zero warnings/errors. Raw
`/private/tmp/vsb-iteration119-mutant-retained-associations/mutant-retained-associations.ctrf.json`,
SHA-256 `3384a99bb845d7663d3cda37278b7580a850326a52799d3016644eb702e67c6f`.
Restore source byte hash91f4f238 before the next independent counterchange.
Repeated GetOrAddPayload in Mark kills exactly seven cases: four marking variants,
both actual diagnostic failures, and the armed alias's redundant-acquisition assertion.
99 pass,7 fail,0 skip; strict counterchange build has zero warnings/errors. Raw
`/private/tmp/vsb-iteration119-mutant-repeated-mark/mutant-repeated-mark.ctrf.json`,
SHA-256 `6c00539d3c1f1adccf85b8219969aff9646b14740fc118c7db8c9d602e4b40c8`.
Restore and independently verify source91f4f238 before the transfer counterchange.
Repeated TryGetPayload during post-cleanup transfer kills exactly both transfer
cases:104 pass,2 fail,0 skip; both incorrectly complete without preserving their
primary/ordered compound failure. Counterchange compilation succeeds. Raw
`/private/tmp/vsb-iteration119-mutant-repeated-transfer/mutant-repeated-transfer.ctrf.json`,
SHA-256 `bb1f8736ac29b7c79bfc18b3eaa137f16b1e534c8af190ade648700ec8928c86`.
Restore and independently verify source91f4f238 before the admission counterchange.
Unguarded initial/retry projection admission kills exactly both sole admission cases:
104 pass,2 fail,0 skip; each incorrectly completes via outer replay. Both compound
positive guards stay green. Counterchange compilation succeeds. Raw
`/private/tmp/vsb-iteration119-mutant-unguarded-admission/mutant-unguarded-admission.ctrf.json`,
SHA-256 `24623076c4c4c825fead18874cd794591ea5c4ab08f440e2fc2b8c01e35c6475`.
Restore PolicyExecution94a779a3 and RetryFilter59bd5c3e; independently verify all
kernel/test/helper/comment inputs against the frozen review/profile byte hashes.
All four counterchanges are individually compilable and killed by behavioral or
actual-release assertions, never by compiler/fixture failures. None remains in source.

## Final restored checkpoint proof

After all four mutations, restore and independently verify the exact frozen source,
test, helper and comment hashes. Strict restored build succeeds; the final full Core
rerun passes3,820/3,820, zero failed/skipped,24.064 seconds. Raw
`/private/tmp/vsb-iteration119-acquired-scope-restored/acquired-scope-restored.ctrf.json`,
SHA-256 `307376bb92a78e9b35a766abf34b762a70cd4d79dd59242fb3e846d2535945ea`.
All recorded causal/profile/Abstractions/mutation artifacts are checked against their
actual bytes. Requirement tuples2,827 are unique; Git whitespace verification passes.
Final Fact/Theory method audit confirms24+33+11=68 methods/205 cases; the early25/69
estimate was an accounting error, not a changed, dropped or skipped test. Correct the
owned reports before checkpointing; source and executed case counts are unchanged.

Bounded acquired-ownership correction is locally validated. Source/comment reads,
behavioral assertions, independent killed counterchanges and static internal review
are evidence for this packet, not an absolute correctness proof or whole-product A+.
Final iteration119 and the original goal remain active through the secured checkpoint.
