# Iteration 119 — in-memory saga ownership and boundary contracts

## Research

The starting branch and annotated context-contract tag both resolve on origin to
40ff13c45018fb0a9436d34cd0618024f2000620. The bound development slice and previously
read governance sources retain their recorded SHA256 values. Protected review and
TestResults trees are not read, changed or scanned.

The lead personally reads the repository contexts, consume-context factories,
default consume context, SagaInstance, indexed dictionary/property/query contracts,
existing saga capability/concurrency/integration tests, project/build configuration,
and the complete existing consume-context fixture. Comments are written manually.
The provided parse-only Roslyn pairing analyzer examines a temporary symlink view:
360 Saga source files and 545 Core test files; 92 paired and 268 unpaired. The
context, default context and generic factory have suggested mirrored test paths.
The in-memory factories and SagaInstance are already paired. These classifications
are heuristics, not behavioral or coverage proof.

Two stale test hosts are discovered by an authoritative process check (56810 and
57265), identified by their exact ServiceBus executable and working directory,
terminated with TERM, and confirmed absent before executable edits. Neither old
partial run is counted as proof. New builds and tests retain their live tool handles
and must terminate before any further executable edit.

## Acceptance checklist and plan

The original requirements remain unchanged: no feature loss; personally read all
source at least once; manually authored current functional comments; effective
tests and mutations; scoped multidimensional validation; Git checkpoint each
iteration. This packet does not claim the whole product is A+.

1. Removed saga loads return null without releasing an unowned saga semaphore:
   test load-only and message contexts, with and without a retained owner,
   and both dictionary-lease states of the message context.
2. A consume context releases its acquired saga exactly once, including concurrent
   Dispose and a subsequent owner; repository Dispose releases its dictionary
   lease exactly once, including concurrent Dispose.
3. Required context/factory/state inputs fail with exact ArgumentNullException
   parameter names, including no-op persistence paths. Invalid modes fail before
   acquisition or mutation; callback null-task outputs receive an explicit fault.
4. Preserve real add/insert/load, existing-insert refusal, state reference identity,
   save/update/undo cancellation selection, successful delete/discard and missing
   state diagnostics. A stale state context must not delete a replacement sharing
   the same correlation identifier.
5. Read and manually rewrite the selected files' inaccurate or generic comments.
   Do not invent retained-object rollback, cloning or a new persistence mechanism.

Write manual behavioral tests and exact requirement tuples before changing product
code; run strict build and causal red. Then correct only established boundaries,
run focused and complete hosts, separately compile/run/restore real ownership,
validation and replacement mutations, review the frozen source/tests, collect
fresh native coverage, verify formatting, and commit/tag/push normally.

## Adjacent findings, not silently closed

The indexed boolean predicate incorrectly evaluates the whole query against a
null saga. Mutable indexed fields also expose stale-key/removal and deferred
enumeration concerns. A complete query/index-integrity packet must preserve
current referenced-state query semantics rather than merely enabling a stale
index optimization. No query-index A+ completion is claimed in this ownership packet.

The immutable internal Sol provider review reads Azure Table, DynamoDB and EF
contexts and both generic saga dispatch pipes. Undo does not universally restore
retained mutable state or undo preinsert, and policy fault unwinding is separate.
Read-only plus preinsert is a public-policy counterexample requiring a coherent
cross-provider semantic correction, not arbitrary cloning or comment-only closure.
This is an internal static review, not external acceptance or cloud execution.

Results are recorded only after their processes terminate.

## Test-wiring corrections before causal product disposition

The initial strict build exits1 with46 test-wiring errors (omitted cancellation
arguments and nullable conditional-task conversions), the next exits1 with two
remaining direct default-token warnings. No suppression is added. Explicit test
tokens, deliberate fallback through the already-explicit helper token argument,
and base-Task conditional conversions resolve these diagnostics. The next strict
build exits0, zero warnings/errors.

The first68-case execution exits2 with2pass66fail. Sixty-two failures are fixture
TypeLoadException: DispatchProxy cannot implement a closed consume-context
interface whose newly supplied message type is private. All four nested messages
are made public, as in the existing representative saga integration tests. These
fixture failures are not causal product red evidence. The remaining six observed
product assertions concern removed-load lease release and factory callback/
pre-cancellation boundaries; a clean-fixture repeat is required before correction.

