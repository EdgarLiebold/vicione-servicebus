# ViciOne.ServiceBus modifications

ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.

The baseline is the complete MassTransit 8.5.10 source tree at upstream commit `62ab339afa3bac2e9b3fe1769d0d35d7e44778e9`, imported into the local fork baseline commit `1de4bf6eb45c406da3cd6f26bdab6ed6d5aeefbc` (tree `2b09d4e2b2e14289f06ba112ce1ae52e326a0307`).

ViciOne changed the technical identity to `ViciOne.ServiceBus` across paths, projects, assemblies, packages, source identifiers, configuration, wire formats, topology, diagnostics, tests, documentation and automation. The deterministic mapping and one-to-one baseline census remain available in Git tag `archive/servicebus-pre-review-cleanup-20260930` under `evidence/WP-F2-SERVICEBUS-IDENTITY/`.

## ViciOne modification: WP-F2-SERVICEBUS-A-PLUS-RECOVERY-03, 2026-08-18

`SessionContext` gained `EnsureTopicExists(Topic)` and the ActiveMQ topology filter asks for that
outcome. The baseline resolved each topic name through the NMS session and treated that as a
deployment; measured against a real broker, resolving leaves the broker without the topic, so a
deployed publish topology existed only in the client.

Beyond the identity change, ViciOne removed and modernised capabilities of the baseline:

- The foreign licence check and the usage telemetry were removed with their dependency injection and
  public API surface. The telemetry reported host, bus, rider and endpoint data to a hard wired third
  party address on every bus start and was on by default.
- The inert `TypeAttributes.Serializable` flag on the dynamically emitted message proxy was removed
  together with its `SYSLIB0050` suppression.
- Analyzers and code fixes were split into two assemblies, so the analyzer assembly no longer
  references `Microsoft.CodeAnalysis.Workspaces`. They still ship as the one package
  `ViciOne.ServiceBus.Analyzers`.
- The build was modernised onto `net10.0`, with the canonical `.slnx` solution and the SDK warning
  level. Compilation output has one central root, `artifacts/sdk`. Test execution and durable evidence
  deliberately keep two separately owned roots: a run's raw files - result file, endpoint projection,
  control files, broker logs - under `artifacts/run-output/<run>/`, and its durable record under the
  evidence parent the caller named, in that run's own child of it. The record is the one file meant to
  outlive the run, which is why it is not written under the raw output.
- The Apache-2.0 licence text is carried as `LICENSE.txt`. The text is unchanged; only its file
  name changed.

The [repository diff script](license/repository_diff.py) summarizes the current Git diff from the original
MassTransit import. Git retains the exact file history; the [changelog](CHANGELOG.md) describes
the source changes by area.

## Changed files without an in-file modification comment

The following retained baseline files are changed, but their format cannot carry a syntax-valid
in-file comment. Each baseline source is bound to its exact current target; `NOTICE` lists the current
targets in the same order:

- `MassTransit.sln` -> `ViciOne.ServiceBus.slnx`
- `MassTransit.snk` -> `ViciOne.ServiceBus.snk`

## ViciOne modification: greenfield circuit breaker and message journal, 2026-08-25

The inherited circuit-breaker configurator, router-event and timer model was replaced by one
validated options boundary, an immutable runtime snapshot and a timer-free state machine with an
exclusive half-open probe. Its health surface is OpenTelemetry only.

The inherited message-audit API and its EF Core and Azure Table implementations were removed. The
useful capture capability now has an intentionally incompatible greenfield `MessageJournal` API:

- it is inactive until a caller explicitly connects a store, a selection/redaction policy, finite
  storage limits and a finite write deadline;
- providers receive only immutable, policy-sanitized entries derived from the serialized envelope;
- journal failures never change the outcome of send, publish or consume operations;
- finite count and age retention are enforced transactionally on every append, without a background
  queue, retry carrier or second outbox; and
- it is a diagnostic message journal, never the ViciOne Suite operational or security audit owner.

The retained provider capability is implemented for EF Core and Azure Table under source-mirrored
namespaces. The [repository diff script](license/repository_diff.py) groups changes to the original
source tree; the [changelog](CHANGELOG.md) explains the replacement and retired capability.

## ViciOne modification: A+ public API program, 2026-09-05

**A — Code policy.** ViciOne applied nullable analysis, implicit imports, deterministic builds,
documentation generation, warnings-as-errors, and build-time style enforcement across product,
test, sample, benchmark, and tool projects; normalized C# layout; removed obsolete suppression and
temporary test-tool state; and aligned the documented build and test commands with the enforced
repository contract.

**B — Application conventions.** ViciOne renamed public asynchronous operations with the `Async`
suffix, added causal cancellation tokens where the operation contract permits them, changed public
scheduler instants to `DateTimeOffset`, replaced ambiguous application overloads with typed option
records, reduced consumer context to its application responsibilities, and replaced direct process
clock access with injectable `TimeProvider` ownership.

**C — API layers and identity.** ViciOne organized public contracts into Application, Configuration,
Advanced, Provider, Operations, and Testing namespaces; aligned transport project, package, assembly,
and namespace names; moved service-registration methods to the standard dependency-injection
namespace; removed hidden and obsolete public symbols; documented the public surface; and added a
generated application-API inventory enforced by architecture tests.

**D — Startup safety and message boundaries.** ViciOne added aggregate per-bus startup validation,
mandatory send-and-receive message size and JSON-depth limits, fail-closed raw JSON type admission,
validated feature ownership, explicit message-journal registration, and authenticated versioned
AES-GCM protection in place of unauthenticated encryption paths.

**E — Reliable messaging.** ViciOne unified durable send, transactional outgoing messages,
duplicate-safe inbox handling, retry, stored scheduling, quarantine, health, telemetry, and operator
actions behind one application-owned store and one delivery service. Entity Framework Core and
in-memory providers implement the unified contracts; durable provider acceptance is explicit and
unsupported transport combinations fail during startup.

**F — Capability packages.** ViciOne extracted Sagas, Courier, Futures, JobService, Mediator, and
Initializers into independently selectable packages; split EF Core reliable messaging and journal
support from EF Core Saga support; introduced the `IConsumerKind` extension contract; updated package
graphs and provider metadata; and added an executable minimal Suite composition.

**G — Product documentation and examples.** ViciOne expanded the package-only developer examples to
eighteen scenarios, added product-focused API, reliability, observability, build, deployment, and
database documentation, and recorded the intentional application call-form, namespace, and package
changes in the product changelog.
