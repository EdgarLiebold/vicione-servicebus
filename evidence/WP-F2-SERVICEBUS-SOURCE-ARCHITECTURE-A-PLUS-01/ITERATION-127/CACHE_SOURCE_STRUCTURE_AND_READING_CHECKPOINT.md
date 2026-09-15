# Cache source structure and complete scoped reading

Original whole-product A+ goal: active. This is an intermediate source/comment
checkpoint and complete reading of the selected Cache folder, not a completed
whole-product iteration, complete Core-owner admission or an A+ certificate.
All seven findings below remain open. No productive runtime fix or test repair
is represented as finished by this checkpoint.

## Secured input and authority

Repository: `repositories/vicione-servicebus`; branch:
`feature/servicebus-a-plus-api`; input:
`cabe618cae88992da2409781a5ed7e8eba001675`.
Input productive tree: `acade7c9332f643564a4df87c20337a8efccfbd2`.
Unchanged Core-owner tree: `e3be6b831183b3b65636c3b5e167c165696037d2`.
Iteration 126 is actually committed, normally atomically pushed and independently
branch/tag/peeled verified, with actual terminal security exits 0:

- Tag: `servicebus-a-plus-iteration-126-consume-request-transport-reading-checkpoint-2026-09-15`.
- Annotated object: `381b613061316e3b98ffb4d9d943b1c16abfb4dd`.
- Push receipt SHA256: `0ecb091dec68971135f40d979aee0e983dd921fd5c0064882bc106f6a3f5fa53`.
- Independent remote receipt SHA256: `a0496a18c190213a426cc10538237294049ee48dbd3fc330aa6f30866a405b8b`.
- Keyed three-reference receipt SHA256: `ab7e4e91bb79148069f66fa70297a7f65d0279a77a5dd5a4beaeb227a448f773`.

Previously completely main-read authority remains hash-identical. The agreement
is additionally completely reread during this continuation:

| Authority | SHA256 |
| --- | --- |
| AI_WORKING_AGREEMENT.md | e6d5f60db535ad6228fca5445b68abaa7a29cd6e24b5d2f876352bc7de875d2e |
| GLOSSARY.md | 7ce780b178a971e40b57ee7ffb3bec472becdff96ef946726e0143339793adf7 |
| DECISIONS.md | 43d9d6a2969e16284706e4b644de73573930cd9fe408fd8db856340849599cd4 |
| current/README.md | a7bd61f878b84fb6f93f48402a21becd37ed253e20bab62b7026bdd165469a39 |
| CURRENT_ORDER.yaml | 49691c76d63dea1591fd5a7fa17450d1e254ec62d616ef4705aee082517bfc3d |
| FINDINGS.md | 9a913a7937a5d216edc3ce83940c215a8d47e81e5823ccab286aff0cf2eea397 |
| Selected DEVELOPMENT_SLICE.json | 5a9cd605540cd826a756e24ca3f813cb3ce9a1eaf1fc4f881b6cecfbee9c7199 |

Agreement §4.3 still requires complete tracked owning test inputs, effective
shared build/package policies, fixtures, data and execution/CI configuration
before new test design, change or Lead acceptance. Executing existing tests does
not substitute for that reading. No Core test design/edit or owner acceptance
occurs here. Deferred criteria below are acceptance requirements, not replacement
test code or permission to bypass admission.

## Productive source: personally read, manually documented, correctly filed

The main completely personally reads all seventeen input productive Cache files,
1,701 physical lines, including all members, ownership paths, synchronization,
callbacks, errors and comments. The final source has eighteen files / 1,707 lines.
The six owned changed/new files are completely reviewed in their final form.
This scoped reading is not evidence that the rest of src has already been read.
No generator or script authors code, tests, comments, dispositions or this report;
all changes are handwritten apply_patch edits.

Four files receive only manually authored XML-comment corrections:

- `IResourceCacheIndex.cs`: committed/pending lookup, shared cache-owned creation,
  registered factory fallback, missing-key failure, caller wait cancellation and
  committed-only removal with uninterrupted resource release.
- `IResourceCacheObserver.cs`: serialized awaited notifications, failure isolation
  without rollback, actual reentry restrictions, callback token provenance,
  removal before disposal, and clear after ownership release.
- `KeyedResourceCache.cs`: one primary index over the shared engine, actual lookup,
  creation/removal/clear/disposal semantics. The creation owner awaits added
  notifications; another reader of a committed resource need not await them.
- `ResourceCache.cs`: shared synchronization boundary and callback placement,
  capacity backpressure, initial cancellation checks, clear ownership drain and
  shared disposal completion. The old unqualified atomicity claim is removed;
  the comparer-failure finding is not thereby considered repaired.

