# T54 — transport ownership and message isolation

Status: research and implementation in progress; no completion or new coverage
claim. Baseline publication `f492b3ed6`, measured implementation `f41b145f6`.

## User requirements and shared acceptance

- "schneide größere pakete": combine the families below before one full33 run.
- "nur hochwertige Tests, die echtes Produktverhalten, Fehlerfälle, Grenzen und
  Regressionen hart prüfen": distinguish effects, identities, error ownership
  and successful continuation; no wrapper-forwarding matrix for coverage alone.
- Microsoft code-testing-agent Research → Plan → Implement runs inline because
  the named generator is unavailable. One find-untested-sources Roslyn pass used
  327 byte-identical inputs across the two transport source packages and three
  test projects:244 sources,78 tests,five projects;93 paired,151 unpaired.
  `artifacts/t54-pairing-inputs.json`, `t54-pairing.json` and `t54-pairing.log`
  retain inputs/results. Static pairing is not runtime coverage evidence.
- Apply test-gap-analysis and assertion-quality before final acceptance, run-tests
  before execution and coverage-analysis for the one final complete measurement.
- Read-only adversarial review, isolated counterprobes with restored hashes,
  focused controls, exact frozen measurement, independent audit and authorized
  publication remain mandatory. Protected TestResults/ and review/ stay untouched.

## Combined implementation map

| Family | Planned test / complete oracle |
| --- | --- |
| Event Hubs configuration ownership | `RejectedReplacement_PreservesEffectiveClientsAndDeliveryAsync`: reject second host/storage/options, never execute discarded callbacks, retain effective settings and deliver through the original factory. |
| Event Hubs configuration repair | `InvalidConfiguration_CanBeCompletedBeforeFirstBuildAsync`: enumerate missing-host/storage failures; invalid calls must not consume the one-time slot; valid completion delivers. |
| Event Hubs endpoint identity | `ConsumerGroups_AreIndependentAndDuplicatePairsPreventBuildAsync`: same hub/different group is valid; exact duplicate pair prevents public construction with diagnostics and no endpoint starts. Positive control uses a fresh factory. |
| Consume-context deferred producer success | `DeferredResolution_PreservesEnvelopeAndAwaitsDeliveryAsync`: deterministic provider and send gates, inherited metadata plus explicit overrides, no effects before resolution, no premature success. Compare singleton/batch and initialized payload routes. |
| Consume-context deferred producer failure | `DeferredFailure_PropagatesWithoutSendEffectsAsync`: provider failure/cancellation ownership across the same routes, no accidental pipe/send invocation. Validate existing tests before adding. |
| ActiveMQ message isolation | `NativeGroups_RemainIsolatedAcrossReusedEndpointAsync`: A/7, ungrouped, B/19, A/0 through one endpoint/destination, exact native and public metadata, payload identity and diagnostic properties across OpenWire/AMQP/Artemis. |

Existing EventHubBatchAndReliabilityTests covers normal options application;
EventHubEndpointAndBusBoundaryTests covers normal dynamic/multibus delivery.
These are retained, not duplicated. Deferred-provider work must examine the
wrapper specifically rather than repeat batch sender tests.

Read-only reviewer `/root/outbox_proof_redteam` confirms the ActiveMQ dictionary
roundtrip tests do not establish native broker mapping or message isolation.
Do not claim native producer identity from a reused endpoint, global group order,
fixed consumer assignment, or a distinction between absent and zero receive
sequence. Compare observations by payload ID. Native getters must be read directly
from the provider message rather than through the product extension under test.
Candidate counterprobe: omit SetGroupSequence at send; positive sequences fail
while zero/absent controls remain. This is not yet empirically verified.

## Completion state

ActiveMQ isolation (one theory,three cases) and the two Event Hubs ownership/repair
facts are implemented and requirement-bound. Both projects compile in the existing
isolated GATE checkout with zero warnings/errors. Logs there:
`artifacts/t54-activemq-build.log` and `t54-eventhubs-build-corrected.log`.
The initial Event Hubs build failed because the test used the old Pipe.ExecuteAsync
name; changing it to the existing ExecuteAwaited API corrected the test. The failed
attempt remains in `t54-eventhubs-build.log`; no product change was needed.

All six families now implement22 new cases in three files, with six requirement
bindings. Endpoint identity verifies public construction, not live delivery through
the emulator's undeclared cg2 group. The other configuration tests independently
perform real delivery and checkpoint readback. Namespace now matches the fixture
SDK client's namespace exactly. Deferred producer tests use a real in-memory
consume context and an explicitly gated provider seam. They prove wrapper routing,
header transfer and task/error ownership, not initializer behavior or broker I/O.

