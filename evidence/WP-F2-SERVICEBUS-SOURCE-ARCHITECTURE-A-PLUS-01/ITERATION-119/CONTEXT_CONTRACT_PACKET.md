# Context contracts and causal activity completion

Date: 2026-09-15. Corrective checkpoint under unchanged bound order
`PO-2026-09-08-SERVICEBUS-SOURCE-ARCHITECTURE-A-PLUS-01`; slice SHA256
`5a9cd605540cd826a756e24ca3f813cb3ce9a1eaf1fc4f881b6cecfbee9c7199`.
The original autonomous Greenfield A+ goal remains active. This packet is not final
iteration119, entire-product acceptance, complete source reading or independent
external red-team acceptance.

## Secured input and source ownership

Input commit `344cbfd22d7f1338991049645ca7750329d7ff8b` and annotated tag
`servicebus-a-plus-iteration-119-acquired-ownership-checkpoint-2026-09-15` are
verified on origin before executable changes. Normative instruction hashes and the
bound order remain unchanged. Protected untracked `review/` and `TestResults/` are
not scanned, edited or staged. No force-push or new package dependency.

`src/ViciOne.ServiceBus` is the Core project, not a product-wide container.
Independently compiled contracts and capability projects remain siblings under
`src`; persistence, scheduling and transport families retain their grouping.
Sharing a product namespace does not imply ownership by the Core assembly. Moving
other SDK projects below Core would require recursive-source exclusions and obscure
optional assembly dependencies. The source tree contains33 project files and no
loose `.cs` files directly under `src`. Type/file/function alignment is assessed
within each project, not by merging unrelated compilation boundaries.

## Personal complete-file reading and manual comments

The lead personally reads these17 complete runtime source files, counting a file
with multiple generic arities once:

- Abstractions: `BasePipeContext`, `ScopePipeContext`, `ListPayloadCache`, `IPayloadCache`,
  `RetryObservable`, `Connectable`.
- Core: `RollingTimer`.
- Sagas: `SendSagaPipe`, `InMemorySagaRepositoryContext`, `InMemorySagaConsumeContext`,
  `StateMachineSagaMessageFilter`.
- Testing: `BaseSagaTestHarness`, `ISagaTestHarness`, `TestSagaRepositoryDecorator`,
  `TrackedActivity`, `TelemetryActivityExtensions`, `ActiveTestHarnessExtensions`.

Affected existing/new test and clock-helper files are personally understood before
manual edits. The payload contexts, saga pipe/repository, tracker, telemetry
extensions and RollingTimer comments describe current behavior. In particular,
in-memory undo does not restore mutations to retained saga objects, and the empty
pipe probe does not produce diagnostics. RollingTimer permits a later restart after
disposing its current timer; its former rollover assertion is removed. Telemetry
cancellation comments no longer promise cancellation of a callback that receives
no token. No source, test, comment, namespace or folder generator is used.

The supplied parse-only source/test pairing analyzer uses a temporary symlink view
of Abstractions only, excluding protected paths. Its710sources/101test files contain
217paired and493unpaired sources. Base has six referring test files, Scope one;
the new Base class deepens already-paired behavior. Pairing is not coverage or proof
that every API parameter is tested. This17-file read does not close the complete
product source/type/file/folder inventory.

## Findings and implementation

### Required payload contracts

Both Base cache-taking constructors reject a null required cache. Base and Scope
reject a null runtime payload type with exact `payloadType` parameter diagnostics.
Get/add/update factories are validated in declaration order before self, local or
parent fast paths. Invalid calls invoke no delegate and retain payload identities.
Required factories cannot be null merely because the selected fast path does not
invoke them; optional null/empty initial payload arrays remain valid.

All six Base construction paths retain token/cache/payload identity, initial-array
snapshot ownership and lazy empty-cache construction. Compatible self takes
precedence over caches. Scope lookup remains self → local → parent; additions and
replacements remain local, including absent-type recovery after thrown/null adds.
Parent objects remain shared references, not deep clones. Storage isolation does
not prevent a user callback from mutating a shared object.

Base has9 test methods/30 cases; Scope10/25, including its two original methods.
The packet adds17 manually authored methods/53 cases; Abstractions696 →749.
No old test is deleted or skipped. The first internal review identifies a genuine
absent-add isolation gap; success and thrown/null-add recovery oracles close it.

### Saga completion milestone

The first complete Core run exposes a genuine test synchronization gap. The testing
repository decorator records consumed observation before `SendSagaPipe` performs
the eventual delete. Consumed/finalized/published observations therefore do not prove
that the saga has left the repository. The correlated scheduling test now awaits
`WaitForSagaRemovalAsync` and asserts its exact correlation ID before retaining
the existing repository-null assertion. Deadline, response, identity, no-early-
delivery and single-delivery assertions are retained. Saga runtime behavior is
unchanged. A separately compiled no-delete mutation fails the real removal milestone.

