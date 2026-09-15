# Saga Core reading and outbox lifecycle documentation checkpoint

The original whole-product A+ Greenfield API/codearchitecture goal remains active
and unbounded. This iteration completes a coherent personal saga test-reading
addition and related manual source-comment repairs. It does not complete the
whole goal, accept an unread test owner or disguise runtime defects as documentation.

## Secured input and unchanged authority

Input commit: 3bb808bc999141638b1620c4adeb5bc51ceacc86.
Input annotated tag:
servicebus-a-plus-iteration-122-state-accessor-source-checkpoint-2026-09-15.
Observed tag object: d153740b5566f4cf6a7023e13b269b6ee94ae7bc; peeled commit
equals the input branch commit. The previous exact ten-file owned checkpoint is
committed, normally atomically pushed (actual exit 0), and independently verified
by reference-keyed branch/tag/peeled comparison (actual exit 0, three matches).
No Force-Push or history rewrite occurs.

Actual input-security receipts:
/private/tmp/vsb-iteration122-state-accessor-read.CdDUDv,
push SHA256 d87c5b731bb3e2bb57a69a422cc0e3109995bb207f13463478a58ae4dd2fe523,
remote-query SHA256 986de1c80edddf96939658493a5b64a40294acd672841eea87e83e4d89a9a649.
Current checkpoint security is credited only after its own actual commit/tag/push
and independent reference verification, not by predicting a future hash here.

The main's previous complete normative readings are reused only after exact
SHA256 reconciliation: Agreement e6d5f60db535ad6228fca5445b68abaa7a29cd6e24b5d2f876352bc7de875d2e;
Glossary 7ce780b178a971e40b57ee7ffb3bec472becdff96ef946726e0143339793adf7;
Decisions 43d9d6a2969e16284706e4b644de73573930cd9fe408fd8db856340849599cd4;
current README a7bd61f878b84fb6f93f48402a21becd37ed253e20bab62b7026bdd165469a39;
CURRENT_ORDER 49691c76d63dea1591fd5a7fa17450d1e254ec62d616ef4705aee082517bfc3d;
Findings 9a913a7937a5d216edc3ce83940c215a8d47e81e5823ccab286aff0cf2eea397.
The selected development slice remains
vicione-architecture/work/delivery/WP-F2-SERVICEBUS-SOURCE-ARCHITECTURE-A-PLUS-01/DEVELOPMENT_SLICE.json,
SHA256 5a9cd605540cd826a756e24ca3f813cb3ce9a1eaf1fc4f881b6cecfbee9c7199.
No second product-role authority, new provider or unapproved capability removal.

## Complete personal reading and manually corrected comments

The main personally completes twenty additional Core-owner files / 6,804 lines:
nineteen test files and one scheduler fixture. With the preserved iteration-122
twenty-seven files / 7,104 lines this is 47/557 files / 13,908 lines; 510 owner
files remain. All forty files in the selected saga/state-machine/job state-machine
folders are read, not all Core. Effective shared configurations are separately read
or reconciled. No new Core test design, modification or full-owner acceptance occurs.
The [exact progress table](CORE_OWNER_READ_PROGRESS.md) records personal reads and
the resolved encoding-sensitive diagnostic, not an inferred percentage of A+.

The main completely reads nine related productive source files / 1,099 input
lines, understands all comments in each, and manually corrects five files:

1. InMemoryOutboxFilter: actual consume-pipeline/outbox types, optional scoped
   rebinding, factory/deferred concurrency, discard and cleanup error ordering.
   Discard or disposal can replace an earlier failure; the comment does not promise
   an exception-preservation policy absent from the code.
2. InMemoryOutboxContextFactory: process-local inbox locking is outbox-aware, not
   proof of a durable provider acceptance boundary. Completion failure is observed
   in the enclosing delivery catch without replacing its rethrown delivery exception.
