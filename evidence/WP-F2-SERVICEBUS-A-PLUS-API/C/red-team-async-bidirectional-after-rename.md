# Internal Red Team — Bidirectional Async Naming Audit After Canonical Rename

## Decision

**PASS — zero true findings on the frozen source state.**

The two required directions are clean after semantic review:

1. No public, non-public product method, or local function with a genuinely asynchronous return is missing the `Async` suffix, except for the explicitly allowed delegate/callback and externally owned member-name contracts documented below.
2. No public, non-public product method, or local function ending in `Async` falsely presents synchronous work as asynchronous. Every mechanically suspicious synchronous-completion case was manually adjudicated as an asynchronous lifecycle, callback, task-adapter, or public async-contract implementation.

This is an **internal adversarial Red Team review performed by the implementation team**. It is not independent acceptance, does not replace the external reviewer, and does not claim organizational separation.

## Frozen source state

| Item | Value |
|---|---|
| Repository | `repositories/vicione-servicebus` |
| Branch | `feature/servicebus-a-plus-api` |
| HEAD | `2e898ea9a16efeab0d74996266e136d02a7e6178` |
| Final semantic scan | `2026-09-04T22:55:50.357231Z` |
| Final reflection inventory | `2026-09-04T22:56:09.966794Z` |
| Freeze recheck | `2026-09-04T23:00:27Z` |
| C# source aggregate | `a3eef2d4dbd6982745cc9469d2de4c2178edf1e397d5c89e3db472f83c316714` |
| `src` code-and-project-metadata aggregate | `ad6a34f2b425331014daaf9a558c0f6dee306e98ad30fe96f1ebc290f8d25987` |
| Engineering solution/global build-input aggregate | `bf7be451166e1fa1e82041a0f312cfcccd8eea34db2e4c9a1b91451bbb0b77f7` |
| Scanner SHA-256 | `59dc75107d177d3fe1aba8ebb57cc42d56934e9d5af97de411ed6aa41c078980` |
| Raw semantic-result SHA-256 | `cc0bb77c8126b8cb012303ecd7756abb28fcdf7e41065dd17df753e629d39256` |
| Raw fresh reflection-result SHA-256 | `3eea03ffd1a8e96cc937bf997ad9c8baffd81e930154c38e587be6a8e16b75a6` |
| Canonical post-format reflection inventory SHA-256 | `36721d4ac0ec06b356dc1895a204c8082ba22c0ebac22faa9f5954b32f1451f5` |

The C# aggregate was identical before the final semantic run and after the reflection and focused-test runs. Concurrent evidence-only changes elsewhere in the shared worktree therefore did not mix the reviewed product snapshot.

The final SHA-256 of this report is communicated alongside the completed file rather than embedded in the file itself: embedding its own digest would change the digest recursively.

Fingerprint commands:

```sh
git rev-parse HEAD
git branch --show-current

find src -type f -name '*.cs' \
  -not -path '*/bin/*' -not -path '*/obj/*' -print0 \
  | sort -z | xargs -0 shasum -a 256 | shasum -a 256

find src -type f \( -name '*.cs' -o -name '*.csproj' -o -name '*.props' -o -name '*.targets' \) \
  -not -path '*/bin/*' -not -path '*/obj/*' -print0 \
  | sort -z | xargs -0 shasum -a 256 | shasum -a 256

shasum -a 256 ViciOne.ServiceBus.Engineering.slnx global.json \
  Directory.Build.props Directory.Build.targets Directory.Packages.props \
  | shasum -a 256
```

## Scope and method

The audit loaded all 24 code-bearing product projects in `ViciOne.ServiceBus.Engineering.slnx` with Roslyn `MSBuildWorkspace`. The 25th project under `src`, `ViciOne.ServiceBus.Analyzers.Package`, is packaging-only and has no C# compilation surface. All 24 loaded compilations reported zero compiler errors.

The scan used resolved symbols, return types, effective type accessibility, explicit interface implementations, override relationships, delegate `Invoke` signatures, invocation targets, and method bodies. It did not depend on pre-rename directory names or hard-coded namespace strings. It covered:

- `Task`, `Task<T>`, `ValueTask`, `ValueTask<T>`, `IAsyncEnumerable<T>`, and `Azure.AsyncPageable<T>`;
- effectively public declarations and explicit implementations of public contracts;
- non-public product methods, including protected lifecycle hooks and private helpers;
- local functions;
- compiler state machines, async streams, delegated asynchronous operations, task-bearing inputs, and synchronous-completion wrappers requiring human review;
- both naming directions and relevant cancellation-token structure/flow;
- the changed durable-send test double and all five of its `IReceiveObserver` contract implementations;
- all nine discovered public `Using…`/`CreateUsing…` transport-selection source files after their canonical namespace moves.

