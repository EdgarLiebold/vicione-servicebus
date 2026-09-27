# T48 — MultiBus host, health and scheduler ownership

Status: implementation verified; final measurement pending. Baseline: completed and pushed T47,
`50bd0a527e9be580daa84ba3eaa034107f2811d9`. No T48 acceptance claimed.

## Requirements and bounded research

Preserve the PO's larger packages, strong product-contract tests, mandatory
Microsoft test skills, adversarial read-only review, isolated counterprobes,
one final exact-commit full33 measurement, changelog and authorized push.

This packet follows bus ownership from Generic Host registration through health
reporting and scheduler resolution to actual transport delivery. Six primary
sources: DependencyInjectionHostingExtensions, ViciOneServiceBusHealthCheckOptionsExtensions,
ConfigureBusHealthCheckServiceOptions, BusHealthCheck,
MessageSchedulerRegistrationExtensions and DelayedMessageSchedulerRegistrationExtensions.
Supporting bus composition and scheduler code is read as needed.

The single Roslyn pairing pass used 74 copied inputs: six sources, 66 tests and
two project files. Three paired sources: hosting and delayed-scheduler extensions
with DependencyInjectionConfigurationContractTests; BusHealthCheck with
BusHealthCheckTests. Three unpaired sources and exact suggestions:

- ViciOneServiceBusHealthCheckOptionsExtensions:
  tests/ViciOne.ServiceBus.Tests/Configuration/DependencyInjection/ViciOneServiceBusHealthCheckOptionsExtensionsTests.cs
- MessageSchedulerRegistrationExtensions:
  tests/ViciOne.ServiceBus.Tests/Configuration/MessageSchedulerRegistrationExtensionsTests.cs
- ConfigureBusHealthCheckServiceOptions:
  tests/ViciOne.ServiceBus.Tests/Monitoring/Health/ConfigureBusHealthCheckServiceOptionsTests.cs

This is a static pairing heuristic, not runtime absence evidence. Extension calls
are missed; existing BusHealthCheckTests exercise default registration already.
Existing BusHealthLifecycleTests exercise aggregate healthy reports and single-bus
transitions. MultiBusScopeIsolationTests already prove consume-scope separation.
The new tests belong beside existing behavior journeys in DependencyInjection.

## Acceptance map

| Contract | Planned evidence |
| --- | --- |
| HostBuilder preserves callback context and each bus identity, including explicit instances | MultiBusHostOwnershipIntegrationTests: actual Generic Host start, independent delivery, stop |
| Health options, tags and failure floors belong only to their bus | same fixture: named per-entry reports before start, after start, after stopping one bus, positive surviving-bus delivery |
| Invalid typed/default health options fail startup with the correct owner and property | same fixture: invalid name/status/tag cases and valid companion options |
| Scoped endpoint/publish schedulers retain owning bus, destination, clock and cancellation identity | MultiBusSchedulerOwnershipIntegrationTests: actual commands received on distinct bus transports |
| Native delayed scheduler remains scoped and uses the owning bus | same scheduler fixture: delivery and canceled admission controls |

No duplicate overload-only matrices. Initial read-only scheduler review runs
alongside implementation. Focused builds/tests follow meaningful code batches;
full measurement only after complete implementation and review corrections.
Survivors are not reported without executable counterprobe evidence.

Global A+ remains open; first source reading remains complete. All-repository
Roslyn API/comment audit remains deferred until coverage/CRAP completion.

## Implementation and review checkpoint

Seventeen cases are implemented, not yet accepted:

- `HostLifecycle_ReportsEachBusAndKeepsTheOtherBusOperationalAsync`: four
  combinations of dynamic/explicit typed instance and default/custom health
  settings. Both buses carry the same message contract. Each bus is stopped in
  turn; the surviving bus must deliver the independent expected marker. Named
  health entries retain their own state and only the configured secondary bus
  applies the Degraded floor. Host shutdown precedes exact final delivery counts.
- `InvalidHealthOptions_RejectStartupWithoutContaminatingTheCompanionBusAsync`:
  ten default/typed invalid-name/status/tag combinations; the companion options
  stay valid and the startup error identifies the failing options type and owner.
- `ScopedSchedulers_DeliverCommandsThroughOnlyTheirOwningBusAsync`: endpoint,
  publish and native-delayed registrations, each exercised through default and
  typed buses, distinct DI scopes, a shared injected clock and the same payload
  contract. Actual receivers capture scope headers, source addresses, command
  tokens, payloads, destinations and recurring identities. Pre-canceled admission
  must retain the exact token; drained counts reject unintended deliveries.

The read-only adversarial review finds no concrete false-green blocker in these
oracles. It confirms the following limits: native delay zero proves routing and
cancellation semantics, not temporal withholding; external scheduler commands
prove routing, not trigger persistence/execution. Explicit DI scopes are tested;
existing consume-context and Quartz MultiBus lifecycle tests supply those separate
contracts and are not copied into this packet.

Microsoft assertion-quality review: all three methods have meaningful assertions,
none are assertion-free or trivial-only. Observed values are compared with
independent bus/scope/payload expectations, not merely with a scheduler handle.
The handle-to-command checks complement those expectations. Exception/token,
structural payload, collection, identity/negative and lifecycle assertions serve
distinct failure modes. No assertion-count target is used as a quality claim.

The first build found fixture compilation issues: ambiguous IHost, reads through
write-only health-configurator properties, two missing cancellation arguments,
and missing concrete HostBuilder dependency. These are corrected in the source.
Microsoft.Extensions.Hosting 10.0.12 is added only to the native test dependency
group and Core test project, matching existing Extensions versions. Isolated
restore is still running; no corrected build/test success is claimed.

Planned isolated counterprobes after the green baseline: route typed health
options to default options; route typed published scheduler commands through the
default publish endpoint. Apply separately, check actual failures, restore exact
source bytes and run the combined restored controls. Do not call a static
mutation candidate a verified survivor or kill.

## Verified implementation checkpoint

The initial executed test run passed eight cases and failed ten invalid-option
cases because reflection wraps OptionsValidationException during health-service
resolution. The fixture now accepts only a direct validation exception or one
TargetInvocationException wrapper, then verifies the exact inner exception type,
owning options type and diagnostic. This is a test-expectation correction, not a
product bug. The corrected baseline passes 18/18 (17 new cases plus projection).

Both planned counterprobes compile without warnings/errors and fail for their
intended reasons: wrong health ownership fails 12/14 with two controls passing;
wrong typed publish provider fails 1/3 on actual primary versus expected secondary
receiver, with endpoint/delayed controls passing. Both source files are restored
to their recorded hashes. Combined restored verification passes 50/50, including
existing health lifecycle, scope and scheduler-clock tests, zero failures/skips
and zero build warnings/errors. All 5,875 source/test paths match MAIN and GATE.
All 96 tracked dependency locks match between the checkouts after the authorized
test-only Hosting restore. Final read-only review confirms no packet blocker.

Evidence: `artifacts/t48-counterprobe-evidence.json` records 14 log hashes,
restored source hashes and the exact input comparison. Restricted restore/build
attempts were deliberately terminated after connection-loop diagnosis and are
not passing evidence. Authorized replacements succeed. Verify-only formatting
on ViciOne.ServiceBus.Tests.Unit.slnx succeeds without changes; the initial use
of the nonexistent shorthand Tests.Unit.slnx is retained as a failed command,
not evidence. The complete implementation is ready for one full33 measurement.
