# ViciOne ServiceBus Deep Review

Date: 2026-09-05
Branch: `feature/servicebus-a-plus-api`
Secured baseline commit: `c26f7cafcba4cc6828ad46ffa0985828c01b8074`
Secured tree: `ede39ccab9fc8dfed674362accb408180555ac22`

## Review status

This is an internal, evidence-based engineering review. It is not represented as an independent external or Red Team acceptance. The review did not modify product code, test code, or anything below `review/`. It establishes the remediation backlog for the next implementation phase.

The requested absolute assurances cannot currently be given:

- The API and parameter test coverage is not 100%.
- Whole-product coverage is 70.1% line and 55.6% branch coverage across the available local profiles.
- Several public configuration members silently discard values or operations.
- At least two concrete runtime correctness defects and one unsupported public execution path remain.
- Comment and XML-documentation quality is not consistently production-grade.
- The API modernization is substantial, but several legacy names, global state patterns, compatibility-era constructs, and structural inconsistencies remain.

All 4,167 tests executed by the available local profiles passed with zero failures and zero skips. This is valuable evidence, but it does not compensate for the measured coverage gaps or the explicitly untested APIs listed below.

## Scope and method

The review covered the complete tracked `src/` and `tests/` trees, solution/project metadata, compiled public API metadata, test source, XML documentation and comments, preprocessor directives, filename/type relationships, namespace/folder relationships, and locally executable coverage profiles.

Evidence sources included:

- Roslyn semantic analysis over 4,911 compiled C# files for async naming and comments.
- A declaration inventory over 4,912 C# files: 4,044 product and 868 test files.
- Compiled metadata from 31 product and 39 test assemblies.
- Public API inventory: 4,153 public types and 23,255 public metadata API entries with 23,218 parameters.
- Static source-to-test pairing and exact IL call mapping. These are useful gap indicators, not a substitute for runtime coverage because DI, reflection, generic dispatch, generated code, and indirect execution can evade static mapping.
- Local .NET coverage collected separately for unit/architecture, general integration, SQL Server, Azure Service Bus emulator, and RabbitMQ profiles, then merged using `dotnet-coverage`.
- Formatting verification for both engineering and unit solutions.

## Executive scorecard

| Area | Result | Evidence-based conclusion |
|---|---:|---|
| Secured baseline | PASS | Local and remote branch both resolve to `c26f7caf`; no force-push was used. |
| Available local tests | PASS | 4,167 passed; 0 failed; 0 skipped. |
| Whole-product line coverage | FAIL | 56,649 / 80,825 = 70.1%. |
| Whole-product branch coverage | FAIL | 15,412 / 27,723 = 55.6%. |
| Every API/new parameter tested | FAIL | Twelve new option/outgoing-message entry points, all 29 option properties, and seven forwarding branches have no direct behavioral tests; further gaps are listed below. |
| No dummy/placeholder behavior | FAIL | No product types named Fake/Stub/Dummy/Mock/Placeholder were found, but silent no-op setters and unsupported public paths are functional placeholders. |
| API modernization | PARTIAL | Large measurable improvement over the prior baseline, but correctness, legacy, naming, lifetime, and consistency findings remain. |
| Bidirectional async naming | PASS with one test-only rename | No misleading product method was found under normal .NET semantic rules. One private test async iterator lacks the suffix. A stricter literal policy would additionally prohibit 130 callback/builder APIs whose names intentionally describe async delegates. |
| Code formatting | PASS | `dotnet format whitespace` and `dotnet format style --severity warn` passed for engineering and unit solutions. |
| Convenience directives | PARTIAL | No blanket warning/nullable suppression exists; one justified scoped pragma remains, plus removable conditional/region/IDE directives. |
| Comments and XML documentation | FAIL | Many mechanically generated or empty descriptions and several stale/process-oriented comments remain. |
| File/type/folder alignment | PARTIAL | 25 concrete filename/type deviations and 51 namespace outliers in otherwise homogeneous product directories remain. |
| MassTransit compatibility | PASS for direct references | No product reference to MassTransit or GreenPipes remains. Adjacent legacy identities and patterns still require deliberate modernization. |