### Activity monitoring and fixture isolation

The next full Core run passes scheduling but reports one request-monitoring timeout
and two later sampling failures in the serialized global-listener collection.
An outer `wait.WaitAsync` timeout does not cancel the underlying fake-clock task.
Its monitor/listener can remain live and sample later tests. The request fixture
previously waited for the first receive and any timer change, not complete trace
quiescence. A later related span can move that single fake-time deadline. This is a
source-derived ordering explanation, not a captured execution trace or a declaration
that the original timeout was harmless flakiness.

Four new controlled-activity tests causally reject actual tracker behavior:
unrelated stops restarting idle, late related starts leaving an old idle deadline,
idle extending the original maximum duration and idle starting before action
completion. The tracker now serializes active-related-span identity, action
completion, idle start and monotonic elapsed-time decisions. It schedules the
earlier of continuous idle and the original maximum deadline. Related starts
invalidate idle; unrelated traces do not affect it. Queued callbacks recheck the
current state/deadlines; disposal prevents rearming and releases the global listener.
Unused private span metadata is removed; trace identity and observable capabilities
remain available.

Additional controlled cases prove queued stale callbacks, callbacks after disposal,
and timer-creation failure preserving the exact exception, ambient activity and
listener release. The repeated internal review finds a real initialization defect:
a second listener can open a child inside the root's synchronous start callback,
before the previous `StartActivity` assignment makes that root visible to the tracker.
The eighth test reproduces it. `CreateActivity` assigns the unstarted root before
`Start` invokes callbacks, allowing those real children to be counted. The lead
checks the actual installed .NET10 reference XML for this unstarted-activity contract.

The request fixture now waits for both request and response receive milestones plus
the actual idle due-time transition; publish/send require their receive milestone.
Bounded fake-clock operations have dedicated cancellation and are drained before
harness teardown even after assertion failure. Unexpected additional drain failures
are attached to an existing primary failure rather than replacing it. All exact
tick-boundary, response/correlation and sampling assertions remain intact.
Eight new tracker methods/cases raise Core3,820 →3,828. The four telemetry classes
contain30 executed cases; this is not all-product API coverage.

| User requirement / invariant | Evidence |
|---|---|
| “features dürfen nicht verloren gehen” | Existing construction, identity, cancellation, payload resolution, scheduling and telemetry assertions are retained |
| “den kommentar selbst zu schreiben ohne scripte” | Complete affected source read and manually authored functional comments; no generator |
| “aber du muss das entscheiden” | Independent SDK project boundaries retained; provider families stay grouped; rationale in `docs/api-surface.md` |
| Test effectiveness | Causal behavioral failures and nine separately compiled, killed and exact-byte-restored mutations below |

## Executed causal and intermediate proof history

All accepted red/mutation runs first compile successfully with zero warnings/errors;
their failures are runtime behavioral assertions, not stale binaries or compiler errors.

| Phase | Passed / failed / total |
|---|---|
| Unchanged Base contract red |22/8/30|
| Unchanged Scope contract red |15/7/22|
| Initial full Abstractions green |746/0/746|
| Expanded foundation green |55/0/55|
| Restored full Abstractions |749/0/749|
| Initial Core, removal-milestone discovery |3819/1/3820|
| Corrected focused scheduling |3/0/3|
| Restored Core, telemetry discovery |3817/3/3820|
| Unchanged tracker causal red |0/4/4|
| First corrected telemetry classes |29/0/29|
| First complete corrected Core |3827/0/3827|
| Constructor-child regression before correction |7/1/8|
| Corrected complete telemetry classes |30/0/30|

Every phase has zero skips. The initial interior-wildcard filter is rejected by the
runner with exit5 before tests execute; corrected multiple exact `--filter-class`
values are used. This parser rejection is not causal red or a mutation kill.
An approval-side model-capacity error occurs before one build starts; read-only
safety/source checks and a legitimate unchanged-scope retry resolve it. It is not a
product compilation defect or permission bypass. Builds/MTP use the established
escalated local path because sandbox IPC otherwise fails; no test is weakened.

## Individually compiled mutations

| Deliberate counterchange | Killed cases | Suite total |
|---|---|---|
| Omit Base cache+token required-cache validation |1|55|
| Omit Base required get factory validation |2|55|
| Omit Scope required get factory validation |3|55|
| Redirect absent Scope additions to parent storage |3|55|
| Skip actual in-memory saga deletion |1|3|
| Restart idle on an unrelated activity stop |1|8|
| Complete a queued callback without state/deadline rechecking |1|8|
| Allow idle to extend the original maximum deadline |1|8|
| Assign monitor root only after synchronous start callbacks |1|8|

