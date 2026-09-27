# T52 — scheduling admission and failure ownership

Status: implementation, review, counterprobes, full measurement and audit complete.
T51 is complete and remotely verified at `e0e9d3c38`.

T52 is measured at `ad84a5ac6`: all33 profiles pass13,147 tests without failures
or skips, and four fixture groups exit0. Independent integrity and numerical
audits agree. Lines85,823/93,753 (91.54160%); conservative branches30,992/36,847
(84.10997%); zero CRAP>30. Remaining union5,843 includes generated identities.
The [complete report](product-wide-profile-ad84a5ac6.md) records exact inputs,
reconciled deltas and artifact hashes. Global A+ remains open. Preparation and
checkpoint notes below are chronological, not outstanding claims.

## User requirements and evidence plan

| User requirement | Planned evidence |
| --- | --- |
| "schneide größere pakete" | One connected scheduling admission/failure packet across endpoint commands, published commands, topology resolution and scheduler control operations. |
| "nur hochwertige Tests, die echtes Produktverhalten, Fehlerfälle, Grenzen und Regressionen hart prüfen" | Exact error identity/token, no premature command, no collaborator effects after rejected admission, healthy successor and exact control key/time. Existing success matrices are retained rather than duplicated. |
| "die Microsoft Testskillss sind verpflichtend" | code-testing-agent Research → Plan → Implement inline; one bounded Roslyn pairing; test-gap-analysis with .NET extension; assertion-quality and run-tests for implementation; coverage-analysis for final measurement. |
| "wie immer mit adversal red team reviews" | Read-only scope/implementation review, material isolated counterprobes, immediate restoration and final controls before frozen full33. |

## Bounded inventory

One byte-identical mirror at `/private/tmp/servicebus-t52-pairing` contains138
inputs:74 source files,59 test files and five project files. The mandatory Roslyn
run reports60 paired sources and14 unpaired. Inputs/SHA values, exact pairing
and suggested paths remain in `artifacts/t52-pairing-inputs.json`,
`artifacts/t52-pairing.json` and `artifacts/t52-pairing.log`.

The mirror covers Core Scheduling, scheduling abstractions/contracts, the Quartz
package and their adjacent tests. Static pairing is not runtime coverage:
MessageSchedulerConverterCache is unpaired by name, although the existing
runtime-typed scheduling tests execute it. Its suggested path is
`tests/ViciOne.ServiceBus.Tests/Scheduling/MessageSchedulerConverterCacheTests.cs`;
do not create a redundant internal test just to satisfy that heuristic.

## Existing evidence and scope refinement

RecurringSchedulingCompletionTests already has132 cases: two command routes,
two payload routes, eleven overload forms and three completion outcomes. It
checks exact command type, payload URNs, schedule, destination, initializer
headers, typed/untyped pipes, awaited success, failure identity and cancellation.
That matrix will not be duplicated.

RecurringSchedulerContractTests already checks constructor guards, all40 null
schedule overloads, concrete/declared payload identities, mismatched endpoint
payloads, generic missing publish addresses and initializer behavior.
SchedulerTimeProviderTests checks immediate cancel/pause/resume command timestamps
and keys. Quartz tests already cover recurring trigger replacement, canonical
pause/resume/cancel, idempotent missing targets, invalid commands before scheduler
access, misfire policy and time-zone rejection. Inspect exact failure ownership
before extending the Quartz suite; adding another project alone is not value.

T51 shows the two recurring scheduler files primarily missing rejection branches:
destination, message/values, type, pipe, absent topology, runtime missing publish
address and invalid message contract. The useful contract is rejection before
external or initializer effects, followed by a healthy request on the same
scheduler; bare exception-only enumeration is insufficient.

## Acceptance map — planned names, not verified claims