3. InMemoryOutboxConsumeContext: deferred delivery precedes scheduler processing;
   discard requests schedule cancellation, not guaranteed successful cancellation
   of every schedule. Scheduler failures are caught and logged only when a warning
   logger is available.
4. OutboxContext: the discard task represents processing and cancellation requests;
   it does not guarantee that every tracked schedule has been canceled successfully.
5. InMemoryDelayProvider: repeated/concurrent DisposeAsync calls return immediately
   once disposal begins; they do not await the original call. The returned ValueTask
   represents that call's actual work, not an unsupported shared-completion promise.

Four completely read neighbors remain unchanged: IInMemoryDelayProvider,
TaskCompletionSources, SagaInstance and JobSaga. JobSaga's actual twenty-nine
properties expose the incomplete/aliased stale-event snapshot oracle. Source
comments are inspected, not automatically rewritten. No generator writes comments,
code, tests or reports. Five exact source repairs are comment-only; all nine
executable/signature byte comparisons terminate 0, final source lines 1,105.
The [source binding](SOURCE_BINDINGS.md) gives all nine input/final hashes.

## Existing-test findings, not fictitious mutation proof

All 93 existing methods / 137 declared cases in the twenty-file addition are
individually accounted for in the [handwritten review](SAGA_TEST_REVIEW.md).
Settled severity: 0 Critical / 4 High / 0 Medium / 1 Low.

| Existing-test finding | Open exact correction priority |
|---|---|
| H1: failed response is never buffered before outbox failure | Actually arrange buffered failure response, then independently require only one success response, no failure response, exact retry counts and one correlated terminal fault. |
| H2: published correlation oracle uses actual output IDs | Compare both records to independently arranged removedId/retainedId. |
| H3: complete stale-event snapshot aliases maps and omits eleven fields | Independently capture all twenty-nine properties and all mutable contents. |
| H4: removed-owner waiter can hang before its lease is released | Bound the waiter and make lease cleanup failure-safe without accepting timeout/cancellation as the expected removal result. |
| L1: Legacy test/harness naming | Rename to native message-saga terminology during an admitted owning change, preserving all functionality. |

These are actual code-reading findings, not executed mutants, newly authored tests
or repository-wide absence claims. The native cases pass, which does not close
their weak oracles. Clock-boundary, canceled-token identity, independent recurrence
date, suspended response, duplicate/unknown request and multiple waiter/observer
cleanup proof priorities are also preserved with explicit sibling-scope qualifications.

## Actual internal source counterreview and scope deviation

Internal read-only Sol advisor iteration123_outbox_comment_counterreview completely
reads eight supplied productive files / 1,022 initial candidate lines with matched
entry/exit hashes. Its bounded advice produces three mandatory manual qualifications:
outbox-aware rather than durable, repeated disposal's actual completion semantics,
and conditional warning logging. The main applies them manually after full code
understanding. The advisor then fully rereads the final factory (101 lines), delay
provider (339) and consume context (272), matches all final supplied hashes and finds
no additional mandatory comment correction. The ninth JobSaga source is the main's
complete personal read, not falsely added to the advisor's eight-file result.

The advisor initially runs an overbroad filename-only inventory that enumerates two
protected review README paths. No protected contents are read and no protected
file is written. The main reports the deviation to the user, requires exact-path
scope and the advisor stops broad enumeration. Therefore this packet does not claim
that the advisor perfectly excluded every protected filename. Main task tools and
current Git capture stay on exact permitted owners; review and TestResults are not
read, enumerated, changed or staged by the main. Unrelated indexed/user work stays.

This is real internal Lead advice, not an independent external red team, product
developer acceptance, real cloud acceptance or full A+ certificate.

## Connected runtime/API candidates remain open

1. Delay-provider repeated disposal lacks a shared awaited completion. Timer change,
   cancellation or disposal failures may prevent the later pending-delay completion
   loop. Complete callers, TimeProvider contract, admitted owning tests and causal
   proof must decide/fix the error/completion policy; weaker documentation is not a fix.