Each run exits2 with the specified behavioral failures and zero skips. Each source
restoration is checked by exact SHA256 before the next mutation. All four source
files touched by these mutations are restored before the final proof build.
This is nine selected effective mutations, not a global mutation-score assertion.

## Final proof and raw artifact binding

Final restored Core build: zero warnings/errors,7.12s. Final restored Abstractions
build: zero warnings/errors,1.39s. Complete Core passes3,828/3,828 in31.706s;
Abstractions749/749 in1.656s, zero failures/skips. Requirement projection gates pass
in those complete hosts; manually maintained catalogues contain2,835 and555 unique
tuples. Targeted Product and Unit whitespace verification includes all seven changed
source and seven changed test/helper files, both exit0 without edits. The known
workspace-load warning remains explicit; Git whitespace checks pass. No executable
edits occur during builders, test hosts, formatters or live reviewer freezes.

Native explicit coverage profile is unchanged: source-only, test assemblies false,
auto-property skipping false, no source/attribute exclusions added. The full Core
host's loaded source graph measures49,358/60,944 lines80.9891% and16,881/23,106
branches73.0589%. The Abstractions host separately measures5,333/8,310 lines64.1757%
and1,863/2,996 branches62.1829%. These overlapping host graphs must not be summed or
presented as entire-product/provider coverage. No new CRAP analysis or all-product
100% test assurance is claimed. The17 complete reads and25 new test methods/61 cases
advance the original goal; they do not close its remaining gates.

Reproduction uses `dotnet build <test-project> --configuration Release --no-restore
--disable-build-servers -m:1 -v:minimal`, then freshly built direct MTP executables
`artifacts/sdk/bin/ViciOne.ServiceBus.Tests.Unit/release/ViciOne.ServiceBus.Tests`
and `artifacts/sdk/bin/ViciOne.ServiceBus.Abstractions.Tests/release/ViciOne.ServiceBus.Abstractions.Tests`.
Full runs require at least3,828/749 tests, explicit temporary results directories,
`--report-xunit-ctrf`, and `--coverage --coverage-settings tools/ci/coverage.settings.xml
--coverage-output <name>.cobertura.xml --coverage-output-format cobertura`.
Focused filters use multiple exact `--filter-class` values, not interior wildcards.
No root protected results directory is used.

Following SHA256/path bindings are byte-verified before commit. Temporary raw
reports are local diagnostic artifacts; reproducible code/tests and their binding
report are committed. Source rows identify final restored bytes, not mutant binaries.