| Family | Planned test | Distinguishing oracle |
| --- | --- | --- |
| Publish topology | `UnavailablePublishTarget_RejectsBeforeCommandAndAllowsRecoveryAsync` | Generic/runtime/declared target identity; missing topology versus missing address; no command or initializer work; successful later resolution remains independent. |
| Endpoint resolution | `EndpointResolution_OwnsPendingFailureAndCancellationBeforeCommandAsync` | Resolution gate holds operation pending; exact address/token; provider failure/cancellation preserved; no command sent before success. Include scheduling and control commands. |
| Existing trigger integrity | `InvalidRecurringReplacement_PreservesTargetAndNeighborBeforeValidRecoveryAsync` | Invalid cron/time zone cannot change existing trigger schedule, payload or metadata; same-name/different-group neighbor survives; valid replacement subsequently affects only the target. |
| Quartz provider ownership | `SchedulerProviderFailure_PreservesTriggersAndAllowsCommandRecoveryAsync` | Factory or ScheduleJob failure preserves original failure/token and existing trigger state; same boundary subsequently succeeds once. Durable shared-job creation may precede ScheduleJob failure and is not falsely forbidden. |

Read-only selection review recommends these four connected families. Do not add
another full nullparameter/overload matrix or probe-only tests. The existing
guards and completion suite already provide that surrounding contract evidence.
For permanently absent topology, the healthy successor is an explicit send on
the same scheduler. For resolver cancellation, require forwarding and preservation
of the resolver's canceled task, not forced cancellation of a resolver that ignores
the token. These are the implementation acceptance boundaries.

The first implementation is `RecurringControlOwnershipTests`: twelve cases
(cancel/pause/resume × resolver/send failure × exception/cancellation). A resolver
gate and a separate send gate prove pending ownership, exact address/token,
creation timestamp retained across a two-hour fake-clock advance, absence of
premature commands and exact provider failure. A second call through the same
scheduler proves successful recovery with a different key/group/token. Cleanup
releases both gates and observes the original task with a configured timeout.
The variant is requirement-bound but not yet built/run. No product source changed.

These are static hypotheses, not empirical surviving-mutation claims. Probe
forwarding or ownership at material boundaries, rather than deleting redundant
internal guards. No product defect or A+ clearance is claimed at this stage.

The combined packet now contains32 cases in three files. Publish admission covers
two command routes, four contract/initialization forms and two missing-target
modes. It observes initializer property access, endpoint resolution, user pipe
execution, exact contract URNs and healthy successor metadata. Quartz replacement
tests use two real stored triggers with the same schedule name in different groups.
Their immutable snapshots retain all job data, cron, time zone, retry policy,
misfire behavior, next fire time and keys across rejection. A later valid command
must replace only the target. Factory/store failures are injected through delegating
interfaces before the real scheduler and executed through a real bus consumer;
tokens, original exception, success/fault observations and successful writes are
observed independently. No build or execution success is implied by this inventory.

## First combined checkpoint

Core builds with zero warnings/errors and passes28/28 new cases, no skips
(`artifacts/t52-core-build.log`, `artifacts/t52-core-focused.log` in the gate
checkout). Quartz initially exposed three test-authoring errors: typed cron
misfire enum, collection-size analyzer and nullable retry policy. Manual fixes
retain a non-null policy assertion; the corrected Quartz build passes without
warnings/errors (`artifacts/t52-quartz-build-final.log`). Quartz passes4/4 without
skips (`artifacts/t52-quartz-focused.log`), giving32/32 new cases overall.

Independent read-only implementation review found an asynchronous-failure oracle
gap: the ScheduleJob proxy originally threw synchronously, which would not detect
a missing await. It now returns a failed ValueTask of the exact API return type.
Both Quartz recovery paths also verify the exact replacement-version header,
closing the identified stale-header gap. The reviewer found no other concrete
static blocker. The corrected tests pass the focused checkpoint.

## Assertion-quality and gap review

The Microsoft assertion-quality skill and .NET extension were applied to all four
new methods and their shared assertions. None is assertion-free, trivial-only or
self-referential. All four combine failure handling, negative side-effect checks
and a successful subsequent operation. No sleeps or polling establish correctness.