Excluded from the product-wide source scan were `review/**`, `evidence/**`, `artifacts/**`, `**/bin/**`, `**/obj/**`, generated code without a source declaration, tests, samples, and tooling. The changed observer test double in `InMemoryDurableSendIntegrationTests.cs` was then reviewed explicitly against its product contract and executed as a focused test class. Compiler-generated delegate `Invoke` members were counted separately instead of being misreported as source methods.

Final semantic command:

```sh
env DOTNET_CLI_HOME=/private/tmp/vicione-sb-dotnet-home-10400 \
  DOTNET_ROOT=/private/tmp/vicione-dotnet-10-current \
  DOTNET_MULTILEVEL_LOOKUP=0 DOTNET_CLI_TELEMETRY_OPTOUT=1 \
  DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=1 \
  MSBUILDDISABLENODEREUSE=1 \
  NUGET_PACKAGES=/Users/edgar.liebold/.nuget/packages \
  /private/tmp/vicione-dotnet-10-current/dotnet run \
  /private/tmp/RedTeamAsyncCurrent.cs -- \
  ViciOne.ServiceBus.Engineering.slnx \
  /private/tmp/red-team-async-current-c-final-freeze-v5.json
```

The first sandboxed attempt on the earlier stable candidate could not create the local MSBuild build-host named pipe and failed with `SocketException: Permission denied`. Repeating the identical read-only analysis outside that sandbox boundary succeeded; the final-freeze run used that established execution mode. This was an execution-environment restriction, not a repository defect.

One non-fatal workspace diagnostic was emitted: the analyzer project reference had no matching metadata reference in one loaded workspace context. There were no compilation errors. The compiled-assembly reflection inventory then loaded every available product assembly with `missingAssemblies=[]` and `loadErrors={}`, closing the public-surface coverage concern raised by that diagnostic.

## Bidirectional results

### Public and publicly exposed contracts

| Measure | Count | Result |
|---|---:|---|
| Public `Task`/`ValueTask` methods | 3,055 | Reviewed |
| Public async-stream methods | 27 | Reviewed |
| Public asynchronous contracts | 3,082 | Reviewed |
| Public methods whose name ends `Async` | 3,207 | Reviewed |
| Source-backed public task-like implementations | 2,459 | Classified |
| Async return without `Async`, true finding | 0 | PASS |
| `Async` suffix without async semantics, true finding | 0 | PASS |
| Externally owned/delegate naming exceptions | 27 | Justified |
| Async callback/task-input builder overloads | 128 | Justified |
| Synchronous-completion candidates manually adjudicated | 3 | 0 true findings |

The 2,459 implementations classified as 814 compiler state machines, 1,500 delegated asynchronous operations, 142 asynchronous contract/lifecycle implementations, and three synchronous-completion candidates requiring manual review.

The 128 `Async`-suffixed synchronous-return builder methods do not claim that the builder invocation itself suspends. In 97 cases the suffix selects a callback whose delegate returns an asynchronous type; in 31 cases it selects an overload accepting existing `Task`/`ValueTask`/async-stream work. The suffix is a necessary overload discriminator and is the explicit Review 11 callback/builder exception.

### Non-public product methods

| Measure | Count | Result |
|---|---:|---|
| Source-backed task-like implementations | 775 | Classified |
| Async return without `Async`, true finding | 0 | PASS |
| `Async` suffix without async semantics, true finding | 0 | PASS |
| Synchronous-completion candidates manually adjudicated | 23 | 0 true findings |

Classification: 452 compiler state machines, 217 delegated asynchronous operations, 83 asynchronous contract/lifecycle implementations, and 23 manually reviewed synchronous-completion candidates.

### Local functions

| Measure | Count | Result |
|---|---:|---|
| Source-backed task-like local functions | 185 | Classified |
| Async return without `Async`, true finding | 0 | PASS |
| `Async` suffix without async semantics, true finding | 0 | PASS |
| Async callback-builder exceptions | 2 | Justified |
| Synchronous-completion candidates manually adjudicated | 22 | 0 true findings |

Classification: 148 compiler state machines, 15 delegated asynchronous operations, and 22 manually reviewed synchronous-completion candidates.

## Required naming exceptions

