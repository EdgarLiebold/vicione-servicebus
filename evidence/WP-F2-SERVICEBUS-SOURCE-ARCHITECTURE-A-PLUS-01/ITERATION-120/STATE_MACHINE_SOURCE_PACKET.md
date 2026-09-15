# State-machine declaration and composite-event source review

## Authority and secured input

The original unbounded whole-product A+ goal remains active. The ServiceBus
direct-Lead slice and unchanged normative bindings apply. Protected review/result
trees are not traversed. The separate Licensing order does not govern this work.

Input commit 3a7386ca27250bffaf482b828833be643c554e23 is actually secured locally
and remotely with annotated tag
servicebus-a-plus-iteration-119-member-api-contracts-checkpoint-2026-09-15.
The tag object is 13c4e68f9f9b2a8f4b01911620ce704a73bb9ab2 and peels to that
commit. The normal atomic push terminates 0. A subsequent independent ls-remote
terminates 0; all three reference-keyed branch/tag/peeled bindings match exactly.
Push and independent remote receipts are checksum-bound in the manifest.

## Complete personal source reading and manual documentation

The main personally reads the entire 2,266-line state-machine root, in contiguous
untruncated ranges 1–260, 261–550, 551–850, 851–1140, 1141–1440, 1441–1740,
1741–2040 and 2041–EOF. Five complete neighbors supply another 481 lines:
InitialIfNullStateAccessor (53), StateAccessorIndex (57), NonTransitionEventObserver
(74), CompositeEventActivity (121) and TransitionActivity (176).

All six complete reads total 2,747 starting lines. Exact inputs and current bytes
are bound in the manifest. This proves personal reading of this bounded set,
not the whole saga family, all source, all tests or the original whole goal.
Earlier complete personal reads are reused only with unchanged bindings; internal
advisor reads never substitute for the main's personal reading obligation.

After understanding each complete file, the main manually corrects the root's
comments and every relevant CompositeEventActivity comment with apply_patch.
The other four files' existing comments are inspected; TransitionActivity's
functional comments remain accurate, and the three private helpers contain no
comments requiring correction. No comment generator or script writes source.

The root now describes cache-backed lookup, actual event enumeration, boundary
states, selected/non-transition observers, replacement of the current-state
accessor, trigger/message declarations, completion-predicate registration,
composite tracking and optional boundary inclusion, nested-state lookup by name,
actual DuringAny distribution, binder creation rather than immediate execution,
conditional ignoring, the twelve one/two/three-response request overloads,
explicit request-ID storage versus correlation-ID fallback, schedule token
filtering and implicit initialization.

CompositeEventActivity comments describe exact-mask completion, per-required-flag
RaiseOnce suppression, writing status before raising the composite event,
continuation after successful processing and unchanged-status fault forwarding.
No unconditional no-exception, callback-order or atomicity guarantee is invented.
An intermediate status-backed overload was mislabeled integer-backed; a complete
signature/comment recheck corrects that wording before final validation/capture.

Current root is 2,227 lines; CompositeEventActivity remains 121 lines. Read-only
comparison must confirm that stripping whole comment-only lines leaves exact
input executable/signature bytes. No visibility, dependency, project, directive,
test, requirement tuple or executable statement is changed. No runtime defect
is closed by corrected documentation.

## API/type/folder disposition

Both edited filenames match their primary .NET types; the state-machine helper
partials retain the established outer-type-qualified filename pattern. The root
belongs to the Sagas assembly and the activity to its SagaStateMachine feature
namespace. No independent assembly is moved inside the Core project.
The existing sibling feature projects and Persistence/Scheduling/Transports
integration families retain optional dependency boundaries; namespace sharing
does not imply one physical assembly or project folder.

The root's overload density, mutable declaration model and reflection-based
implicit initialization need connected architectural disposition after behavioral
contracts are proved. This documentation-only checkpoint does not label the
current API greenfield A+, remove an extension feature or restore legacy wrappers.
BuilderStateMachine is the concrete instance used by New, not a placeholder for
missing behavior. The default false completion predicate supplies no completion
decision until completion is explicitly configured. Conditional CompletedTask
paths in the observer/composite helpers implement filtering or incomplete status,
not unimplemented production operations.

## Connected behavioral and architectural findings

These are static source findings requiring owning behavioral tests and an explicit
contract. They are not fabricated observed runtime failures or automatic test-gap
claims. The Core test owner is not completely personally admitted yet; existing
Core execution is not permission to author new Core tests or relocate them into
Architecture. The following work remains connected to earlier NST/saga/provider/
cancellation/losslessness obligations.