`Implementation/ResourceCacheIndexBase.cs` now owns the existing internal untyped
base type previously sharing `ResourceCacheIndex.cs`. Namespace, accessibility,
generic constraint, signatures and exact executable body are preserved. The
typed index keeps its own matching file. This is a file/type ownership repair,
not a new abstraction, runtime mechanism, API or removed feature. The new base
summary describes only its functional role. Other correct comments are retained.

Read-only exact diagnostics prove all non-XML text of the four documentation
files unchanged, twelve other input files byte-identical, and both moved base
and retained typed index bodies/signatures exact. Diagnostic exit: 0. Fresh
strict builds and native tests additionally verify the file split's integration.
No source project, dependency, test, directive or gate is changed.

Paths below are relative to `src/ViciOne.ServiceBus/Caching/`. Input bytes are
reconstructible from the secured input tree; the table binds final reviewed bytes.

| Productive file | Final lines | Final SHA256 |
| --- | ---: | --- |
| IResourceCacheIndex.cs | 32 | 464ca1305591f2717d5c9649ca9dce30cef51ced28416d873a54b00d43f00d12 |
| IResourceCacheObserver.cs | 32 | d9143384e1e91739ecad02ca2dbd32fac7571db655726279c74480eac9e9ad69 |
| IResourceUsageSource.cs | 13 | 425dbadfaa95efff5c152655984281da4a8741106a9a41da9b22fe70b929727c |
| Implementation/PendingResourceCreation.cs | 24 | bba8eeb8b9366996b489c820d793bb568d47c396127095a18ef45426fd2a164e |
| Implementation/PreparedResourceKeys.cs | 24 | 57e5e9d950b73282c7375b8c0101e11c01c6950825cbc073e2cd611c16da73e4 |
| Implementation/ResourceCacheEntry.cs | 27 | 907772e0d3f688f3b11b0542b79d45e76f098559a0fb99e2ad1395ebdb13112d |
| Implementation/ResourceCacheIndex.cs | 112 | 579fe812ea746dbd5b2e0e5cd458cd97a51cabe0348d38dd0e77b685373b9eca |
| Implementation/ResourceCacheIndexBase.cs | 24 | 961040eccaa94ee567fc0b95570af8fa010929242c32b5fdbc21843b4f3fb749 |
| KeyedResourceCache.cs | 82 | 0e1ad799ed2211388d2f9f197199bb5431400c25cb86b0cea92ebc54cd5ea743 |
| ResourceCache.Creation.cs | 284 | 6b1bc32ce78bad7db2dbc90c2d1ea5709e1b4759a015d9de134439c4c8180c2d |
| ResourceCache.Lifecycle.cs | 163 | 0aeb97aeae8196ed5918ccafb7401cdb2bc6f176c74f9e4b8de89833d8a3d00d |
| ResourceCache.Observers.cs | 102 | 2ce1f0d2d155c797c4e51b458bcf1e5755896810f71604377560de5272d1bb47 |
| ResourceCache.Resources.cs | 319 | 069f3f12970db51ca231d3403536023a3e90c9f9bd21143a382f83dc7aa3b282 |
| ResourceCache.cs | 354 | a017da47dd422d909edc874a21658e9474c35a1ab95edc33e7023ba22e19cf45 |
| ResourceCacheExpirationMode.cs | 11 | 70e1ef83ad34228694b8b4ff292c541e326ef7a638a9c457e8afe920728b98c7 |
| ResourceCacheOptions.cs | 75 | 8ef0c5e0beb008e8dc1fced15128dcb892166daa5ec830f1b3b8ecd861937a76 |
| ResourceCacheStatistics.cs | 16 | 69c3ab0534a57ade8c8f1007541cfa4644b15aed3e71d17db43258091087a8f4 |
| ResourceFactory.cs | 13 | 91ac695975b938389565aec792c24118b5c4b0ad99e03d0f9086496d386ce0cc |

## Core test owner: complete Cache reading, incomplete overall admission

All six Cache test files, 2,974 physical lines, are completely personally read,
including every method, fixture, helper, data arrangement, assertion and comment.
All 104 methods are reviewed and exactly reconciled to 105 passed historical
iteration-123 cases and 105 fresh iteration-127 cases. No source/test generator,
partial reading or advisor substitutes for main reading. Lexical declaration/
native membership checks are not the complete C# parser admission gate.

Paths are relative to `tests/ViciOne.ServiceBus.Tests/Caching/`:

| Test file | Lines | Methods | Historical / fresh cases | SHA256 |
| --- | ---: | ---: | ---: | --- |
| ResourceCacheConcurrencyTests.cs | 488 | 15 | 15 / 15 | 7087567d31ff3d8d42df1dcef4763d11f90cdfe6a97b9265ad8edc8528ed6063 |
| ResourceCacheContractTests.cs | 383 | 18 | 18 / 18 | fe08a94c9d3339cc2a83c85bfcc19c90e3b4f6decd3d55810d9147811062d5db |
| ResourceCacheExpirationTests.cs | 542 | 22 | 22 / 22 | 8f128a449bc35bf7012638d79237393754c1e823fb33f34d68f34005996299ba |
| ResourceCacheGenerationAndLockingTests.cs | 586 | 17 | 18 / 18 | 56e5e5b8ba01517ba221441930ecfdd9c50f8c6d1279c29220d626d96da63fdf |
| ResourceCacheLifecycleTests.cs | 361 | 13 | 13 / 13 | eca2e0a8241bb66fd2d3fd9ca90efe3a03bad181995432b348514c14d6c0bcb0 |
| ResourceCacheObserverAndDisposalTests.cs | 614 | 19 | 19 / 19 | 006f949f8ae5c2e4615b96e8c7f7dd5b2d2fef5e5126aa3aa74f3cfb665ed818 |

The 206 prior complete readings remain byte-bound to the input and retain
61,376 physical lines. No overlap with the new six. Cumulative owner reading:
212/557 files / 64,350 lines; 345 remain. The larger connected 108-file selection
is still in progress: six read, 102 not yet read. This intermediate source repair
does not shrink that selection or the original goal.

## Seven calibrated open findings

Settled scope: 0 Critical / 6 High / 1 Medium / 0 Low. These are source-contract
and existing-test weaknesses, not executed mutations or demonstrated regressions.
Correction must follow complete relevant admission, causal independent regression
proof, the smallest complete product repair where needed and effective mutations.

### CS01 — High: constructor failure does not release allocated ownership

`ResourceCache.cs:55-60` allocates the observer gate and linked lifetime source
before injected `TimeProvider.CreateTimer`, without failure cleanup. A throwing
timer provider can leave owned lifetime registration/state unreleased. The exact
portable interval policy also needs analysis; no unverified system timer maximum
is asserted here. Acceptance requires the original timer-construction failure,
released owned registration/resources, no usable partial cache and an explicit
portable timer policy. This does not accuse the valid no-op timer fixtures of
being dummy implementations.

### CS02 — High: cancellation and creation-commit boundaries are incomplete

Caller checks occur before potentially held key projection or synchronization;
Add/Remove/Cleanup do not recheck the caller at commit. Initial-only caller
cancellation may be an intentional contract and must be distinguished from
shared creation lifetime. More materially, `ResourceCache.Creation.cs:176-206`
checks creation cancellation before projection, but afterward checks disposal/
explicit invalidation rather than external lifetime cancellation again. Existing
held-projection tests cover Clear's invalidation, not this lifetime boundary.
Acceptance must settle the caller's precise best-effort/admission contract and
causally establish no late lifetime-canceled factory commit, no published index
entry, released produced ownership and the independently expected cancellation.
The comments now accurately describe existing checks; the risk remains open.

### CS03 — High: comparer failure/reentry threatens multi-index consistency

Custom comparers execute inside index dictionaries under the shared lock.
`ResourceCache.Resources.cs:88-101` commits index dictionaries sequentially before
the owning entries dictionary, without rollback if a later comparer throws.
`ResourceCache.cs:134-141` can update entry keys before a new index is published.
Acceptance requires a late independent comparer failure to preserve every old
index/root pairing and ownership, reject unpublished index/orphan key state and
avoid comparer-triggered lock-order deadlock. Caller-owned rejected resources
and factory-owned rejected resources need their distinct disposal outcomes.
Removing the overbroad atomicity comment does not settle these functional cases.

### CS04 — Medium: minimum age has no distinct ordinary expiration effect

For legal `0 <= MinAge <= MaxAge` and monotonic timestamps, creation age is at
least absolute/sliding reference age. When reference age exceeds MaxAge, creation
age already exceeds MinAge. Thus the MinAge check in
`ResourceCache.Resources.cs:129-139` cannot independently change timed eligibility;
capacity eviction explicitly ignores it. Existing equal minimum/maximum boundary
proof also proves maximum age. Resolve this greenfield policy with full feature
equivalence before removing/redefining a public parameter. Do not demand a kill
for an actually equivalent minimum-age mutant or assume all malicious custom
time providers obey monotonic semantics.