Twenty-four exceptions are compiler-generated `Invoke` methods for public delegates whose declared return is `Task`/`ValueTask`. `Invoke` is a CLR delegate member name, not an author-controlled product API name. The scanner discovered 98 public delegate types and excluded only the 24 with asynchronous returns.

Three further exceptions are explicit implementations of externally owned Quartz contracts. Renaming them would break the external interface:

| Location | Exact symbol | Disposition |
|---|---|---|
| `src/Scheduling/ViciOne.ServiceBus.Quartz/QuartzIntegration/ScheduledMessageJob.cs:43` | `ViciOne.ServiceBus.Quartz.ScheduledMessageJob.Quartz.IJob.Execute(Quartz.IJobExecutionContext, CancellationToken)` | Required Quartz member; delegates to `ExecuteAsync`. |
| `src/Scheduling/ViciOne.ServiceBus.Quartz/QuartzIntegration/ViciOneServiceBusJobFactory.cs:37` | `ViciOne.ServiceBus.Quartz.ViciOneServiceBusJobFactory.Quartz.Extensibility.IJobFactory.CreateJob(TriggerFiredBundle, IScheduler, CancellationToken)` | Required Quartz member; delegates to `CreateJobAsync`. |
| `src/Scheduling/ViciOne.ServiceBus.Quartz/QuartzIntegration/ViciOneServiceBusJobFactory.cs:42` | `ViciOne.ServiceBus.Quartz.ViciOneServiceBusJobFactory.Quartz.Extensibility.IJobFactory.ReturnJob(JobScope, CancellationToken)` | Required Quartz member; delegates to `ReturnJobAsync`. |

## Manual adjudication of synchronous-completion candidates

### Public candidates

| Location and symbol | Adjudication |
|---|---|
| `src/Scheduling/ViciOne.ServiceBus.Quartz/QuartzIntegration/ViciOneServiceBusJobFactory.cs:53` — `ViciOneServiceBusJobFactory.CreateJobAsync(TriggerFiredBundle, IScheduler, CancellationToken)` | Intentional `ValueTask<JobScope>` lifecycle façade for Quartz `IJobFactory.CreateJob`; the current allocation-free fast path may complete synchronously. It rejects pre-cancellation and preserves one consistently awaitable job-factory contract. |
| `src/ViciOne.ServiceBus.Testing/BusTestHarness.cs:293` — `BusTestHarness.CleanAsync(CancellationToken)` | Intentional virtual asynchronous cleanup lifecycle. The base no-op rejects pre-cancellation; Azure Service Bus and RabbitMQ overrides perform real awaited broker cleanup and forward the token. |
| `src/ViciOne.ServiceBus.Testing/MediatorTestHarness.cs:89` — `MediatorTestHarness.StartAsync(CancellationToken)` | Intentional virtual test-harness start lifecycle. The current in-process mediator setup is a synchronous fast path, but the public lifecycle remains awaitable/extensible and rejects pre-cancellation. |

These are allowed asynchronous lifecycle abstractions, not fake-async methods.

### Non-public candidates

Every candidate below was inspected at its call site. The classification column states why synchronous completion is contractually correct.

