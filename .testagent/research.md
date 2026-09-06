# A+ remediation research

## Baseline

- Product commit: `c26f7cafcba4cc6828ad46ffa0985828c01b8074`
- Product tree: `ede39ccab9fc8dfed674362accb408180555ac22`
- Protected remote tag: `servicebus-a-plus-api-program-complete-2026-09-05`
- Architecture commit: `94da260cbe66ea49abe61459218cb576cb47f27e`
- Protected architecture tag: `architecture-servicebus-a-plus-program-baseline-2026-09-05`
- Review input: immutable files below `review/**`; they are never changed or staged.

## Test platform and repository conventions

- Target framework: .NET 10.
- Test runner: Microsoft Testing Platform v2.
- Test framework: xUnit 4.
- Existing tests use `Fact`, `Theory`, `Assert`, and `RequirementCoverage` mappings.
- Focused project builds and tests precede a clean full Release validation.
- Any behavior change requires a causal test that fails under a one-cause mutation.

## Existing test architecture

- Core behavior: `tests/ViciOne.ServiceBus.Tests`.
- Public abstractions: `tests/ViciOne.ServiceBus.Abstractions.Tests`.
- EF reliable storage: `tests/Persistence/ViciOne.ServiceBus.EntityFrameworkCore.Tests`.
- Azure Service Bus: `tests/Transports/ViciOne.ServiceBus.AzureServiceBus.Tests` and its local-integration project.
- Architecture/API rules: `tests/Architecture/ViciOne.ServiceBus.Architecture.Tests`.
- Requirement mappings are stored in each owner project's `Requirements/*.json` files.

## Static source-to-test pairing scan

The mandatory Roslyn-based repository scan inspected 4,190 source files and 859 test files. It classified 1,030 source files as name-paired and 3,160 as not name-paired. This is a discovery heuristic only: generic dispatch, integration tests, architecture tests, and differently named behavior owners make a direct filename pair neither necessary nor sufficient.

High-value findings from the scan and the semantic reviews:

- `OutgoingOptionsPipe.cs` has no name-paired behavior test; all public options branches need direct tests.
- `ConsumeTopology.cs` has no name-paired behavior test; its shortened hash needs deterministic collision-resistance tests.
- Several composition and configuration types lack direct tests even where setters currently discard caller intent.
- The broad count cannot be used as evidence that 3,160 files are behaviorally untested.

## Confirmed iteration-1 defects

1. EF reliable inbox abandonment discards the supplied `abandonedAt` and does not persist `CompletedAt`, unlike the in-memory provider.
2. `ReceiverConfiguration` silently discards six operations even though the wrapped `IReceiveEndpointConfiguration` implements their semantics.
3. `InMemoryBusFactoryConfigurator.AutoStart` silently discards the configured value.
4. `EndpointRegistration<T>.IncludeInConfigureEndpoints` silently discards the configured value.
5. Both composite filter implementations expose replaceable-looking setters that discard assigned values; the public variant also contains the typo `DoesNotMatcheAny`.
6. The Azure Service Bus message-session saga repository exposes query correlation but throws `NotImplementedException` only when a message is consumed. The supported capability boundary must be explicit and fail before processing.

## Related risks queued for later iterations

- `NewId` has mutable process-global providers with ambiguous post-initialization semantics.
- Job notifications may discard caller cancellation.
- Public options and outgoing-message application APIs lack complete parameter-level behavior coverage.
- Public API layering, member-level baselines, XML documentation, filenames, folders, namespaces, directives, and comments need repository-wide remediation.
- Topology hash suffixes provide only 30 bits of disambiguation.
- SQL URI handlers expose incorrect nullability.
- Test polling and fixed-delay negative assertions should become deterministic.

## Confirmed iteration-10 defects

1. The fresh-package API gate writes an inventory but never compares it with a committed baseline,
   so an accidental public API change remains green.
2. The gate packs only the nineteen packages needed by the developer journeys although the product
   publishes thirty packages. Eleven delivery packages and twelve runtime assemblies are therefore
   outside its package and API verification boundary.
3. No package-only consumer restores the complete runtime package catalog. The API inventory can
   consequently inspect only the subset that earlier samples happened to restore.

The greenfield reference is the current .NET task-based asynchronous pattern, the .NET runtime
framework-design-guideline digest, and NuGet package-authoring guidance. The resulting repository
rule is stricter than compatibility-oriented library evolution: every intended API change must be
an explicit reviewed baseline update, while an unreviewed addition, removal, rename, visibility
change, default-value change, or signature change fails CI.

## Reference API direction

The greenfield reference model is a small application surface, explicit capability packages, validated immutable options, provider contracts outside application IntelliSense, standard .NET naming and cancellation conventions, and no historical compatibility aliases. Feature preservation means retaining behavior through the correct layer; it does not require retaining accidental public exposure or silently ignored members.

## Confirmed iteration-11 defects

1. Both generic and non-generic `Task.OrTimeoutAsync` convert caller cancellation into a
   `TimeoutException`, whether the token is canceled before entry or while the operation is
   waiting. The caller's exact token and cancellation outcome are therefore lost.
2. `ConsumerJobHandle.CancelAsync` checks the caller token only before canceling the job. It does
   not pass that token to the subsequent job-completion wait, and its broad cancellation catch
   would also hide caller cancellation if the token were forwarded.
3. ActiveMQ session topic and queue deletion check cancellation only before enqueueing and then
   submit the operation with `CancellationToken.None`, so cancellation cannot release a caller
   waiting for bounded executor capacity.
4. The state-machine test harness polling loop substitutes `CancellationToken.None` for its public
   operation token, so an aborted observation can remain asleep until the polling interval ends.

The iteration uses deterministic cancellation sources and virtual time. It distinguishes caller
cancellation from a genuine timeout and from the job-owned cancellation that represents normal job
shutdown.

## Confirmed iteration-12 defects

1. Two synchronous SignalR group-membership methods carry an `Async` marker even though they return
   `void`; four synchronous test infrastructure helpers carry the same misleading marker.
2. Request-handle factory contracts place `CancellationToken` before `RequestTimeout` across the
   abstractions, client implementations, dependency-injection adapters, and mediator. This conflicts
   with the standard .NET cancellation shape and makes positional calls inconsistent.
3. The existing cancellation architecture check verifies only that a token identifier appears in a
   public method body. It does not enforce the API parameter shape, and asynchronous naming has no
   executable repository-wide contract.

The bidirectional gate inspects every evaluated product and test source method, including local
functions. An `Async` name marker must correspond to `async`, `Task`, `ValueTask`,
`IAsyncEnumerable`, or `IAsyncEnumerator`, and every such asynchronous contract must expose the
marker. The public product API separately requires `CancellationToken` to be its final parameter.