| ID | Concern | Concrete source flow and remaining proof |
| --- | --- | --- |
| SMR-01 | Declaration identity and cache consistency | Implicit registrations skip any non-null property without reconciling it with the state/event cache. Named event creation replaces the cached event; named substate creation may replace a state under another parent. Prove or constrain preinitialized properties, repeated names and retained references. |
| SMR-02 | Declaration lifecycle and snapshots | Registration/property/backing-field caches are instance-local. StateAccessorIndex lazily materializes its state array while the root still exposes mutable declaration operations. Prove the supported mutation/freeze boundary and late-state index behavior; do not invent cross-machine cache sharing. |
| SMR-03 | Partial declaration after failure | Property/name composite overloads assign/register the event before validating required events. Request and schedule declarations assign their property before correlation callbacks, pending-state setup and binding complete. Prove failure rollback or stage a complete declaration before publication. |
| SMR-04 | Required inputs and optionality | New invokes modifier without a null guard; many binder/declaration callbacks and collections are dereferenced without local guards. Event(name, configure) treats its nonnullable callback as optional. Define consistent required-input and optional-callback contracts, then test their real public boundaries. |
| SMR-05 | Cancellation ownership | Completion checks wait with the supplied token but pass only the context to the stored predicate. InitialIfNullStateAccessor forwards the explicit token to reads, while its initialization behavior uses the context token. Prove separate-token semantics and propagation through user work, not only cancellation of the wait. |
| SMR-06 | Scheduled delivery identity and mutation | A supplied mismatched token is rejected; an absent message token can still deliver when the saga has a current token. Received runs before token clearing; clearing is conditional on the token staying unchanged. Prove missing/stale/current-token policy, fault behavior and handler replacement of the token. |
| SMR-07 | Composite repetition and failure | Required-event flags are written before raising the composite event; RaiseOnce suppresses an already-set flag. Duplicate required inputs, extra status bits and failed composite raises need explicit repetition/recovery contracts and owning tests, not assumptions from simple completion examples. |
| SMR-08 | Transition hierarchy and partial progress | BeforeEnter recursively visits ancestors before the target, while Enter iterates the target's parent chain in the opposite direction. State storage occurs before AfterLeave/Enter. Prove hierarchy notification order, cycles and failure progress/rollback under the intended saga transaction contract. |
| SMR-09 | Configured versus diagnostic identity | Name changes the configured _name, while Probe emits GetType().Name. Prove one consistent diagnostic identity contract; registration also accesses derived properties during base construction, before the derived constructor body. Do not incorrectly claim derived field initializers have universally not run. |

## Counterreview and terminal validation

A bounded internal Sol advisor completely reads the six current files, with exact
entry/exit checksum binding (2,708 current lines; all six hashes in the manifest).
It finds no mandatory comment correction. Four medium static priorities are
preinitialized-property adoption, event-name collision observer classification,
caught-declaration-failure partial configuration and initialization's mismatched
cancellation owner. In particular, the live name-keyed observer cache can classify
a previously declared transition as ordinary after an ordinary event overwrites
that name. Uncaught construction failure discards the unusable machine; retained
state after a caught failure is the qualified lifecycle concern. Remaining index,
configured/probe-name and base-construction callback observations stay qualified.
It performs no edits, builds, native tests, mutations, delegation or Git writes.
Its source advice is not independent external acceptance, executed regression or
universal correctness certification. No mandatory source correction follows;
the runtime findings remain open.

Strict Release Architecture/Core builds terminate 0, zero warnings/errors,
52.05s / 34.21s. Fresh complete Core native execution terminates 0, 4,007/4,007
passed, zero failures/skips (22.348s). The exact six-row input/current comparison
terminates 0: two changed comment-only files, zero executable/signature/hash/line
mismatches. The two-source scoped whitespace check terminates 0 with empty output
and no writes. Fresh unfiltered Architecture terminates 0, 401/401 passed,
zero failures/skips (2m 55.903s overall CLI duration). The actual
EveryMethodName_MatchesItsAsynchronousContractBidirectionally test passes
(139,076ms). The two complete selected owners total 4,408 passing cases;
this is not execution of every product test owner. All owned validation handles
are terminal. The final manifest binds 22 actual input/current/receipt/result
checksums, including both fresh native CTRF reports; capture follows observation.
The previous fresh package gate remains terminal 1 at unchanged API-baseline comparison; its 31 packs,
18 journeys, three isolated consumers and 30 inventories are previous-checkpoint
evidence, not a newly executed package gate for this documentation-only pass.
No new mutation is appropriate to a source comment-only delta; the previous ten
compiled formatter mutation kills are not relabeled as this pass's new mutations.
Current global line/branch coverage and full multidimensional A+ acceptance remain
open. Checkpoint capture and remote security follow actual observed outcomes.