| Location and exact symbol | Classification |
|---|---|
| `src/ViciOne.ServiceBus/Clients/ClientRequestHandle.cs:276` — `ClientRequestHandle<TRequest>.FaultHandlerAsync(ConsumeContext<Fault<TRequest>>)` | Task-returning message-handler callback. |
| `src/ViciOne.ServiceBus/Clients/ResponseHandlerConfigurator.cs:88` — `ResponseHandlerConfigurator<TResponse>.MessageHandlerAsync(ConsumeContext<TResponse>)` | `MessageHandler<TResponse>` callback alternative to the genuinely awaiting handler. |
| `src/ViciOne.ServiceBus/Consumers/Batching/BatchCollector.cs:82` — `BatchCollector<TMessage>.RemoveAsync(BatchConsumer<TMessage>)` | `Func<Task>` callback executed by the asynchronous serialized collector. |
| `src/ViciOne.ServiceBus/Consumers/Batching/BatchCollector.cs:189` — `BatchCollector<TMessage,TKey>.RemoveAsync(ConsumeContext<TMessage>, BatchConsumer<TMessage>)` | `Func<Task>` callback executed by the asynchronous serialized collector. |
| `src/ViciOne.ServiceBus/Contexts/Context/BaseConsumeContext.cs:464` — `BaseConsumeContext.ConsumeTaskAsync(Task)` | Registers and returns the actual in-flight task; it is an async-work adapter, not a completion wrapper. |
| `src/ViciOne.ServiceBus/Contexts/Context/MessageConsumeContext.cs:582` — `MessageConsumeContext<TMessage>.ConsumeTaskAsync(Task)` | Registers and returns the actual in-flight task. |
| `src/ViciOne.ServiceBus/Futures/Future.cs:479` — `Future<TCommand,TResult,TFault>.GetResultAsync(BehaviorContext<FutureState>)` | Task-returning response factory supplied to `RespondAsync`. |
| `src/ViciOne.ServiceBus/Futures/Future.cs:487` — `Future<TCommand,TResult,TFault>.GetFaultAsync(BehaviorContext<FutureState>)` | Task-returning response factory supplied to `RespondAsync`. |
| `src/ViciOne.ServiceBus/MessageData/Values/EmptyMessageData.cs:37` — `EmptyMessageData<T>.NoValueAsync()` | Implements the failure path behind the public task-valued `MessageData<T>.Value` contract. |
| `src/ViciOne.ServiceBus/SagaStateMachine/ViciOneServiceBusStateMachine.cs:281` — `ViciOneServiceBusStateMachine<TInstance>.DefaultUnhandledEventCallbackAsync(UnhandledEventContext<TInstance>)` | Task-returning state-machine callback contract; this default path throws. |
| `src/ViciOne.ServiceBus/SqlTransport/ConnectionContextSupervisor.cs:96` — `ConnectionContextSupervisor.CreateSendTransportAsync(Uri, SendTransportContext<ClientContext>)` | Private completion path of the public task-based send-transport provider contract. |
| `src/ViciOne.ServiceBus/Transports/ConsumerAgent.cs:71` — `ConsumerAgent<TKey>.HandleDeliveryCompleteAsync()` | Task-returning `ZeroActivity` event callback. |
| `src/ViciOne.ServiceBus/Transports/ConsumeSendEndpoint.cs:89` — `ConsumeSendEndpoint.ConsumeTaskAsync(Task)` | Registers and returns the actual in-flight task. |
| `src/ViciOne.ServiceBus/Transports/ReceiveEndpoint.cs:524` — `ReceiveEndpoint.EndpointHandle.SetReadyAsync(ReceiveEndpointReady)` | Task callback passed through `Connectable.ForEachAsync`. |
| `src/ViciOne.ServiceBus/Transports/ReceiveEndpoint.cs:534` — `ReceiveEndpoint.EndpointHandle.SetFaultedAsync(ReceiveEndpointFaulted)` | Task callback passed through `Connectable.ForEachAsync`. |
| `src/ViciOne.ServiceBus.Abstractions/Middleware/Agent.cs:107` — `Agent.StopAgentAsync(StopContext)` | Virtual asynchronous stop lifecycle; multiple concrete overrides await transport/agent shutdown. |
| `src/Transports/ViciOne.ServiceBus.ActiveMq/ActiveMqTransport/ConnectionContextSupervisor.cs:90` — `ConnectionContextSupervisor.CreateSendTransportAsync(ReceiveEndpointContext, ISessionContextSupervisor, IPipe<SessionContext>, string, DestinationType)` | Private completion path of the public task-based send-transport provider contract. |
| `src/Transports/ViciOne.ServiceBus.AmazonSqs/AmazonSqsTransport/ConnectionContextSupervisor.cs:119` — `ConnectionContextSupervisor.CreateTransportAsync(IClientContextSupervisor, SendTransportContext<ClientContext>)` | Private completion path of the public task-based send-transport provider contract. |
| `src/Transports/ViciOne.ServiceBus.AzureServiceBus/AzureServiceBusTransport/ConnectionContextSupervisor.cs:112` — `ConnectionContextSupervisor.CreateSendTransportAsync(Uri, SendSettings, ReceiveEndpointContext)` | Private completion path of the public task-based send-transport provider contract. |
| `src/Transports/ViciOne.ServiceBus.EventHubs/EventHubIntegration/EventHubDataReceiver.cs:59` — `EventHubDataReceiver.HandleErrorAsync(ProcessErrorEventArgs)` | Azure SDK task-returning event callback. |
| `src/Transports/ViciOne.ServiceBus.EventHubs/EventHubIntegration/EventHubProducerProvider.cs:64` — `EventHubProducerProvider.CreateProducerAsync(Uri)` | Task-returning producer factory supplied to the asynchronous producer cache. |
| `src/Transports/ViciOne.ServiceBus.RabbitMq/RabbitMqTransport/ConnectionContextSupervisor.cs:95` — `ConnectionContextSupervisor.CreateSendTransportAsync(ReceiveEndpointContext, IChannelContextSupervisor, ConfigureRabbitMqTopologyFilter<SendSettings>, string, RabbitMqEndpointAddress)` | Private completion path of the public task-based send-transport provider contract. |
| `src/Transports/ViciOne.ServiceBus.RabbitMq/RabbitMqTransport/RabbitMqBasicConsumer.cs:217` — `RabbitMqBasicConsumer.ObserveChannelShutdownAsync(object, ShutdownEventArgs)` | RabbitMQ task-returning shutdown-event callback. |