```text
2303156020d82484c5e11820816eeea2ba27d0699ce51ba56a38fdf93cbfc586  src/ViciOne.ServiceBus.Abstractions/Middleware/BasePipeContext.cs
4432944f0e99e03e4f90806ca193c54e222723da3b33f0d438e13b0b8c83fcdd  src/ViciOne.ServiceBus.Abstractions/Middleware/ScopePipeContext.cs
4dc805b383f74a9a0bea1ca7027761a8e7fc4c7220ee39b397f9a768fbcdfe6f  src/ViciOne.ServiceBus.Sagas/Saga/InMemoryRepository/InMemorySagaRepositoryContext.cs
232a80b1e38b9277c28f389ade998cb71d0843b666801a3f3631d37e526e940c  src/ViciOne.ServiceBus.Testing/Diagnostics/TrackedActivity.cs
0a767ff690aa675fd17469db8b0f66c7dbbab08b7520f66ff5ae4c2b810f2c01  tests/ViciOne.ServiceBus.Tests/Testing/Diagnostics/TrackedActivityTests.cs
0dce53c3d71d4476a123801f91d82cda6901476970a4174f4c8eb11563a63e5d  /private/tmp/vsb-iteration119-context-base-red/base-red.ctrf.json
c3eef1b57c97104bd2703eb70f0d4047e64069ce68ad537e94ab5a8e92be0207  /private/tmp/vsb-iteration119-context-scope-red/scope-red.ctrf.json
27d6dc3de7c113e6b9170f023517b0919a8892d69762b7f38b0fb96bbea264d0  /private/tmp/vsb-iteration119-context-core-final/context-core-final.ctrf.json
46b77e1cb41bf3e18d147f10458ff6f93d90a8d4ec8bd35c99444dbf8d4092e7  /private/tmp/vsb-iteration119-context-core-restored/context-core-restored.ctrf.json
556b5de1f1c3e747d8247da01507e6dcced3231e0498242e210995d3fad58afa  /private/tmp/vsb-iteration119-context-telemetry-red/telemetry-red.ctrf.json
6ac6404a0e358ed26a4dfa0534ed8dc0a32790d40675313d393d8aed5f3775a0  /private/tmp/vsb-iteration119-context-telemetry-startup-red/telemetry-startup-red.ctrf.json
43fd29536426ba2bd6c92510bd277a5ec4db7a872c404ff96f17d85cdc8354af  /private/tmp/vsb-iteration119-context-telemetry-startup-green/telemetry-startup-green.ctrf.json
59968c767eecf0a43e8776fdf9036e4a9f60353ee970153676bb0e1413a9ba8f  /private/tmp/vsb-iteration119-context-mutant-cache/mutant-cache.ctrf.json
b08ecafda312f488217127e10cc7fe1fa858dee96cf997f596012466221bdbd4  /private/tmp/vsb-iteration119-context-mutant-self-factory/mutant-self-factory.ctrf.json
5c918d9fce097a9b213065a8086571f6a21b9dfa4055d5bbcee181546eefeeff  /private/tmp/vsb-iteration119-context-mutant-scope-factory/mutant-scope-factory.ctrf.json
e82bd5f103f492e09e378c625e3999fd30e672f10cbe487aae35789092e67993  /private/tmp/vsb-iteration119-context-mutant-parent-write/mutant-parent-write.ctrf.json
dd97f622147320ea9e80d8e850b2a5a65d9b61fd5d29d352ef952adcb5772dc2  /private/tmp/vsb-iteration119-context-mutant-saga-delete/mutant-saga-delete.ctrf.json
8d3f3992bc06f263c42fc87f0be3692a2674f8352b1a02f8c20d8fad0a931673  /private/tmp/vsb-iteration119-context-mutant-unrelated-stop/mutant-unrelated-stop.ctrf.json
9d6e315a90c78bb029b072d92a7344b0743aec89b3cd3b3d9b0943a9abe4b068  /private/tmp/vsb-iteration119-context-mutant-queued-idle/mutant-queued-idle.ctrf.json
d3c450ccecb9b1f69a4bd08f5fd43394741c55a1d8d5f36e07e292b2cce7b377  /private/tmp/vsb-iteration119-context-mutant-extended-maximum/mutant-extended-maximum.ctrf.json
160985e074599f696cfb7907a75581e39554ec67e7b505808ea3270bbd694a36  /private/tmp/vsb-iteration119-context-mutant-root-start/mutant-root-start.ctrf.json
f9af2382d1d7226fa49e352fdbe234caf939160185a00dd28877e08fcea9ea4a  /private/tmp/vsb-iteration119-context-core-telemetry-final/core-telemetry-final.ctrf.json
bd9536e0b61093d6d4bb2f06e13c21509059507dc7a23308dfee906e724e687a  /private/tmp/vsb-iteration119-context-core-telemetry-final/core-telemetry-final.cobertura.xml
5d2599e9d90ff128fbf644572e36fb91e05ff5752cfdb2ad08de8cd7021880c0  /private/tmp/vsb-iteration119-context-abstractions-restored-final/abstractions-restored-final.ctrf.json
eca44eaa33552f78d8b8217e6284fedfd2a7cf564d89e955f130759bc1b7a760  /private/tmp/vsb-iteration119-context-abstractions-restored-final/abstractions-restored-final.cobertura.xml
```

Checkpoint name:
`servicebus-a-plus-iteration-119-context-contracts-checkpoint-2026-09-15`.
Publication is reported only after normal atomic branch/tag push and exact remote
commit/tag-object/peeled-target verification. The original goal remains active.

## Explicit remaining work

- Complete legitimate retry null-fault-task/getter/probe/constructor boundaries and
  reassess the13 prior emitted kernel gaps; do not fabricate inactive private states.
- Continue personally reading remaining context/cache subclasses and the entire
  source inventory; this packet is not completion of the whole Abstractions project.
- Reassess in-memory saga save/update/undo semantics against all callers and required
  persistence capabilities; accurately documenting no rollback is not product-wide
  transactional acceptance. Review broader required constructor/context inputs.
- Reassess RollingTimer's public disposal/restart and trigger-visibility contracts
  against all callers; only its comments change here.
- Close actual delayed/RabbitMQ provider publishing/topology cancellation and execute
  proportional real local-provider acceptance. Recording fixtures are not providers.
- Complete current all-host, package/API/journey/isolated-consumer, full-source and
  wider coverage/architecture gates before final119 or whole-goal A+ acceptance.

Internal gpt-5.6-sol reviewers are explicitly authorized but not an independent
external product team. Frozen foundation, scheduling and telemetry reviews are
read-only; they execute no tests. Findings are accepted, causally tested and reviewed
again. Every live freeze is explicitly released before executable edits.