## Critical and high-priority findings

### DR-001 — Entity Framework durable-send abandonment loses the supplied timestamp

Severity: Critical
Evidence:

- `src/Persistence/ViciOne.ServiceBus.EntityFrameworkCore/DurableSend/EntityFrameworkReliableStore.cs:693`
- `src/Persistence/ViciOne.ServiceBus.EntityFrameworkCore/DurableSend/EntityFrameworkReliableStore.cs:725`
- Correct in-memory comparison: `src/ViciOne.ServiceBus/DurableSend/InMemoryReliableStore.cs:658`
- Entity timestamp field: `src/Persistence/ViciOne.ServiceBus.EntityFrameworkCore/DurableSend/EntityFrameworkDurableSendEntities.cs:40`

The EF implementation accepts `abandonedAt` but does not persist it, and the completion path does not populate `CompletedAt`. Existing tests only verify disposition and invisibility. Durable history can therefore disagree between providers.

Required remediation and acceptance:

- Define one provider-independent timestamp contract for abandon and completion.
- Implement it identically in EF and in-memory stores.
- Add behavioral contract tests that execute against every durable-store provider and assert exact persisted timestamps and dispositions.

### DR-002 — Public Azure Service Bus saga-query path always throws

Severity: Critical
Evidence: `src/Transports/ViciOne.ServiceBus.AzureServiceBus/AzureServiceBusTransport/MessageSessionSagaRepositoryContextFactory.cs:58`

The public path always throws `NotImplementedException`. Its direct test only verifies a probe name and never exercises the query behavior.

Required remediation and acceptance:

- Implement the advertised query behavior, or remove/narrow the public capability if Azure Service Bus sessions cannot support its semantics.
- Add provider acceptance tests for supported queries and explicit capability-boundary tests for unsupported operations.
- No public route may remain that deterministically fails as an implementation placeholder.

### DR-003 — Public receiver configuration silently discards operations

Severity: Critical
Evidence: `src/ViciOne.ServiceBus/Configuration/ReceiverConfiguration.cs:44`

`ConfigureConsumeTopology`, `PublishFaults`, `AddDependency`, `AddDependent`, and two `ConfigureMessageTopology` members accept calls but do nothing. Azure queue and subscription configurations inherit this behavior, while the normal receive-endpoint implementation performs the operations.

Required remediation and acceptance:

- Implement each operation with the same observable contract as other receive endpoints, or expose an explicit unsupported-capability result instead of silently succeeding.
- Add behavioral tests for each member on queue and subscription endpoints.

### DR-004 — Additional public configuration setters are silent no-ops

Severity: High
Evidence:

- `src/ViciOne.ServiceBus/InMemoryTransport/InMemoryBusFactoryConfigurator.cs:46` — `AutoStart`
- `src/ViciOne.ServiceBus/DependencyInjection/Registration/EndpointRegistration.cs:36` — `IncludeInConfigureEndpoints`
- `src/ViciOne.ServiceBus/Configuration/CompositeFilter.cs:15` — `Includes` and `Excludes`
- `src/ViciOne.ServiceBus.Abstractions/Middleware/ExceptionFilters/CompositeFilter.cs:11` — `Includes` and `Excludes`

Required remediation and acceptance:

- Implement the documented effect or remove the misleading mutable surface.
- Add outcome-based tests; property round-trip tests alone are insufficient.

### DR-005 — Job cancellation is not propagated consistently

Severity: High
Evidence: `src/ViciOne.ServiceBus/JobService/ConsumeJobContext.cs:120`

Several notification/progress paths accept a cancellation token but use `CancellationToken.None` downstream.

Required remediation and acceptance:

- Propagate the caller token through every cancellable path unless a documented atomic boundary intentionally suppresses cancellation.
- Test pre-cancelled tokens and cancellation during progress/notification operations.