### Local-function candidates

All 22 are adapters supplied immediately to Task-returning delegate contracts; none is invoked as a misleading standalone synchronous helper.

| Location and exact local symbol | Classification |
|---|---|
| `src/ViciOne.ServiceBus/Configuration/Transformation/TransformSpecification.cs:85` — `PropertyProviderAsync(TransformPropertyContext<TProperty,TMessage>)` | Adapts a synchronous value provider to the asynchronous property-provider delegate. |
| `src/ViciOne.ServiceBus/SagaStateMachine/MessageFactory.cs:354,379,420` — `FactoryAsync(BehaviorContext<TSaga,TMessage>)` | Three synchronous-factory adapters to `ContextMessageFactory`'s Task-returning delegate. |
| `src/ViciOne.ServiceBus/SagaStateMachine/MessageFactory.cs:690,717,762` — `FactoryAsync(BehaviorExceptionContext<TSaga,TMessage,TException>)` | Three synchronous-factory adapters to the Task-returning delegate. |
| `src/ViciOne.ServiceBus/SagaStateMachine/MessageFactory.cs:1024,1047,1084` — `FactoryAsync(BehaviorContext<TSaga>)` | Three synchronous-factory adapters to the Task-returning delegate. |
| `src/ViciOne.ServiceBus/SagaStateMachine/MessageFactory.cs:1297,1322,1363` — `FactoryAsync(BehaviorExceptionContext<TSaga,TException>)` | Three synchronous-factory adapters to the Task-returning delegate. |
| `src/ViciOne.ServiceBus/SqlTransport/ScopeClientContextFactory.cs:49` — `CreateAsync(ConnectionContext, CancellationToken)` | Async context-factory delegate supplied to the supervisor. |
| `src/ViciOne.ServiceBus/SqlTransport/SharedClientContextFactory.cs:49` — `CreateAsync(ClientContext, CancellationToken)` | Async context-factory delegate supplied to the supervisor. |
| `src/Transports/ViciOne.ServiceBus.ActiveMq/ActiveMqTransport/ScopeSessionContextFactory.cs:92` — `CreateSessionContextAsync(SessionContext, CancellationToken)` | Async context-factory delegate supplied to the supervisor. |
| `src/Transports/ViciOne.ServiceBus.AmazonSqs/AmazonSqsTransport/ClientContextFactory.cs:61` — `CreateAsync(ConnectionContext, CancellationToken)` | Async context-factory delegate supplied to the supervisor. |
| `src/Transports/ViciOne.ServiceBus.AmazonSqs/AmazonSqsTransport/ScopeClientContextFactory.cs:49` — `CreateAsync(ClientContext, CancellationToken)` | Async context-factory delegate supplied to the supervisor. |
| `src/Transports/ViciOne.ServiceBus.AzureServiceBus/AzureServiceBusTransport/ClientContextFactory.cs:67` — `CreateAsync(ConnectionContext, CancellationToken)` | Async context-factory delegate supplied to the supervisor. |
| `src/Transports/ViciOne.ServiceBus.EventHubs/EventHubIntegration/ProcessorContextFactory.cs:76` — `CreateAsync(ConnectionContext, CancellationToken)` | Async context-factory delegate supplied to the supervisor. |
| `src/Transports/ViciOne.ServiceBus.EventHubs/EventHubIntegration/ProducerContextFactory.cs:65` — `CreateAsync(ConnectionContext, CancellationToken)` | Async context-factory delegate supplied to the supervisor. |
| `src/Transports/ViciOne.ServiceBus.RabbitMq/RabbitMqTransport/SharedChannelContextFactory.cs:81` — `CreateChannelContextAsync(ChannelContext, CancellationToken)` | Async context-factory delegate supplied to the supervisor. |

Two additional local methods named `OnPartitionInitializingAsync` and `OnPartitionClosingAsync` return `Func<...,Task>` rather than a task. They are callback selector/builders, so the suffix describes the returned async callback and is the same explicitly allowed builder exception applied to the public API.