Read-only review found two test weaknesses: missing failure cleanup of deferred
gates/tasks and insufficient overload discrimination. Both are corrected with
bounded waits/terminal observation and exact parameter/generic type assertions.
An intermediate cleanup-placement error was corrected before runtime execution;
the reviewer confirmed the final block preserves all success assertions.
Test-authoring compile errors (typed versus advanced consume context and missing
explicit cancellation tokens) were corrected. Final combined Event Hubs build
`artifacts/t54-eventhubs-cleanup-build.log` in GATE exits0, zero warnings/errors.

The first focused fixture attempt failed before test execution because Docker
could not bind the Event Hubs config from the temporary checkout. Its log remains
in GATE `artifacts/t54-focused.log`; no test pass is claimed. The replacement
fixture starts from MAIN and invokes the same isolated GATE test binaries through
`artifacts/t54-focused.py`. MAIN log: `artifacts/t54-focused-main.log`.
Runtime validation, counterprobes and publication remain open. T53 remains the
authoritative coverage measurement; no T54 coverage measurement has started.

## Assertion quality and mutation reasoning

Microsoft assertion-quality and test-gap-analysis with the .NET reference were
applied to the six new methods and their helpers. None is assertion-free or
trivial-only. Native roundtrip equality is accompanied by independent provider
field checks, distinct payloads, nondefault/default transitions and cardinality.

| Family | Distinguishing assertions / candidate fault |
| --- | --- |
| Configuration replacement | Exception category/message, unchanged effective namespace/identifier, zero discarded callbacks, successful delivery and persisted checkpoints; removal of a once-only guard must fail. |
| Configuration repair | Exact fully enumerated validation keys before/after rejected input, then real delivery; consuming a slot before validation must fail. |
| Endpoint identity | Duplicate diagnostic plus zero endpoint callbacks, versus two distinct builds; grouping by hub alone must fail the positive control. |
| Deferred success | Negative pending-state checks at two gates, exact forwarded overload/input/token, independent inherited/overridden metadata and terminal completion; missing downstream await is a material counterprobe candidate. |
| Deferred failure | Same exception or exact canceled token, zero pipe effects and one resolution; swallowed resolution failure must fail. |
| Native grouping | Public/native group values, payload identity, absence of stale diagnostic fields, exact set cardinality; omitted native sequence setter is a material counterprobe candidate. |

Initial focused controls pass ActiveMQ7/7 and EventHubs16/16 without skips; MAIN
`artifacts/t54-focused-main.log` and its fixture evidence exit0. Four delayed
provider-send-failure cases were then added to strengthen detection of missing
awaits independently of scheduling. Their DeferredProducer controls pass16/16
in GATE `artifacts/t54-deferred-control.log`.

The first isolated probe removes only the singleton downstream await. It builds
cleanly and fails1/16 (the singleton delayed-failure case), with15 passing controls.
This is empirical detection, not a complete mutation score. Source was immediately
restored manually, matching SHA-256
`d0fed0b5ae3aa7f29b2ee394abb7f93c422d81b2275a555dc98c2845c0f3427a`.
GATE retains `t54-deferred-mutation-build.log` and `t54-deferred-mutation.log`.
Other candidates remain static until executed. No global A+ acceptance is claimed.

The second isolated probe omits the native group-sequence setter. All three
OpenWire/AMQP/Artemis cases fail with expected7/actual0; three existing header
controls pass. MAIN `artifacts/t54-group-mutation.log` retains the result and
fixture cleanup. Source is immediately restored manually, matching SHA-256
`3978e58ca64adf323782e4e1d62978b4b8c8e2659a9df12df01b4733b2474b54`.

Both restored builds exit0 without warnings/errors. Combined restored controls
pass27/27:ActiveMQ7 and EventHubs20, zero skips. MAIN `artifacts/t54-restored.log`
and `t54-restored-fixture` retain the successful fixture and four broker logs.
All5,891 source/test inputs match MAIN/GATE (`artifacts/t54-main-gate-inputs.json`).
Verify-only formatting found a switch-expression indentation issue, corrected
manually; both final Event Hubs and ActiveMQ format checks exit0 without edits.
Logs in GATE are `t54-eventhubs-format-final.log` and `t54-activemq-format.log`;
empty logs alone do not prove exit status, which was observed in tool sessions
76385 and59250. This whitespace-only change
does not alter the prior runtime control logic; the exact frozen full33 will build
and test the final source. No optional repeat of focused controls is required.
The separate reviewer confirms both deferred oracle corrections and the additional
late provider-failure cases. Full measurement, independent audit and publication
remain pending.