### DR-006 — `NewId` uses mutable process-global configuration with unsafe lifecycle semantics

Severity: High
Evidence: `src/ViciOne.ServiceBus.Abstractions/NewId/NewId.cs:33` and `:498`

Public setters mutate global generator state, cached generators are not consistently invalidated, concurrent configuration is racy, and null guards are absent.

Required remediation and acceptance:

- Prefer injected generator/clock/process providers for greenfield use.
- If a static facade is retained, define and enforce an atomic, one-way configuration lifecycle with null validation.
- Add concurrency, reconfiguration, and deterministic-generation tests.

### DR-007 — New options and outgoing-message API surface lacks direct behavioral tests

Severity: High
Evidence:

- `src/ViciOne.ServiceBus.Abstractions/ISendEndpoint.cs:23`
- `src/ViciOne.ServiceBus.Abstractions/IPublishEndpoint.cs:23`
- `src/ViciOne.ServiceBus.Abstractions/Scheduling/IMessageScheduler.cs:25`
- `src/ViciOne.ServiceBus.Abstractions/Clients/IRequestClient.cs:20`
- `src/ViciOne.ServiceBus/Contexts/Context/ConsumeContext.cs:225`
- `src/ViciOne.ServiceBus/Clients/RequestClient.cs:58`
- `src/ViciOne.ServiceBus/Clients/GenericRequestClient.cs:48`
- `src/ViciOne.ServiceBus.Abstractions/Contexts/IOutgoingMessages.cs:11`
- `src/ViciOne.ServiceBus/Contexts/Context/OutgoingOptionsPipe.cs:93`

Twelve new entry points and 37 parameters are absent from direct test usage. None of the 29 properties across `SendOptions`, `PublishOptions`, `ScheduleOptions`, and `RequestOptions` occurs in test source. Seven forwarding branches in `OutgoingOptionsPipe` are untested: headers, time-to-live, correlation ID, conversation ID, message ID, request ID, and partition key.

Required remediation and acceptance:

- Build a parameter-to-observable-effect matrix for all option types.
- Test every property independently, combined precedence, default behavior, null/invalid boundaries, and transport propagation.
- Execute the same contract on in-memory and representative real/emulated transports where semantics differ.

### DR-008 — Reliable-messaging operations are reflected but not behaviorally exercised

Severity: High
Evidence: `IReliableMessagingOperations.GetSnapshotAsync` and `GetInboxQuarantineAsync` have no direct behavioral call in tests; three associated parameters are likewise uncovered.

Required remediation and acceptance:

- Add authorization, empty/non-empty, pagination/filter, cancellation, concurrency, and provider parity tests.

### DR-009 — SQL URI handlers violate non-nullable contracts

Severity: High
Evidence: both SQL provider `UriTypeHandler.Parse` implementations return `null!` for a non-nullable `Uri` contract.

Required remediation and acceptance:

- Decide whether database null is invalid or modeled as nullable.
- Enforce that contract without null-forgiving operators and test null, malformed, absolute, and relative values.

### DR-010 — Generated topology names use a collision-prone 30-bit hash

Severity: High
Evidence:

- `src/ViciOne.ServiceBus/Topology/ConsumeTopology.cs:125`
- `src/Transports/ViciOne.ServiceBus.AzureServiceBus/Topology/ServiceBusPublishTopology.cs:39`

Six base32 characters carry about 30 bits. At 10,000 generated candidates, the birthday-bound collision probability is approximately 4.5%.

Required remediation and acceptance:

- Increase the stable suffix to at least 10–13 base32 characters or use an equivalent collision-resistant naming scheme within transport limits.
- Add determinism, maximum-length, normalization, and collision-set tests.

## Coverage and test-completeness findings

### Runtime coverage

Merged product-only coverage across all available local profiles:

| Metric | Covered | Total | Rate |
|---|---:|---:|---:|
| Lines | 56,649 | 80,825 | 70.1% |
| Branches | 15,412 | 27,723 | 55.6% |