Live-build diagnosis verifies native Roslyn `csc`, not merely `csc.dll`; a one-second
parent sample confirms normal compiler waiting. The verified build is not restarted
on an observation timeout. Every build and test handle is awaited to termination.

## Clean causal result and adjacent lease boundary

After public message fixtures compile strictly with zero warnings/errors, the
clean native run exits 2: 68 cases, 32 passed, 36 failed, no skips and no fixture
TypeLoadException. Failures are verified per method in the native CTRF, not inferred
from the earlier invalid run. Product correction begins only after termination.

Three additional manual methods add seven cases and three requirement tuples:
both callback capabilities preserve synchronous fault identity, valid load tasks
may return null, and Delete/Discard must use an already-owned initial dictionary
lease without waiting on themselves. The four removal cases bound and cancel
the actual acquisition, rather than abandoning a live operation after WaitAsync.
They are run before the initial-lease removal correction.

The 75-case native repeat exits 2: 71 pass and four failures, no skips. Each
failure is actual Delete/Discard waiting on the context's already-held dictionary
semaphore until its explicit acquisition token cancels. Initial-lease removal is
then corrected to reuse the transferred lease and release it on both success and
the exact missing/replacement fault path.

A further manually authored acquisition-race test uses the real in-memory factory
behind a storage-factory decorator that invalidates the selected retained wrapper
at acquisition. This observes whether the recovery loop repeatedly selects the
same already-removed wrapper. No internal exception is fabricated. The loop is
bounded by cancellation of its actual repository wait, not an abandoned task.
The catalogue contains 2,860 unique tuples: baseline 2,835 plus 25 new methods.

## Internal counterreview and lifetime disposition

The 76-case focused green run exits 0, zero skips. The full native Core host then
passes 3,904 cases with the explicit coverage profile; Abstractions builds strictly
with zero warnings/errors. These runs precede the following additional correction
and are not final packet closure.

An internal read-only Sol counterreview accounts for all 25 methods and the nine
manually read/comment-corrected source files, verifies identical entry/exit hashes,
and explicitly releases the freeze. It is not external product acceptance.
It finds two issues: active initial-lease Add/Insert/Delete can outlive a concurrent
Dispose and mutate while a foreign dictionary owner holds the released semaphore
(major ownership defect); acquisition wrappers and an existing-insert contender
need unconditional cancellation/drain on assertion failure (test hygiene warning).

Disposition: retain initial dictionary ownership while operations actively use it;
release requests from Dispose or successful handoff are deferred until the final
active use ends. No synchronous waiting in Dispose, no user callbacks under the
lifetime-state lock, no new persistence mechanism and no state cloning. Preserve
the existing context's temporary dictionary acquisition after its initial lease
has ended. This is a technical correction, not an excluded concurrency assumption.

Two new methods add three deterministic cases: block Add/Insert inside the actual
factory path and Delete at its state identifier getter; call Dispose; a foreign
contender must stay blocked until the active mutation finishes. Tests cancel and
drain their original operations and release only successfully acquired contenders.
All timed dictionary availability probes now cancel the actual acquisition, not
only a WaitAsync wrapper. The catalogue has 2,862 tuples and 27 new methods.

The reference-count boundary adds one further method/one case: two initial Add
operations are held at independently controlled factory gates; completing the
first must not admit a foreign dictionary owner while the second remains active.
Both operations and the contender are drained even when a drain faults.

The final test-delta review identifies exceptional-cleanup ordering, then explicitly
releases its freeze. Unconditional nested contender cleanup and Task.WhenAll of
the actual creation drains close this warning. One additional theory contributes
two Add/Insert fault cases with exact callback exception identity, zero registration
and release after concurrent Dispose. Final inventory: 29 methods, 82 cases,
2,864 unique requirement tuples. Strict build exits 0 with zero warnings/errors;
the focused 82-case native execution exits 0, no skips. The final internal review
accounts for all 29 methods, verifies all 14 unchanged hashes and reports no
remaining concrete finding within this bounded packet. Adjacent work remains open.