### Changed test/observer contract

`tests/ViciOne.ServiceBus.Tests/InMemoryTransport/InMemoryDurableSendIntegrationTests.cs:235` now declares `ConsumerObservation : IReceiveObserver`. Its five methods are not fake async: `PreReceiveAsync` (line 246), `PostReceiveAsync` (line 248), `PostConsumeAsync<T>` (line 254), `ConsumeFaultAsync<T>` (line 257), and `ReceiveFaultAsync` (line 264) are exact implementations of the public Task-returning callback members declared at `src/ViciOne.ServiceBus.Abstractions/Observers/IReceiveObserver.cs:17,24,34,46,55`. Their synchronous completion is the explicitly allowed observer/callback case; changing their names would break the contract. The accompanying `SignalingDurableSendStore` methods either forward the actual task from `IDurableSendStore<IBus>` or await it before signaling the deterministic test barrier.

Focused .NET 10 / Microsoft Testing Platform v2 / xUnit v3 command:

```sh
env DOTNET_CLI_HOME=/private/tmp/vicione-sb-dotnet-home-10400 \
  DOTNET_ROOT=/private/tmp/vicione-dotnet-10-current \
  DOTNET_MULTILEVEL_LOOKUP=0 DOTNET_CLI_TELEMETRY_OPTOUT=1 \
  DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=1 \
  MSBUILDDISABLENODEREUSE=1 \
  NUGET_PACKAGES=/Users/edgar.liebold/.nuget/packages \
  /private/tmp/vicione-dotnet-10-current/dotnet test \
  --project tests/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj \
  --configuration Release --no-build --no-restore \
  --filter-class 'ViciOne.ServiceBus.Tests.InMemoryTransport.InMemoryDurableSendIntegrationTests'
```

Result: **3 passed, 0 failed, 0 skipped**.

## Cancellation evidence

The final compiled-surface inventory reported:

| Rule | Violations |
|---|---:|
| Async method without `Async` suffix | 0 |
| Async method without a `CancellationToken` or a documented context/callback exception | 0 |
| `CancellationToken` not last | 0 |
| `CancellationToken` parameter not named `cancellationToken` | 0 |

Reflection inventory command:

```sh
env DOTNET_CLI_HOME=/private/tmp/vicione-sb-dotnet-home-10400 \
  DOTNET_ROOT=/private/tmp/vicione-dotnet-10-current \
  DOTNET_MULTILEVEL_LOOKUP=0 DOTNET_CLI_TELEMETRY_OPTOUT=1 \
  DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=1 \
  MSBUILDDISABLENODEREUSE=1 \
  NUGET_PACKAGES=/Users/edgar.liebold/.nuget/packages \
  /private/tmp/vicione-dotnet-10-current/dotnet run \
  --property RestoreLockedMode=false \
  --property NuGetLockFilePath=/private/tmp/api-inventory-red-team-c-final-v5.packages.lock.json \
  tools/api-conventions/ApiInventory.cs -- \
  artifacts/sdk/bin /private/tmp/api-inventory-red-team-async-c-final-v5.json
```

Result: 4,091 public types, 2,881 emitted async methods, zero missing assemblies, zero load errors, and zero violations in the four rules above. The inventory recognized 719 documented callback/context-owned cancellation-token exceptions; these are exceptions to an explicit parameter, not unclassified omissions.

The architecture guard that checks every body-bearing public method consumes or forwards each declared token was then run directly on the frozen Release output:

```sh
env DOTNET_CLI_HOME=/private/tmp/vicione-sb-dotnet-home-10400 \
  DOTNET_ROOT=/private/tmp/vicione-dotnet-10-current \
  DOTNET_MULTILEVEL_LOOKUP=0 DOTNET_CLI_TELEMETRY_OPTOUT=1 \
  DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=1 \
  MSBUILDDISABLENODEREUSE=1 \
  NUGET_PACKAGES=/Users/edgar.liebold/.nuget/packages \
  /private/tmp/vicione-dotnet-10-current/dotnet test \
  --project tests/Architecture/ViciOne.ServiceBus.Architecture.Tests/ViciOne.ServiceBus.Architecture.Tests.csproj \
  --configuration Release --no-build --no-restore \
  --filter-method 'ViciOne.ServiceBus.Architecture.Tests.Product.CancellationTokenFlowArchitectureTests.PublicMethods_ConsumeOrForwardEveryDeclaredCancellationToken'
```