The merged report contains 30 instrumented product packages and 6,781 source-class entries. Every included source path resolves under this repository's `src/`; no embedded MessagePack source or test source is included. The packaging-only analyzer project has no runtime product assembly to instrument.

Executed profiles:

| Profile | Passed | Failed | Skipped |
|---|---:|---:|---:|
| Unit and architecture | 3,709 | 0 | 0 |
| General local integrations | 344 | 0 | 0 |
| SQL integrations | 63 | 0 | 0 |
| Azure Service Bus emulator | 24 | 0 | 0 |
| RabbitMQ | 27 | 0 | 0 |
| Total | 4,167 | 0 | 0 |

The general integration profile covered PostgreSQL, Azurite, LocalStack, ActiveMQ, Artemis, Event Hubs, and real ActiveMQ outage/restore behavior. The SQL profile included PostgreSQL and SQL Server.

### Static source-to-test pairing

The type-reference heuristic identified 4,044 product source files, of which 1,006 were paired with tests and 3,038 were not. Across all analyzer inputs it identified 4,190 source files, 1,030 paired, and 3,160 unpaired. This is intentionally a worklist signal rather than a coverage percentage: indirect tests, reflection, conventions, and shared helpers can make a file behaviorally covered without a direct type reference.

### Public API mapping

Exact IL mapping found direct calls for 8,007 of 23,255 public metadata APIs and 5,758 of 23,218 public parameters. It left 15,248 APIs and 17,460 parameters unmapped. These numbers must not be described as definitive untested counts because interface dispatch, reflection, DI, extension/generic resolution, and compiler-generated paths can hide real execution. They do establish that a 100% API/parameter claim has not been demonstrated.

Additional mapping indicators:

- 1,425 overload groups contain 4,489 methods/constructors and 11,005 parameters.
- 3,351 overloads and 8,446 overload parameters have no exact mapped call.
- 1,329 of 2,041 interface methods have no exact mapped call.
- Resolver errors: zero.

The remediation phase must create a version-controlled public API manifest and an API-to-contract-test ledger. Coverage alone cannot prove that every optional parameter, overload, and semantic branch has an effective assertion.

## API modernization and legacy assessment

The modernization is substantial and measurable. Compared with the prior A baseline, public root-namespace types decreased from 1,192 to 113, root extension methods from 1,474 to zero, async methods without cancellation tokens from 1,062 to zero under the public reflection policy, `DateTime` signatures from 423 to zero, obsolete members from four to zero, and hidden `EditorBrowsable(Never)` members from 69 to zero. Send/publish surface shapes also contracted sharply.

It is nevertheless not yet a fully clean greenfield API because of the correctness and no-op findings above and the following design debt.

### Direct MassTransit compatibility

No product reference to `MassTransit` or `GreenPipes` remains. No compatibility interface with the old MassTransit API was found.

### Remaining legacy identities and patterns

- `Automatonymous` remains in four public/project identities:
  - `src/ViciOne.ServiceBus.StateMachineVisualizer/ViciOne.ServiceBus.StateMachineVisualizer.csproj:15`
  - `src/ViciOne.ServiceBus/DependencyInjection/Registration/SagaRegistrationExtensions.cs:19`
  - `src/ViciOne.ServiceBus/SagaStateMachine/SagaStateMachineInstance.cs:4`
  - `src/ViciOne.ServiceBus/SagaStateMachine/ViciOneServiceBusStateMachine.cs:16`
- `RequestTimeout` occurs 223 times across 39 product files. The concept may remain valid, but the public model should be reviewed against one coherent greenfield `RequestOptions`/deadline model.
- 106 `[Serializable]` attributes occur in 104 files without custom serialization contracts. Determine whether binary-serialization compatibility is still a real feature requirement; otherwise remove it.
- `INewIdFormatter`/parser APIs expose eleven `in string` or `in byte[]` signatures. Prefer `ReadOnlySpan<T>`/`ReadOnlyMemory<T>` where appropriate and assess whether `Guid.CreateVersion7` can replace custom surface without feature loss.
- The public API lacks a complete member-level compatibility manifest such as `PublicAPI.Shipped.txt` or an ApiCompat baseline. Current architecture tests mainly guard root type names and broad counts.
- Thirteen `NotImplementedByDesign` and 51 `NotSupported` sites require classification. Most appear to be legitimate capability boundaries, but custom public exception/type names must not imply unfinished implementation.