| Test method | Distinguishing assertions |
| --- | --- |
| `EndpointResolution_OwnsPendingFailureAndCancellationBeforeCommandAsync` | Pending state at two separate gates; original exception/token; exact command type/key/time; no early command; successor on the same instance. |
| `UnavailablePublishTarget_RejectsBeforeCommandAndAllowsRecoveryAsync` | Correct exception class and contract lookup sequence; zero initializer/pipe/endpoint effects before admission; exact payload/URNs/destination/token/header after recovery. |
| `InvalidRecurringReplacement_PreservesTargetAndNeighborBeforeValidRecoveryAsync` | Exact command-ID fault observation; immutable full trigger snapshots; exact two-key set; correct replacement cron/time/payload/header; unchanged neighbor. |
| `SchedulerProviderFailure_PreservesTriggersAndAllowsCommandRecoveryAsync` | Original provider exception identity; inbound token forwarded at both seams; zero successful writes on failure; retained trigger snapshots; exactly one successful replacement and matching payload/header. |

The real bus creates its receive token, so the Quartz provider test compares that
token with factory/scheduler tokens, not the unrelated sender token. Provider
failure does not promise that shared durable-job creation has no effects. The
stored shared job already exists in these replacement journeys. Large future dates
prevent delivery while snapshots are compared; execution uses configured timeouts.

The first isolated counterprobe removes awaiting of recurring Quartz ScheduleJob.
It builds and yields exactly1 failing/3 passing cases: the provider-store-failure
case detects an incorrect successful consumption. The production file is manually
restored and its original SHA-256 verified:
`788fd94a01b204b3ae86ee243c3b95c604b5e2d8a9be2c09636f3acd4c77232a`.
This evidence is at `artifacts/t52-quartz-mutation-build.log` and
`artifacts/t52-quartz-mutation.log`; restored controls still follow all probes.

The second probe removes awaiting only from the Cancel control send. Exactly2/12
cases fail at the premature-completion assertion; the other10 pass. The third
probe substitutes a fallback URI for rejected generic endpoint publish resolution:
exactly2/16 fail because the expected rejection no longer occurs;14 pass. Each
probe builds successfully and runs independently. The endpoint source is manually
restored after each and matches its original SHA-256:
`5fa2d65cf46ba4aead08a6a36708de97a3a2c8e634d5d4162e15dcf1e0397952`.
Logs are `artifacts/t52-control-mutation*.log` and
`artifacts/t52-admission-mutation*.log` in the gate checkout. Both affected product
files match their original hashes before the combined restored controls start.
The reviewer confirms both corrected Quartz oracles and no remaining concrete
static blocker; full measurement remains pending.

Final restored controls pass Core160/160 (including the existing132 completion
cases) and Quartz19/19 (including existing scheduler-command integration tests),
zero skips. Both restored builds have zero warnings/errors. Evidence:
`artifacts/t52-core-restored*.log`, `artifacts/t52-quartz-restored*.log` in the gate
checkout. All5,887 source/test paths are byte-identical between MAIN and GATE;
the hash inventory is `artifacts/t52-main-gate-inputs.json` in MAIN. No product
source edit remains. Only the frozen product-wide measurement can update coverage
or CRAP claims; T51 remains the latest complete metrics baseline.
Both project-scoped `dotnet format whitespace --verify-no-changes --no-restore`
checks exit0 for the three added test files without rewriting any file. Logs:
`artifacts/t52-core-format.log` and `artifacts/t52-quartz-format.log` in GATE.

## Execution order

1. Complete read-only selection review and exact existing-test comparison.
2. Implement the connected families manually and bind requirement variants.
3. One focused validation checkpoint, fix concrete failures, read-only test review
   and assertion-quality assessment; no repeated full-product measurement.
4. Isolated counterprobes, source-hash restoration, final focused controls and
   verify-only formatting; freeze implementation.
5. One full33 measurement including providers, independent integrity/numerical
   audit, documented limits, changelog/CHANGELIST and authorized push.