Result: **1 passed, 0 failed, 0 skipped**. Manual review additionally confirmed cancellation at the three public synchronous-completion lifecycle candidates and at the Quartz explicit-interface bridges.

## Rename false-negative controls

The scan discovered assemblies and symbols from the current solution and Roslyn compilations. It did not match the old product identity to decide inclusion. The following final-freeze checks produced no source namespace, assembly-name, root-namespace, or package-ID matches for the legacy product identity or obsolete transport namespace identities. Matches in folder names such as `QuartzIntegration` and `EntityFrameworkCoreIntegration` were deliberately not treated as namespaces.

```sh
rg -n '(^|[^A-Za-z0-9_])MassTransit([^A-Za-z0-9_]|$)|<AssemblyName>MassTransit|<RootNamespace>MassTransit|<PackageId>MassTransit' \
  src --glob '*.cs' --glob '*.csproj' --glob '*.props' --glob '*.targets'

rg -n '^namespace (MassTransit|ViciOne\.ServiceBus\.(RabbitMqTransport|RabbitMQ|ActiveMqTransport|ActiveMQ|AmazonSqsTransport|AmazonSQS|Azure\.ServiceBus\.Core|EventHubIntegration|EntityFrameworkCoreIntegration|DynamoDbIntegration|QuartzIntegration))([.;]|$)' \
  src --glob '*.cs'

find src -type f -name '*.csproj' -print0 | sort -z | xargs -0 \
  rg -n '<(AssemblyName|RootNamespace|PackageId)>[^<]*(MassTransit|RabbitMQ|ActiveMQ|AmazonSQS|Azure\.ServiceBus)'
```

The four genuine fake-async candidates found during the moving pre-freeze review were also rechecked by their former names:

```sh
rg -n 'DiscardPendingConsumerMessagesAsync|CreateConnectionAsync|GenerateOutputAsync' \
  src/ViciOne.ServiceBus/Middleware/Outbox/InMemoryOutboxConsumeContext.cs \
  src/Transports/ViciOne.ServiceBus.ActiveMq/ActiveMqTransport/ConnectionContextFactory.cs \
  src/Transports/ViciOne.ServiceBus.AzureServiceBus/AzureServiceBusTransport/ConnectionContextFactory.cs \
  src/ViciOne.ServiceBus.Testing/TestActivityListener.cs
```

Result: no matches. Their final forms are synchronous `DiscardPendingConsumerMessages`, `CreateConnection`, and `GenerateOutput`; they are not hidden by the final exception classifications.

### Canonical transport-selection delta

The final delta moved the public transport-choice entry points; it did not change asynchronous behavior. Symbol and source inspection found exactly these nine files, all declaring `namespace ViciOne.ServiceBus.Configuration;`:

| File | Entry-point family |
|---|---|
| `src/ViciOne.ServiceBus/InMemoryTransport/InMemoryConfigurationExtensions.cs` | `CreateUsingInMemory` / `UsingInMemory` |
| `src/Transports/ViciOne.ServiceBus.ActiveMq/Configuration/ActiveMqBusFactoryConfiguratorExtensions.cs` | `CreateUsingActiveMq` / `UsingActiveMq` |
| `src/Transports/ViciOne.ServiceBus.AmazonSqs/Configuration/AmazonSqsBusFactoryConfiguratorExtensions.cs` | `CreateUsingAmazonSqs` / `UsingAmazonSqs` |
| `src/Transports/ViciOne.ServiceBus.AzureServiceBus/AzureBusFactory.cs` | `CreateUsingServiceBus` |
| `src/Transports/ViciOne.ServiceBus.AzureServiceBus/Configuration/ServiceBusConfigurationExtensions.cs` | `CreateUsingAzureServiceBus` / `UsingAzureServiceBus` |
| `src/Transports/ViciOne.ServiceBus.EventHubs/EventHubIntegrationExtensions.cs` | `UsingEventHub` |
| `src/Transports/ViciOne.ServiceBus.RabbitMq/Configuration/RabbitMqBusFactoryConfiguratorExtensions.cs` | `CreateUsingRabbitMq` / `UsingRabbitMq` |
| `src/Transports/ViciOne.ServiceBus.SqlTransport.PostgreSql/Configuration/PostgresBusFactoryConfiguratorExtensions.cs` | `UsingPostgres` |
| `src/Transports/ViciOne.ServiceBus.SqlTransport.SqlServer/Configuration/SqlServerBusFactoryConfiguratorExtensions.cs` | `UsingSqlServer` |