### Naming and documentation defects

- `CompositePredicate.DoesNotMatcheAny` contains a spelling error.
- `SendEndpointExtensions` uses receiver name `publishEndpoint` in two send methods.
- SQL Server configurator documentation incorrectly says PostgreSQL.
- `BusTestHarness` says “bus text fixture”.
- Several Amazon SQS comments use exchange terminology where topic terminology is intended.

## Bidirectional async naming review

Semantic inventory:

- 21,398 methods inspected.
- 7,460 methods have an awaitable or async-stream contract.
- 7,585 names end in `Async`.
- 3,569 use the `async` keyword.
- Zero `async void` methods.

Under normal .NET semantic conventions, no misleading product method was found:

- Four unsuffixed `Task` methods are required Quartz interface contracts (`IJob.Execute`, job-factory methods, and a test implementation).
- 130 methods with an `Async` suffix but no direct awaitable return were individually classified: 120 configure callback/builder APIs whose parameters are async delegates, eight Azure `OnMessageAsync`/`OnSessionAsync` registrations, and two Event Hub callback factories. Their suffix communicates the asynchronous delegate shape and is not a dummy async implementation.
- One private test helper is a true bidirectional naming exception: `tests/ViciOne.ServiceBus.Tests/Testing/MessageObservationListTests.cs:379`, `Empty<T>() : IAsyncEnumerable<T>`, should be named `EmptyAsync<T>()`.

If the project intentionally adopts the user's stricter literal rule—every suffix must itself return an awaitable—then the 130 callback/builder APIs need a breaking rename policy. That would be stricter than common .NET event/delegate-registration usage and should be decided as an API rule before mechanical changes.

Async-related comment findings:

- `SupervisorExtensions.cs:150` names `CreateAgent` where the implementation is `CreateAgentAsync`.
- `IStateObserver.cs:12` says notification occurs before the state change, while raw/string/integer implementations update first.
- `ActivePipeContext.cs:17` contains “pretty nasty async mess” and “I haven’t tested it”; both are process-history commentary and must be replaced by current behavioral documentation or removed.

All 478 XML `cref` references inspected by the semantic pass resolved successfully.

## Dummy, placeholder, TODO, and unfinished behavior review

No product type declaration named Fake, Stub, Dummy, Mock, or Placeholder was found. This is not enough to claim that dummy behavior is absent: DR-002 through DR-004 identify real unsupported or silent-placeholder behavior.

Sixty-five TODO/FIXME/HACK/WIP comments remain. Fifty-nine are in generated/vendored `ExpressionCompiler` sources. The six project-owned findings are:

- `src/ViciOne.ServiceBus.Abstractions/Transports/IReceiveEndpointDispatcher.cs:36`
- `src/ViciOne.ServiceBus/Middleware/HandlerMessageFilter.cs:21` — appears outdated
- `src/ViciOne.ServiceBus/DependencyInjection/Configuration/ConsumerDefinition.cs:24` — appears outdated
- `src/Transports/ViciOne.ServiceBus.RabbitMqTransport/RabbitMqTransport/Topology/RabbitMqSendTopology.cs:59`
- `src/Transports/ViciOne.ServiceBus.ActiveMqTransport/ActiveMqTransport/Topology/QueueEntity.cs:91`
- `src/Analyzers/ViciOne.ServiceBus.Analyzers/Analyzers/MessageContractAnalyzer.cs:74`

Each must be resolved into implemented behavior, a tracked design decision, or deletion. Generated/vendored comments should be handled by changing the generator/upstream artifact, not by editing generated output blindly.