### CT01 — High: shared-creation token assertion observes a losing closure

`ResourceCacheConcurrencyTests.cs:263-294`,
`CallerCancellation_CancelsOnlyThatWaiterAndNotSharedCreationAsync`, writes
`ownerTokenCanceled` only in the expected-unused losing factory. Assert.False
therefore does not observe the actual winning creation token. The same independently
known healthy resource reaches the survivor, a real positive proof, but it does
not replace token observation at the held boundary. Acceptance requires actual
winning-token state after caller cancellation, exact canceled caller identity,
one winner/no losing invocation, known survivor identity and bounded failure-safe
release. No replacement test code is authored before owner admission.

### CT02 — High: failure-sensitive bounds and release are not uniform

Concurrency, GenerationAndLocking, Lifecycle and ObserverAndDisposal headers
claim every potentially blocking assertion is independently OperationTimeout-
bounded, but direct pending ThrowsAsync/Get/Clear waits remain. Some held factories,
observers or disposals are released only after successful assertions or after a
first entry wait outside a protecting finally. Assertion failure can then strand
await-using disposal. The local ten-second timeout also needs shared policy
alignment. Require bounded causal waits and unconditional release/drain from
before the first held entry, preserving the primary failure. Continue JR03/MT03/
CR04, not a blanket ban on valid bounded synchronous callback/reentry probes.

### CT03 — High: coverage variant names overclaim unarranged mechanisms

Expiration lines 274, 294 and 311 label ordinary explicit/repeated cleanup and
sequential churn as scheduler rejection, invoke-then-throw or single-flight
handoff. No such scheduler fault or overlapping handoff is arranged. Retired
bucket/rebucket/rollover labels also require current catalog reconciliation.
Observer lines 14 and 73 claim fanout before rethrow, while tests and production
isolate callback failures instead of rethrowing them to the caller. Acceptance
must reconcile current requirement/catalog ownership and observable variants,
or provide actual causal mechanism/fault proof if that feature is still required.
Do not accept catalog green as actual fault coverage or auto-update a baseline.

## Strong adjacent proof and rejected false finding

The review retains real positives: exact 32-contender single creation, winning/
losing counts and known identities; original failure identity with healthy retry;
independent retained multi-index values and key/factory rejection ownership;
absolute/sliding boundary plus next-tick proof with custom timestamp frequency;
known capacity eviction identities and high-churn bounds; old-generation usage/
completion unable to affect the new generation; compensated subscription failure;
causal outside-lock timestamp/detach probes; observed callback fanout after faults
even when logging fails; serialized observer execution and reentry rejection;
deferred child-context mutation after callback exit; held true async disposal
without synchronous fallback; separately observed timer/resource/log failures.
Legitimate no-op timers, ownership spies and expected unused losing factories
are not indiscriminately classified as dummy elements.

The apparent expired-resource leak on a later rejected Add is rejected, not
published as a defect: expiration increments index version, invalidates the
prepared projection, causes release before reprojection, then allows duplicate
rejection on the next pass. The completely read lifecycle regression observes
actual expired disposal and passed in both bound native reports.

No assertion-free methods, disabled assertion conveniences or convenience source
directives are found in this completely read Cache scope. This is not a whole-src
zero-dummy/directive/comment or 100%-test-coverage certificate.

## Fresh applicable verification and honest counter-review accounting

SDK 10.0.302; xunit.v3.mtp-v2 4.0.0; native MTP. Main completely reads the
effective project/configuration inputs and run-tests skill/detection reference.
Known sandbox IPC restriction is handled up front with authorized escalated local
execution, not cleaning, reinstalling SDKs or repeatedly compiling unchanged code.

Release builds use `--no-restore --disable-build-servers -m:1
-p:UseSharedCompilation=false -warnaserror` for the exact Core and Architecture
projects. Fresh DLLs execute through `dotnet exec`, no VSTest separator, with
minimum expected counts 4007 / 439, strict zero-tests policy, fail-skips on,
progress off, native CTRF and results only in the owned temporary evidence root.

| Fresh verification | Observed outcome | Duration | Log SHA256 |
| --- | --- | --- | --- |
| Core Release build | success, 0 warnings / errors; captured terminal exit 0 | 1m 04.11s | 87ba7e33b1b0793829af9cc07d9d955937e3cafa4d9431807df793f31cabacc6 |
| Architecture Release build | completed success log, 0 warnings / errors | 23.03s | 3ea5b69d9931fa2e9017062ecc36efc23d4593eff4a25a6da5e5370bcce93c8c |
| Core native run | 4,007 passed, 0 failed / skipped | 21.345s | 5d6fa71f662804689eb3919243937e7db029f58fe6fdf954cc4b4b2b330be3b3 |
| Architecture native run | 439 passed, 0 failed / skipped | 3m 23.013s | f6a1bd6c421f9d012d869bb6e1dec4494cfec278de415defe5ced694f9ef75f7 |