These selectors return `IBusControl` or `void` and perform synchronous configuration selection; none is an asynchronous operation missing an `Async` suffix. `CreateUsingServiceBus` likewise has no misleading `Async` suffix. The full Roslyn run loaded the declarations from their new namespaces, so the move did not remove them from either naming direction.

The pattern-based architecture guard independently discovered the same nine source files and enforced the canonical namespace:

```sh
env DOTNET_CLI_HOME=/private/tmp/vicione-sb-dotnet-home-10400 \
  DOTNET_ROOT=/private/tmp/vicione-dotnet-10-current \
  DOTNET_MULTILEVEL_LOOKUP=0 DOTNET_CLI_TELEMETRY_OPTOUT=1 \
  DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=1 \
  MSBUILDDISABLENODEREUSE=1 \
  NUGET_PACKAGES=/Users/edgar.liebold/.nuget/packages \
  /private/tmp/vicione-dotnet-10-current/dotnet test \
  --project tests/Architecture/ViciOne.ServiceBus.Architecture.Tests/ViciOne.ServiceBus.Architecture.Tests.csproj \
  --configuration Release --no-build --no-restore \
  --filter-method 'ViciOne.ServiceBus.Architecture.Tests.Product.ApiSurfaceArchitectureTests.FiveApiLayers_HaveEnforcedDiscoveryAndPackageBoundaries'
```

Result on the final post-format snapshot: **1 passed, 0 failed, 0 skipped**.

## Engineering prerequisites supplied with the freeze

The engineering lead supplied these final post-format freeze results:

- `dotnet format --verify-no-changes`: exit 0;
- strict Release/no-incremental build of the complete `ViciOne.ServiceBus.Engineering.slnx`: 0 warnings, 0 errors, 2m 54.69s;
- three complete UnitArchitecture runs: each 3,502 of 3,502 passed, 0 skipped (2m 56.634s, 2m 49.510s, 2m 50.262s);
- post-format complete-profile rebind: 3,502 of 3,502 passed, 0 skipped, 3m 01.184s;
- package gate: 14 of 14 journeys, eight freshly packed NuGet packages, 0 warnings, 0 errors;
- canonical package output: 22 packages;
- API baseline: 20,769 lines, SHA-256 `1a2dd8d8fd7ed98b77b5d920c480356bd162580ef0391dfecc7a06340d68bf71`;
- canonical compiled inventory: SHA-256 `36721d4ac0ec06b356dc1895a204c8082ba22c0ebac22faa9f5954b32f1451f5`, 2,881 async methods, and zero suffix/token-position/token-name violations.

The previously communicated `bf9f…` inventory hash belonged to the pre-format intermediate state and is not final evidence. The canonical post-format hash above is the one bound here; the fresh Red Team rerun produced identical metrics and a distinct raw-file hash because its timestamp and output path differ.

These results establish the frozen build/test prerequisite but remain implementation-team evidence, not independent validation. This internal Red Team run separately repeated the semantic inventory, compiled-assembly reflection inventory, fingerprint checks, focused cancellation-flow test, changed durable-send test class, and nine-file transport-selection architecture guard described above.

## Limitations

- This is a static semantic and compiled-metadata naming audit plus focused architecture and contract tests. It is not a runtime liveness, scheduler, deadlock, throughput, or broker acceptance test.
- A method may legitimately complete synchronously on a fast/no-op path while implementing a task-based callback or lifecycle. Such cases were accepted only after inspecting the contract and call site; bare `Task.CompletedTask`/`Task.FromResult` was not accepted by itself.
- The cancellation architecture test proves that each declared public token is consumed or forwarded syntactically. Reflection proves parameter presence, position, and name. These checks do not constitute exhaustive interprocedural proof of every downstream library's cancellation behavior.
- Generated code, general test/support code, samples, tooling, `review/**`, prior evidence, and build output were outside the product-wide naming inventory. The changed durable-send test and its observer interface contract were included explicitly.
- The review was performed internally and must not be represented as an independent Red Team acceptance.

## Final conclusion

On the source fingerprint recorded above, the canonical post-rename product surface is bidirectionally consistent: asynchronous public contracts are named `Async`, `Async`-named public APIs represent asynchronous work or a justified asynchronous callback/lifecycle abstraction, and the same condition holds for inspected non-public methods, local functions, the changed durable-send observer test contract, and all nine canonical transport-selection entry-point files. All suspicious synchronous-completion wrappers were resolved by semantic call-site review, leaving **zero true findings**.