Commented-out code remains in at least:

- `ValidationResultExtensions.cs:126`
- `ViciOneServiceBusStateMachine.cs:2026`
- `SqlRegistrationBusFactory.cs:48`
- `SystemTextJsonSerializationContext.cs:12`
- `PostgresDbConnectionContext.cs:255`
- `ActiveMqMessagePublishTopology.cs:76`

These sites require deletion or restoration as real code with tests; source control, not comments, should retain build history.

## Formatting and directive review

Formatting verification passed without changes for both solution scopes:

- `dotnet format whitespace ViciOne.ServiceBus.Engineering.slnx --verify-no-changes --no-restore`
- `dotnet format style ViciOne.ServiceBus.Engineering.slnx --verify-no-changes --no-restore --severity warn`
- The corresponding two commands for `ViciOne.ServiceBus.Unit.slnx`.

Directive inventory contains 767 semantic directive nodes:

- 477 nullable directives: 469 enable and eight enable-annotations; no nullable disable.
- 94 `#if`, 85 `#else`, 94 `#endif`, and three `#define` nodes.
- Six `#region` and six `#endregion` nodes.
- Two `#pragma` nodes forming one tightly scoped `CS0618` suppression in `AmazonS3MessageDataRepository.cs:302`–`:304`.

There are no blanket warning disables, nullable disables, `#line`, `#warning`, or `#error` shortcuts. The scoped Amazon S3 pragma is justified by a documented compatibility call and is not evidence of broadly relaxed code quality. The following cleanup remains:

- Remove six convenience region pairs unless a generated-code requirement exists.
- Simplify the always-selected `USE_CONCRETE_MAPPERS` conditional in the MessagePack resolver.
- Keep vendored/generated ExpressionCompiler conditionals aligned with their source generator/upstream policy.
- Review 35 ReSharper-disable comments: nine are in generated ExpressionCompiler files and 26 are in other product code. Replace convenience suppressions with corrected code or a narrowly documented analyzer policy.

## Comment and XML-documentation review

The semantic pass inspected 24,468 comment blocks over 130,743 distinct comment lines. It found no review-package or Red Team work-history narratives, but documentation quality is not acceptable as a whole.

Mechanically weak XML documentation patterns include approximately:

- 1,785 “Provides … implementation” descriptions.
- 5,132 “The result of the operation” descriptions.
- 12,616 “The … value” descriptions.
- 4,757 “The T type” descriptions.
- 3,471 “Performs … operation” descriptions.
- 4,154 “Gets … value” descriptions.
- 911 empty `returns`, 2,456 empty `param`, 673 empty `typeparam`, and 24 empty `summary` elements.

These patterns occur in 3,326 of 4,044 product files, including 673 of 897 abstractions files. They satisfy syntax but often do not explain contract, invariants, units, ordering, nullability, cancellation, failure modes, or transport-specific behavior.

Other comments requiring correction or removal include:

- `AzureBusFactory.cs` roadmap language about Event Hubs.
- Amazon SQS `Topic.cs` using “exchange”.
- ActiveMQ publish-topology uncertainty/dead-code notes.
- `MessageEntityNameFormatter` speculative commentary.
- `EvaluatedBuildGraphTests.cs:208`–`:211` build-history narrative.

Remediation acceptance criteria:

- Every retained comment describes current code behavior, contract, rationale, invariant, or non-obvious constraint.
- No comment describes the sequence by which the code was built, temporary uncertainty, prior implementation, or completed work package.
- Public XML documentation explains semantic value rather than restating the identifier.
- Documentation changes are reviewed together with the corresponding behavior and tests to avoid creating new drift.

## Filename, type, folder, and namespace review

The declaration analysis classified 4,347 files as exact primary-type matches, 404 as matching plus additional declarations, 77 as generated/infrastructure exceptions, 61 as single-declaration deviations, 21 as multi-declaration files without a primary match, and two as conditional cases. After applying justified exceptions, 25 concrete filename/type findings remain.

Product findings:

1. `DeadLetterMessageFilter.cs` → `DeadLetterQueueFilter`
2. `IEventHubProducer.cs` → `IEventHubProducerConfigurator`
3. `Advanced/Observers/IActivityPipeConfigurator.cs` contains three public interfaces and needs splitting or a collective filename.
4. `PipeBuilderException.cs` → `PipeFactoryException`
5. `TarjanNodeProperties.cs` → `ITarjanNodeProperties`
6. `TopologicalSortNodeProperties.cs` → `ITopologicalSortNodeProperties`
7. `JobRetryWaitElapsed.cs` → `JobRetryDelayElapsed`
8. `ProcessIdProvider.cs` → `CurrentProcessIdProvider`
9. `SerializerFactory.cs` → `ISerializerFactory`
10. `ViciOne.ServiceBus.Testing/IStateMachineSagaTestHarness.cs` → `ISagaStateMachineTestHarness`
11. `StateMachineSagaTestingExtensions.cs` → `StateMachineSagaTestHarnessExtensions`
12. `SendPipeConfiguratorExtensions.cs` → `DelegatePipeConfiguratorExtensions`
13. `DurableSender.cs` → `DurableSendAdmission`
14. `ProviderPropertyInitializer.cs` → `ProviderHeaderInitializer`
15. `IOutputMessageTypePipeFilter.cs` → `IConsumeContextOutputMessageTypeFilter`
16. `MessageTypeFilter.cs` → `ConsumeContextMessageTypeFilter`
17. `EnvelopeSerializerContext.cs` → `BaseSerializerContext`
18. `Serialization/MessageBody.cs` → `SerializedMessageBody`
19. `SqlTransport/ServiceBusHost.cs` → `SqlHost`
20. `ConventionTypeCache.cs` → `TopologyConventionCache`

Test findings:

1. `BusOutboxDeliveryServiceTests.cs` → `EntityFrameworkTransactionalOutboxSourceTests`
2. `ServiceBusFunctionsAndProbeTests.cs` → `ServiceBusSessionSagaProbeTests`
3. `PayloadAdmissionMessagePackFixtures.cs` contains only `BoundaryPayload` and should be renamed or regrouped.
4. `InMemoryDurableSendStoreTests.cs` → `InMemoryReliableStoreTests`
5. `SystemTextJsonConfigurationTests.cs` contains three public test classes and should be split.

All 243 test directories are namespace-consistent. In product code, 64 of 306 directories contain multiple namespaces; 27 otherwise highly homogeneous directories contain 51 outlier files. Representative examples are:

- `Configuration/MessageSchedulerBusExtensions.cs` declares an `Advanced` namespace.
- `Abstractions/Contexts/IOutgoingMessages.cs` declares the root namespace.
- `Contexts/OutgoingOptionsPipe.cs` declares `Context`.
- `Serialization/ReliableEnvelopeMetadataCodec.cs` declares `Advanced.Serialization`.
- `AzureServiceBus/AzureBusFactory.cs` declares `Configuration`.

Some layering is clearly intentional, so folder changes must be decided per component rather than applied mechanically. Public namespaces and type names take precedence over cosmetic folder uniformity.

## Test quality review

The source suite contains 2,992 test methods and 13,209 direct assertions, averaging 4.41 direct assertions per test. All 444 `ThrowsAsync` calls are awaited. No `async void`, tautological assertion, unexpected empty catch, theory without data, or skipped test was found.

Requirement traceability metadata exists on 2,831 of 2,992 test methods (94.62%). The remaining 161 are mainly infrastructure and architecture tests, but every omission should still be explicitly classified.

Fifteen tests have no direct assertion and are classified as must-not-throw scenarios; another 114 reach assertions through helpers. These are not automatically defects, but the must-not-throw intent should be explicit in the test name and the exercised observable boundary.

Quality smells requiring remediation:

- Fixed 250 ms waits in `MessageFabricTests.cs:89` and `:192`.
- Wall-clock polling in Entity Framework inbox-pipeline tests around lines 301–317.
- Direct `DateTime.UtcNow` use in `StateMachineSchedulingIntegrationTests.cs:153`.
- Broad exception assertions in `EntityFrameworkOptionsStartupValidationTests.cs:19`, `TestHarnessOptionsStartupValidationTests.cs:24`, and `ReliableMessagingRegistrationAndAdmissionTests.cs:649`.

Replace timing sleeps/polling with deterministic signals or controlled time, and assert the narrowest meaningful exception type plus message/parameter details where those are part of the contract.

## Remediation programme

The next phase should be executed in dependency order and should not claim completion until the acceptance evidence is version-controlled.

### Wave 1 — Correctness and explicit capability semantics

1. Fix EF durable timestamps and add cross-provider contract tests.
2. Resolve the Azure saga query public path.
3. Implement or remove every silent configuration no-op.
4. Correct cancellation propagation and SQL URI nullability.
5. Extend topology-name collision resistance with deterministic boundary tests.

### Wave 2 — Complete the new API contract suite

1. Create a public API/member manifest.
2. Create a machine-readable API/parameter-to-test ledger.
3. Cover every new options property and forwarding branch.
4. Cover reliable-messaging operations and capability boundaries.
5. Add mutation tests for critical option forwarding, timestamp persistence, cancellation, and unsupported-path changes.

### Wave 3 — Greenfield API cleanup

1. Replace global mutable `NewId` configuration.
2. Decide the strict async delegate-registration naming policy, then enforce it with an architecture test.
3. Remove remaining `Automatonymous` identities and review `RequestTimeout`, `[Serializable]`, and custom NewId APIs without feature loss.
4. Correct naming, spelling, receiver names, and misleading transport documentation.
5. Classify all `NotSupported`/`NotImplementedByDesign` sites as explicit capability boundaries or defects.

### Wave 4 — Structural and documentation hygiene

1. Apply the 25 verified file/type corrections and decide the 51 namespace outliers.
2. Remove commented-out code, stale TODOs, convenience regions/conditionals, and unjustified IDE suppressions.
3. Rewrite public contract documentation and remove build-history/process commentary.
4. Keep generated/vendored changes generator-driven.

### Wave 5 — Proof and acceptance

1. Run formatting and analyzers with zero new warnings.
2. Run all unit, architecture, provider-integration, and outage/recovery profiles.
3. Recollect product-only line/branch coverage.
4. Run mutation testing on the highest-risk modules and every fixed defect.
5. Re-run the compiled API inventory, bidirectional async check, comment/directive review, filename/type review, and API/test ledger validation.
6. Commit and push the implementation plus evidence only after all gates are green.

## Required completion definition

The requested future assurances become defensible only when all of the following are true:

- No public member silently ignores input or deterministically throws as an implementation placeholder.
- Every public API member and parameter is classified and linked to behavioral contract evidence or a justified non-executable declaration role.
- All newly introduced options and forwarding paths have positive, negative, boundary, precedence, cancellation, and provider-parity tests as applicable.
- Coverage gaps are reviewed method-by-method; critical paths reach the agreed line/branch thresholds and survive targeted mutation testing.
- Async naming passes the documented project policy bidirectionally.
- Formatting, analyzers, comments, directives, file/type names, namespaces, and legacy searches pass version-controlled architecture checks.
- No feature is removed during legacy cleanup without an equivalent clean greenfield API and regression evidence.

## Reproducibility notes

The first sandboxed .NET coverage attempt failed because .NET 10/Microsoft Testing Platform/Roslyn needed named-pipe access. Running the same bounded build/test/coverage operations outside the sandbox succeeded. This is the established diagnostic resolution for this repository and should be reused instead of changing build or test composition in response to the sandbox-only failure.

Coverage input files and the companion method/file-risk analysis are stored under `TestResults/coverage-analysis/`. The initial broad-filter exploratory unit XML is deliberately excluded from the merged report because it included embedded MessagePack source; only the strict repository-`src` profiles were merged.