2. Outbox-filter factory and scoped-context push occur before the cleanup try; required
   factory guards and cleanup/error masking need caller/contract reconciliation.
3. Checkpoint discard checks a token without forwarding it to scheduler DiscardSinceAsync;
   parent batch failure may prevent child cleanup. Prove checkpoint/recovery ownership.
4. Context-factory lock acquisition links caller/context tokens, but discard/final release
   may still mask the primary exception. Process-local storage is not cloud durability.
5. Public infrastructure and the OutboxContext interface lacking the I prefix require
   complete API/consumer/feature-equivalence closure, not mechanical internalization
   or renaming from a bounded eight-file review.

All earlier declaration/cache/state index/configuration/observer/composite/recovery
NST and SMR findings, specialized custom-retry capability equivalence, metadata
contracts and package baseline obligations remain connected and unresolved.

## Source grouping decision clarified with current Git evidence

The user's grouping question is checked against the actual src root tree and
docs/api-surface.md, Source ownership and navigation. Directly at src root there is
one tracked shared build file, Directory.Build.props, and no C# file. Independently
compiled projects include Core, Abstractions, Sagas, JobService and Mediator as
sibling project directories. Persistence, Scheduling and Transports group provider
and integration projects by responsibility; Adapter alone is too narrow a label.

src/ViciOne.ServiceBus is the Core assembly owner, not the product umbrella folder.
Moving sibling projects under it would obscure optional package/assembly ownership
and conflict with the Core SDK project's default recursive source inclusion unless
exclusions were introduced. Retain clear sibling assembly boundaries and the three
integration families. Continue reviewing type/file/namespace/subfolder coherence
inside each fully read project. No physical project move, exclusion, dependency
change or feature loss is justified or performed by this navigation clarification.

## Actual final validation and remaining original goal

All owned validation handles are terminal. Focused strict Release Core/Architecture
builds terminate 0 with zero warnings/errors (12.60s / 4.94s). Final fresh native
unfiltered Core terminates 0: 4,007/4,007, zero failures/skips, 22.745s. Final fresh
native unfiltered Architecture terminates 0: 439/439, zero failures/skips,
4m03.398s. The actual bidirectional Async case passes (204,718ms). Independent
final report parsing verifies exact passed case counts, all 93 reviewed methods /
137 cases and the unique actual Async record. Five-source whitespace and nine-file
executable/signature equivalence both terminate 0 without source writes.

The [bound raw receipts](SOURCE_BINDINGS.md) distinguish initial/final candidates,
actual build/host exits and diagnostic-only failures. Binary byte comparison resolves
the Å fixture encoding error; map/compact resolves unsupported Ruby filter_map.
Neither diagnostic error is a product failure or justification for blind build reruns.
Strict focused incremental builds are not a clean global warning inventory.

The final read-only handwritten-report reconciliation terminates 0: all nine
source binding rows match actual git/current hashes and counts; all twenty sorted
additional owner rows match 6,804 actual lines; all ninety-three unique handwritten
method rows match the enumerated methods and 137 declared cases; all seven final
receipt hashes match their actual raw files. Its receipt is
/private/tmp/vsb-iteration123-saga-core-read.oI6Ada/final-report-binding-validation.log,
SHA256 7626a1fa33206190a7a401d92f712b407fd32a77c0f1b9219ae7f40196a363a8.
This diagnostic checks a manually authored report, not an automated disposition
or a claim to have read the remaining owner files.

No fresh actual mutation, package execution, real durable sender/provider acceptance
or current global coverage/CRAP is claimed. Historical evidence is not relabeled as
current product-wide proof. Continue complete Core-owner personal admission, then
correct the exact independent oracles/runtime contracts with real red/green and
selected one-cause mutations. Complete whole-src personal reading, manual comments,
API/type/file/folder/legacy/dummy/directive closure, feature equivalence, current
global line/branch coverage/CRAP and genuine provider acceptance still belong to
the same original active goal. This checkpoint changes no functionality.