Real counterchanges begin only after this clean baseline and review release. Each
is a single manually applied, individually compiled product mutation, executed
against exact covering tests and restored with apply_patch to its accepted SHA256.
No static candidate is counted as an executed kill or surviving mutation.

The first two compiled counterchanges are killed: consume-context unconditional
Release fails both repeated/concurrent-disposal cases (2/8 failed), and restoring
an unowned saga Release to load-only removed-state handling fails both ownership
cases (2/2 failed). Each strict build has zero warnings/errors. Each product file
is restored byte-exactly to its accepted SHA256 after its native host terminates.

The first off-by-one lifetime counterchange fails its single case, but a cleanup
token-identity assertion masks the primary ownership oracle. This first execution
is not accepted as the causal kill. SagaInstance.MarkInUseAsync uses a linked
caller/removal token, so its cancellation exception need not carry the original
caller token. The creation drain is corrected manually to require that its own
caller token is cancelled, while still awaiting/disposal-draining both actual
operations unconditionally. Production token-normalization semantics remain an
explicit adjacent finding. The counterchange is restored byte-exactly before
this correction; the corrected baseline and counterchange will be rerun.

The cleanup-only delta is independently read by the internal Sol reviewer; unchanged
entry/exit source/test hashes are reported and the freeze is explicitly released.
Its strict baseline build has zero warnings/errors and all 82 cases pass. M3's
strict rerun then fails exactly Assert.False(contender.IsCompleted) at the final-user
boundary, not cleanup. Its product bytes are restored. M4 stale Delete/Discard
fails 2/2; M5 pre-cancelled Execute fails 2/2; M6 required Save/Update context
fails 2/7. Each strict build succeeds and each mutated file is restored exactly.

The first M7 native execution was launched before the build handle's terminal
status was observed; that overlapping observation is excluded from proof. After
both processes terminate, an exact repeat fails 2/2 with NullReferenceException
instead of the specified null-task InvalidOperationException. Only this repeat
counts. The product file is then restored byte-exactly.

M8 premature Dispose release fails 6/48, including four direct ownership assertions.
Two fault-creation cases additionally mask that assertion with a cancelled factory
gate during their cleanup. Production bytes are restored before manually adding
expected caller-cancellation draining to this fault-case finally. The exact failure
identity assertions in its functional body remain intact. Baseline and M8 are
repeated after internal read-only review release; the first masked case failures
are not claimed as six causal kills.

After the final cleanup-only review explicitly releases its unchanged-hash freeze,
the strict baseline and all 82 cases pass. The M8 strict repeat fails all six
direct blocked-owner assertions; no gate-cancellation masking remains. M9's
strict build succeeds and its one case fails because the invalidated wrapper is
retried until actual operation cancellation rather than returning null. Both
product restores match the exact accepted hash. All nine selected mutations are
killed; no whole-product mutation score is implied.

The restored strict build has zero warnings/errors, focused 82/82 passes and the
complete Core host passes 3,910/3,910 with no skips. Native source-only coverage
is collected. Scoped Product whitespace exits 0 with the known workspace warning;
Unit exits 2 on ten switch-expression indentation lines in the new helper. Both
format hosts terminate before the manual indentation correction. In-memory reversal
of only those ten edits reproduces exactly the reviewed 8af11c test hash, establishing
no semantic delta. Final test hash is 038452ad5421798068064a02e4f1b49bebc34baf6921a2be464b9a9ee2bd9fed.
Strict build, full Core with fresh coverage and Unit whitespace are repeated before
Git capture; the first format failure remains disclosed, not called a pass.

Final strict post-format build exits 0 with zero warnings/errors. Complete native
Core is again 3,910/3,910 with no skips, including all 82 packet cases and exact
compiled requirement metadata. Fresh final graph-only coverage is 49,423/60,987
lines and 16,936/23,148 branches. Corrected Unit whitespace exits 0 with the known
workspace-load warning; Product already exits 0. All owned handles terminate.
The source/test/catalogue bindings and accepted raw artifacts are verified before
capture in the Saga checkpoint named in the evidence report. No overall119/goal
completion or whole-product API/coverage/cloud acceptance is claimed.