Fresh Core CTRF SHA256: `ff5d9473fac7f5ea46ea75b3994c54089170c7d263ac11035c73c9172c877e00`.
Fresh Architecture CTRF SHA256: `e3530b04532350af10f6685e79fe6c5d37ded2e46feb4a453aa50226b30308b1`.
Both complete native record sets independently reconcile exactly with their
summaries: no failed, skipped, pending or other records. The bidirectional
`EveryMethodName_MatchesItsAsynchronousContractBidirectionally` record is passed,
duration 158,836 ms. A truncated launch/poll wrapper loses some session outcomes;
they are not invented as exit 0. Exact authorized process inspection plus complete
terminal logs and native reports establish completion, without duplicate runs.

One attempted internal Sol source advisor returns explicit non-completion:
required authority reading not closed, 0/18 productive files read, no advice,
no productive/test writes. It is credited with no code review or independent
acceptance. Main self-review is Author-Red-Team, not external product-team review.
The original goal continues; this is no product semantic blocker.

## Diagnostics, structure decision and next connected work

Owned temporary evidence root:
`/private/tmp/vsb-iteration127-core-composition-read.fSTkRG`.

| Read-only receipt | Exit | SHA256 |
| --- | ---: | --- |
| productive-input-read-bindings.log | 0 | 3b38be9e29f09d8b09bae033f8624e2427a1a4c950dabb70a2da9749bdff3929 |
| exact-comment-and-type-move-equivalence.log | 0 | f0fd3e28ba56699336a3f39a816e356b8476027af19671eba6a66c4a5e65a78d |
| cache-test-read-bindings-and-historical-methods.log | 0 | 9871387424bbacce7560ccdf9b185484ec8b590ad24deec2c23a21fc94e4edae |
| final-reading-and-native-bindings.log, first inventory check | 1 | 927cd4a232a6987573e9c1ba352d6f0e240b9e2d95e302c5f3ab7b376667e8de |
| corrected-reading-and-native-bindings.log | 0 | 1c8af7843ca2e0f3950af61dd7a6baec563ab3ab744c14504b77b1f2d7a39363 |

The first inventory command's unqualified exclude produces an empty Git path
selection and invalid remaining total, despite all 212 read-byte comparisons
and both native validations succeeding. The corrected explicit owner-qualified
glob/exclude returns the exact 557-file input manifest, 212 read and 345 remaining.
The failed receipt is retained, not accepted or used as missing-code/test evidence.
Two guessed nonexistent Cache/Architecture paths are resolved from actual project
inventories. Neither diagnostic warrants a blind clean, SDK change or build replay.
Use physical lines, binary Git equality and explicit owner-qualified glob excludes.

Final handwritten-manifest validation terminates 0: all eighteen productive and
six test rows match actual hashes/physical lines, every receipt hash matches,
the seven severity headings reconcile, earlier history tails are unchanged and
all ten owned files have valid EOF/whitespace. The first table parser incorrectly
reads the trailing empty Markdown cell as the digest; its retained failed receipt
is corrected by stripping the line before splitting, without changing any source
or manifest data. Scoped Git diff whitespace and unchanged Core-test checks are 0.

The actual src-root has only shared `Directory.Build.props`, no direct C# files.
`src/ViciOne.ServiceBus` is the core assembly, not a repository umbrella.
Independent Abstractions/Sagas/JobService/Mediator and other SDK projects stay
siblings. Persistence, Scheduling and Transports remain integration families.
Namespace sharing does not require nesting separate projects under the core's
recursive compile glob. Each type is filed inside its actual owning project.

Secure exactly six source files, this report and three history prefixes with a
normal scoped commit, annotated checkpoint tag, atomic non-force branch/tag push
and independently keyed remote refs. Security completion follows actual receipts,
not this future instruction. Keep unrelated indexed/working changes untouched.

Continue the 102 unread connected selection inputs and all 345 remaining Core
owner files, then finish effective shared-input/parser/GitReadSet admission and
causal test repairs, productive cancellation/comparer/ownership decisions and
effective mutations. Retain all earlier findings. Current global coverage/CRAP,
real persistent/durable sender provider/cloud acceptance, packed count mismatch,
whole-src manual comments, greenfield API/type/file/folder/naming, no-feature-loss
and complete multidimensional A+ review remain in the original active goal.
