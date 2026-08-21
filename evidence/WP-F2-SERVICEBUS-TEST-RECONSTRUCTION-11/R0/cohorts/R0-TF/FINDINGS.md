# R0-TF — Findings

Cohort `R0-TF` (`src/ViciOne.ServiceBus.TestFramework`, 147 tracked files, 144 `.cs`, 5474
lines), baseline commit `ae73c6da748e3bc3257dffa4971ee8680e086207`.
Every statement below names the file and, where a count is given, how it was derived. Findings
are read out of the code, not out of file or type names.

## 1. Explicitly flagged categories (plan section 10)

Plan section 10 names four things that must not be carried over as shared infrastructure and
one that stays only as semantic evidence. All five are present in this cohort.

### F-01 — NUnit-internal loggers (4 files, all `REMOVE`)

`Logging/TestOutputLogger.cs`, `Logging/TestOutputLoggerFactory.cs`,
`Logging/TestOutputListenerObserver.cs`, `Logging/DiagnosticListenerObserver.cs`.
They write through `NUnit.Framework.Internal.TestExecutionContext` and
`NUnit.Framework.TestContext.Out` — NUnit internal API — and beyond the framework binding they
carry four defects that must not be reproduced:

- `TestOutputLoggerFactory.Current` is public mutable state, written from static fixture code
  in `BusTestFixture`, `MediatorTestFixture`, `InMemoryTestFixture` and
  `InMemoryContainerTestFixture` and read concurrently by every logger — output from one
  fixture can land in another test's output.
- `TestOutputLogger.BeginScope` stores the state in a field that is never read and returns a
  no-op disposable, so logging scopes silently do nothing.
- `TestOutputLoggerFactory.AddProvider` is a silent no-op — a registered provider is dropped
  without error.
- `DiagnosticListenerObserver.OnNext` reassigns its subscription handle while the disposal of
  the previous one is commented out (line 25), leaking a subscription per listener for the
  process lifetime; and `TestOutputListenerObserver.OnNext` dereferences `Activity.Current`
  without a null check, so a `.Start`/`.Stop` event outside an activity scope throws
  `NullReferenceException` inside a diagnostic observer.

All four also format with `DateTime.Now` (local time) although the target process runs with
`TZ=UTC` per plan section 7.

### F-02 — Old random helper (1 file, `REMOVE`)

`ThreadSafeRandom.cs` has **zero references anywhere in the repository**, including inside the
TestFramework itself (`git grep -lw ThreadSafeRandom` returns only its own file). The
technical reason for removal is not the absence of callers: the seed is drawn from a
process-global `Random` and is never recorded or reported, so a failure caused by a particular
draw cannot be reproduced — precisely what plan section 11 forbids. Both capabilities it
offers already exist: `Random.Shared` is thread-safe on net10.0, and a reproducible test needs
a seeded `Random` the test owns and prints.

### F-03 — Domain-specific collection helpers / sample domains

Two sample domains are present and neither becomes shared infrastructure:

- **Fast food (ForkJoint), 67 files** — `ForkJoint/{Activities,Consumers,Contracts,Futures,
  ItineraryPlanners,Services,Tests}`. It stays as semantic evidence exactly as plan section 10
  says. Its real content is the 16 inherited future obligations; the burger, fry and shake
  vocabulary does not survive into any target project. Externally it is reached only through
  the `*_Specs` fixtures, with one exception: `Azure.ServiceBus.Core.Tests/Future_Specs.cs`
  imports `TestFramework.ForkJoint.{Consumers,Contracts,Futures}` directly and registers
  `CookFryConsumer`, `CookFryConsumerDefinition`, `OrderFry` and `FryFuture`.
- **Choir (`Sagas/ChoirTest.cs`)** — a sample domain that nevertheless carries a genuine
  product obligation (four concurrent events against one instance, `CompositeEvent`,
  `ISagaVersion`). Two owners use it. Disposed to the owning test projects, see
  open question Q-3 in `RECONCILIATION.md`.

### F-04 — Unimplemented `TestConsumeContext` paths

`TestConsumeContext.cs` (294 lines) is the single largest correctness risk in the cohort:

- **24 members throw `NotImplementedException`**: all ten `IPublishEndpoint.Publish`
  overloads, all ten `RespondAsync`/`Respond` overloads and `GetSendEndpoint`. Any product
  path a test drives into publish or respond fails with `NotImplementedException` rather than
  with a statement about the product.
- Ten context properties (`Headers`, `SerializerContext`, `RequestId`, `CorrelationId`,
  `ConversationId`, `InitiatorId`, `ExpirationTime`, `ResponseAddress`, `FaultAddress`,
  `SentTime`) are auto-properties that are never assigned, so they silently read `null`;
  `Host` is explicitly `null`.
- `ConsumeCompleted` returns `Task.FromResult(true)` unconditionally and `AddConsumeTask`
  discards the task — a consume that never completed reads as completed.
- `TestConsumeContext.GetContext()` caches one `InMemoryReceiveEndpointContext` in a static
  field for the process lifetime, never disposed; `Build()` also constructs an `InMemoryHost`
  into a local that is immediately discarded.

Seventeen files in `ViciOne.ServiceBus.Tests` consume it, plus `TestStateMachineExtensions`.

## 2. Contract breaches against the plan (report now, not at the freeze)

### F-05 — The TestFramework is a packable NuGet package carrying NUnit

`src/Directory.Build.props` sets `IsPackable=True` for every project under `src/`, and
`ViciOne.ServiceBus.TestFramework.csproj` adds `PackageTags` `ViciOne.ServiceBus;NUnit` and a
`Description` naming NUnit. Its direct `PackageReference`s are `NUnit` and `NUnit.Analyzers`.
A test framework therefore sits in the product package graph today. Plan section 6 no. 1
forbids the direct `NUnit`/`NUnit.Analyzers` reference in the target state.

### F-06 — `GitHubActionsTestLogger` 3.0.5 is a Direct dependency of a packable src project

`src/ViciOne.ServiceBus.TestFramework/packages.lock.json` lists
`"GitHubActionsTestLogger": {"type": "Direct", "requested": "[3.0.5, )"}` although the
`.csproj` never names it — it arrives through a `GlobalPackageReference`. Plan section 6
no. 1 excludes every VSTest-specific test logger from product, package, lock, publish and
non-executable tool graphs *regardless of reference kind*, and states that the global
`GitHubActionsTestLogger` 3.0.5 disappears entirely. `RestorePackagesWithLockFile` and
`RestoreLockedMode` are true repository-wide, so the lock file is binding for every restore.

### F-07 — A log4net configuration for a framework that is not present

`ViciOne.ServiceBus.TestFramework.log4net.xml` configures a console appender and two rolling
file appenders. No project in the repository references log4net (the lock file lists no
log4net package), no code reads the file, and the `.csproj` has no `Content`/`None` item for
it, so it is never copied to any output directory.

## 3. Semantic gaps and latent defects

### F-08 — The per-test time budget does not reach container tests

`AsyncTestHarness.BeginTestScope()` exists because a shared budget made a test that passes
alone in 60.3 s fail after 0.037 s (documented in the product source). `AsyncTestFixture`
binds it with `[SetUp]`. **`InMemoryContainerTestFixture` does not derive from
`AsyncTestFixture`**, so `BeginTestScope` is never called for it — the documented defect is
still live for the 19 container spec files that use it. Two further defects in the same file:
`TestCancelledTask.ContinueWith` is fire-and-forget behind `#pragma warning disable 4014`, so
a failure in a cancellation callback is unobserved; and two different `TestOutputLoggerFactory`
instances exist (a static one whose `Current` is steered and a second registered in DI), so
the steering applies to the instance that is not writing the log.

### F-09 — Ambient environment steers what the tests do

`BusTestFixture` and `MediatorTestFixture` read the `CI` environment variable to decide
whether logging is enabled, and `BusTestFixture` reads `DIAG` to decide whether to subscribe a
process-wide `DiagnosticListener.AllListeners` observer (guarded by `Interlocked`, never
disposed). Plan section 11 forbids tests changing their own behaviour from process
environment. `FutureTestFixture.ConfigureLogging` and `BusTestFixture.ConfigureBusDiagnostics`
additionally write the process-global `LogContext` from fixture code.

### F-10 — `HealthCheckServiceExtensions.WaitForHealthStatus` throws `NullReferenceException`
### on its own diagnostic path

`HealthReport report = default;` initialises the variable to `null`. If the very first
`CheckHealthAsync` is cancelled, the `catch (OperationCanceledException)` block evaluates
`report.Entries` and `report.Status`, both of which dereference `null`. The carefully written
diagnostic message is therefore replaced by a `NullReferenceException` in exactly the case it
was written for. Five files in four projects call it.

### F-11 — `TestInstance.CurrentState` is typed `State`, so it cannot be persisted

`Sagas/TestInstance.cs` declares `public State CurrentState { get; set; }` — the automaton
state object, not a persistable primitive. This is the concrete, code-level reason the
`EntityFrameworkCoreIntegration.Tests`, `Azure.Table.Tests`, `DynamoDbIntegration.Tests` and
`Azure.ServiceBus.Core.Tests` projects each declare a *private duplicate* `TestInstance` (with
a `string CurrentState`) and a private duplicate `TestStateMachineSaga`, rather than
referencing the shared ones. Verified by reading the declarations in
`tests/Persistence/ViciOne.ServiceBus.Azure.Table.Tests/Saga/Container_Specs.cs:72` and
`tests/Persistence/ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Tests/Container_Specs.cs:199`.
The rebuilt shared instance must expose a persistable state property so one definition serves
every repository.

### F-12 — `PingMessage` gives every default instance the same correlation id

`Messages/PingMessage.cs` line 11: `Guid _id = new Guid("D62C9B1C-8E31-4D54-ADD7-C624D56085A4");`.
Every default-constructed `PingMessage` in all 149 consuming files shares one correlation id.
Additionally `GetHashCode()` is computed from `CorrelationId`, which has a public setter, so an
instance's hash changes after it has been placed in a hash container. `PongMessage` and
`PingNotSupported` have the same `GetHashCode` problem and use an unseeded, unreported
`Guid.NewGuid()`.

### F-13 — `Grill` never stores a newly cooked patty, and matches by reference

`ForkJoint/Services/Grill.cs`: `CookOrUseExistingPatty` creates a new `BurgerPatty` and returns
it **without adding it to `_patties`**, so the "use existing" branch can only ever match an
instance that a compensation put back. `BurgerPatty` overrides neither `Equals` nor
`GetHashCode`, so the `HashSet<BurgerPatty>` matches by reference regardless. The grill is
registered as a singleton, making the inventory shared mutable state across all tests of a
fixture.

### F-14 — `TestStateMachineExtensions` squats the product namespace, and one method ignores
### the instance state

`TestStateMachineExtensions.cs` is declared in `namespace ViciOne.ServiceBus`, not in the
TestFramework namespace, so its extension methods attach themselves to product types without
the consumer importing a test namespace. `TransitionToState` builds a `LastBehavior` over a
`TransitionActivity` but constructs the behavior context from `machine.Initial.Enter`, so the
transition executes from the initial-enter event regardless of the instance's actual current
state.

### F-15 — Unverified and unused surface inside the cohort

| What | Where | Observation |
|---|---|---|
| `BatchExpiry` | `Futures/BatchRequest.cs` | declared, never read by `BatchFuture` or by any test — either a missing batch-expiry obligation or dead surface |
| `ConcurrentMessageLimit = 32` | `ForkJoint/Consumers/CookFryConsumerDefinition.cs` | no test asserts the limit is honoured — unverified configuration |
| `FryShakeReady` | `ForkJoint/Contracts/FryShakeReady.cs` | never sent or received by any future or consumer in the cohort |
| `GuidValue` | `Courier/SetVariableArguments.cs` | declared, never read by `SetVariableActivity` |
| `IFryer`/`Fryer` | all three fixtures in `Futures/Tests/PriceCalculationFuture_Specs.cs` | registered although nothing in those tests uses a fryer — copied ceremony from the ForkJoint fixtures |
| `LinesFaulted` | `ForkJoint/Contracts/OrderFaulted.cs` | declared as `IDictionary<Guid, Fault<OrderLine>>`, but `OrderFuture.MapOrderFaulted` builds `Dictionary<Guid, Fault>`; no assertion reads it, so the mismatch is unverified |
| `BusFactoryConfigurator` | `tests/.../RabbitMqActivityTestFixture.cs:29` | the only implementation of `ActivityTestContextConfigurator`, a private nested class that is never instantiated |
| `DeleteMessage` | `Messages/DeleteMessage.cs` and `tests/ViciOne.ServiceBus.Tests/Messages/DeleteMessage.cs` | two identical empty marker classes, neither referenced; the name also collides with the `DeleteMessage` transport operation on the SQL and Amazon SQS client contexts |

### F-16 — Assertion gaps inside the 16 inherited cases

Read per assertion, not per method:

- `OrderFuture_Specs.Should_fault_with_lettuce` binds `out Response<OrderFaulted> faulted` and
  never inspects it; `Should_fault_with_lettuce_durably` discards it with `_` twice. All three
  order-fault cases establish only *that* the order faulted, never *what* it reported — so
  `OrderFaulted.LinesCompleted`, `LinesFaulted` and `Exceptions` are entirely unasserted.
- `ShakeFuture_Specs.Should_complete` asserts size but not `Flavor`, although the response
  contract carries it and the future composes the description from it.
- `PriceCalculationFuture_Specs.Should_complete` and `..._RegistrationSpecs.Should_complete`
  assert only the correlated order line id; the consumer's fixed `Amount` of 1234.55 is never
  checked, so a future that returned a wrong amount would pass.
- `PriceCalculationFuture_Faulted.Should_faulted` asserts the exception *type* but not the
  message `"The sku was invalid"`.
- `BurgerFuture_Specs.Should_complete` asserts `Cheese` and `Weight` but not the option flags
  that `Burger` defaults to `true` (`Pickle`, `Mustard`), so a future that lost the defaults
  would pass.
- `FryShakeFuture_Specs.Should_complete` asserts the composed literal `"FryShake(2)"`, which
  couples the assertion to a string format rather than to a result count.

### F-17 — Fixed waits used as synchronisation (plan section 11)

- `Futures/ProcessBatchItemConsumer.cs`: `Task.Delay(2000)` for the magic job number `"Delay"`.
- `Courier/TestActivity.cs`: sets a one second completion `Delay`, but only when a
  `MessageSchedulerContext` payload happens to be present — an ambient probe that changes
  behaviour with bus configuration.
- `HealthCheckServiceExtensions`: a 50 ms `Task.Delay` poll loop (bounded, but still a fixed
  wait as synchronisation).

### F-18 — Copy errors and console output

- `Courier/ReviseItineraryActivity.cs` prints `"ReviseToEmptyItineraryActivity: Execute"` —
  the wrong class name.
- `Courier/ObjectGraphTestActivity.cs` throws `ArgumentException("dateTimeValue")` from the
  check that guards the **decimal** value, and performs its expectation checks *inside the
  activity*, so a mismatch surfaces as a routing-slip fault instead of a named expectation.
- `ForkJoint/Services/ShakeMachine.cs` compares the flavor with
  `StringComparison.InvariantCultureIgnoreCase` on what is a protocol value; `OrdinalIgnoreCase`
  is correct.
- `ForkJoint/Futures/ComboFuture.cs` and `CalculateFuture.cs` dereference
  `SelectResults<T>().FirstOrDefault()` without a null check.
- `ForkJoint/Futures/FryFuture.cs` uses `context.Saga.Completed ?? default`, masking a saga
  that was never marked completed as `DateTime.MinValue`.
- `Console.WriteLine` is used as test output in 14 Courier activities and in
  `Sagas/ChoirTest.cs`.
- `ForkJoint/ItineraryPlanners/BurgerItineraryPlanner.cs` carries a `// TODO create a future
  with address/id` and publishes `OrderOnionRings` without awaiting a result, so an onion-ring
  failure cannot fault the burger.
- `Sagas/StartStateMachineTest.cs` declares the type `StartTest` — file and type name disagree.
- `async` without `await` (CS1998) in `FaultyActivity`, `SecondTestActivity`, `Fryer`,
  `MediatorTestFixture.TearDownMediatorTestFixture` and several Courier activities.
- `InMemoryTestFixture` and `MediatorTestFixture` each carry an empty `[SetUp]` and `[TearDown]`
  pair returning `Task.CompletedTask` — ceremonial hooks with no effect.
- `MediatorTestFixture` disposes the harness at teardown but never calls `Stop()`, unlike every
  other fixture in the cohort.

### F-19 — A near-verbatim duplicate of a shared fixture already exists downstream

`tests/Transports/ViciOne.ServiceBus.RabbitMqTransport.Tests/RabbitMqActivityTestFixture.cs`
reproduces `InMemoryActivityTestFixture` almost line for line (same dictionary, same
`AddActivityContext`/`GetActivityContext`/`SetupActivities`). The rebuilt shared capability
must be transport-neutral so the copy disappears rather than being ported.

## 4. Public surface consumed from outside the TestFramework (TLP-009)

Method: text search is candidate discovery only. A candidate counts as a confirmed reference
only when the consuming file (a) imports the declaring TestFramework namespace or qualifies
the name with it, **and** (b) does not itself declare a type of the same simple name, **and**
(c) belongs to a project whose `.csproj` carries the `ProjectReference` to
`ViciOne.ServiceBus.TestFramework`. All 12 referencing projects were confirmed from their
`.csproj` files:

`tests/ViciOne.ServiceBus.Tests`, `tests/Persistence/{Azure.Table, DynamoDbIntegration,
EntityFrameworkCoreIntegration}.Tests`, `tests/Scheduling/QuartzIntegration.Tests`,
`tests/Transports/{ActiveMqTransport, AmazonSqsTransport, Azure.ServiceBus.Core,
EventHubIntegration, RabbitMqTransport, SqlTransport}.Tests`, `tests/ViciOne.ServiceBus.SignalR.Tests`.

**F-20 — one of the twelve references nothing.** `tests/ViciOne.ServiceBus.SignalR.Tests`
carries the `ProjectReference` at line 17 of its `.csproj`, but `git grep -n TestFramework --
'tests/ViciOne.ServiceBus.SignalR.Tests/**'` returns only that `.csproj` line — not a single
source file in the project imports or names anything from this cohort. The reference is dead
and can be dropped independently of the rebuild. At the other end,
`tests/Transports/ViciOne.ServiceBus.SqlTransport.Tests` uses exactly five types, all of them
in two files (`RoutingSlip_Specs.cs`: `TestActivity`, `TestArguments`, `TestLog`;
`Request_Specs.cs`: `PingMessage`, `PongMessage`).

Step (b) matters: naive text search reports `Sagas.TestInstance` in 6 projects and
`Sagas.TestStateMachineSaga` in 6, but four of those projects declare their own copies — the
confirmed count is 2 files in 1 project each. Likewise `SubmitOrder`, `Size`, `MessageA`,
`MessageB` and `Data` produce text hits in the transport projects that are all locally
declared types, not references into this cohort.

### 4.1 Types referenced by name

`TF` = `ViciOne.ServiceBus.TestFramework`. "Owners" counts distinct consuming test projects.

| Type | Owners | Files | Consuming projects | Single consuming file (where 1–2) |
|---|---:|---:|---|---|
| `TF.Messages.PingMessage` | 8 | 149 | ActiveMqTransport, AmazonSqsTransport, Azure.ServiceBus.Core, EntityFrameworkCoreIntegration, QuartzIntegration, RabbitMqTransport, SqlTransport, Tests |  |
| `TF.InMemoryTestFixture` | 7 | 150 | Azure.Table, DynamoDbIntegration, EntityFrameworkCoreIntegration, EventHubIntegration, QuartzIntegration, RabbitMqTransport, Tests |  |
| `TF.IntentionalTestException` | 7 | 72 | ActiveMqTransport, AmazonSqsTransport, Azure.ServiceBus.Core, EntityFrameworkCoreIntegration, QuartzIntegration, RabbitMqTransport, Tests |  |
| `TF.Messages.PongMessage` | 7 | 56 | ActiveMqTransport, AmazonSqsTransport, Azure.ServiceBus.Core, QuartzIntegration, RabbitMqTransport, SqlTransport, Tests |  |
| `TF.BusTestFixture` | 6 | 39 | ActiveMqTransport, AmazonSqsTransport, Azure.ServiceBus.Core, EntityFrameworkCoreIntegration, RabbitMqTransport, Tests |  |
| `TF.Sagas.StartTest` | 6 | 7 | Azure.ServiceBus.Core, Azure.Table, DynamoDbIntegration, EntityFrameworkCoreIntegration, EventHubIntegration, Tests |  |
| `TF.Sagas.TestStarted` | 5 | 6 | Azure.ServiceBus.Core, Azure.Table, DynamoDbIntegration, EntityFrameworkCoreIntegration, Tests |  |
| `TF.Courier.TestArguments` | 4 | 21 | QuartzIntegration, RabbitMqTransport, SqlTransport, Tests |  |
| `TF.Courier.TestLog` | 4 | 21 | QuartzIntegration, RabbitMqTransport, SqlTransport, Tests |  |
| `TF.Courier.TestActivity` | 4 | 20 | QuartzIntegration, RabbitMqTransport, SqlTransport, Tests |  |
| `TF.ForkJoint.Tests.FryFuture_Specs` | 4 | 4 | Azure.ServiceBus.Core, Azure.Table, EntityFrameworkCoreIntegration, Tests |  |
| `TF.IFutureTestFixtureConfigurator` | 4 | 4 | Azure.ServiceBus.Core, Azure.Table, EntityFrameworkCoreIntegration, Tests |  |
| `TF.Sagas.TestUpdated` | 4 | 4 | Azure.ServiceBus.Core, DynamoDbIntegration, EntityFrameworkCoreIntegration, Tests |  |
| `TF.AsyncTestFixture` | 3 | 14 | Azure.ServiceBus.Core, RabbitMqTransport, Tests |  |
| `TF.ForkJoint.Tests.BurgerFuture_Specs` | 3 | 3 | Azure.Table, EntityFrameworkCoreIntegration, Tests |  |
| `TF.ForkJoint.Tests.CalculateFuture_Specs` | 3 | 3 | Azure.Table, EntityFrameworkCoreIntegration, Tests |  |
| `TF.ForkJoint.Tests.ComboFuture_Specs` | 3 | 3 | Azure.Table, EntityFrameworkCoreIntegration, Tests |  |
| `TF.ForkJoint.Tests.FryShakeFuture_Specs` | 3 | 3 | Azure.Table, EntityFrameworkCoreIntegration, Tests |  |
| `TF.ForkJoint.Tests.OrderFuture_Specs` | 3 | 3 | Azure.Table, EntityFrameworkCoreIntegration, Tests |  |
| `TF.Futures.Tests.PriceCalculationFuture_Faulted` | 3 | 3 | Azure.Table, EntityFrameworkCoreIntegration, Tests |  |
| `TF.Futures.Tests.PriceCalculationFuture_RegistrationSpecs` | 3 | 3 | Azure.Table, EntityFrameworkCoreIntegration, Tests |  |
| `TF.Futures.Tests.PriceCalculationFuture_Specs` | 3 | 3 | Azure.Table, EntityFrameworkCoreIntegration, Tests |  |
| `TF.ForkJoint.Tests.ShakeFuture_Specs` | 3 | 3 | Azure.Table, EntityFrameworkCoreIntegration, Tests |  |
| `TF.InMemoryActivityTestFixture` | 2 | 20 | QuartzIntegration, Tests |  |
| `TF.Courier.FaultyActivity` | 2 | 7 | QuartzIntegration, Tests |  |
| `TF.Courier.FaultyArguments` | 2 | 7 | QuartzIntegration, Tests |  |
| `TF.Courier.SecondTestActivity` | 2 | 7 | RabbitMqTransport, Tests |  |
| `TF.Courier.FaultyLog` | 2 | 6 | QuartzIntegration, Tests |  |
| `TF.ActivityTestContext` | 2 | 3 | RabbitMqTransport, Tests |  |
| `TF.Sagas.ChoirConcurrency.Baritone` | 2 | 2 | DynamoDbIntegration, Tests | `tests/Persistence/ViciOne.ServiceBus.DynamoDbIntegration.Tests/Choir_Specs.cs` |
| `TF.Sagas.ChoirConcurrency.Bass` | 2 | 2 | DynamoDbIntegration, Tests | `tests/Persistence/ViciOne.ServiceBus.DynamoDbIntegration.Tests/Choir_Specs.cs` |
| `TF.Sagas.ChoirConcurrency.ChoirState` | 2 | 2 | DynamoDbIntegration, Tests | `tests/Persistence/ViciOne.ServiceBus.DynamoDbIntegration.Tests/Choir_Specs.cs` |
| `TF.Sagas.ChoirConcurrency.ChoirStateMachine` | 2 | 2 | DynamoDbIntegration, Tests | `tests/Persistence/ViciOne.ServiceBus.DynamoDbIntegration.Tests/Choir_Specs.cs` |
| `TF.Sagas.ChoirConcurrency.Countertenor` | 2 | 2 | DynamoDbIntegration, Tests | `tests/Persistence/ViciOne.ServiceBus.DynamoDbIntegration.Tests/Choir_Specs.cs` |
| `TF.Courier.FirstFaultyActivity` | 2 | 2 | QuartzIntegration, Tests | `tests/Scheduling/ViciOne.ServiceBus.QuartzIntegration.Tests/Courier_Specs.cs` |
| `TF.Sagas.ChoirConcurrency.RehearsalBegins` | 2 | 2 | DynamoDbIntegration, Tests | `tests/Persistence/ViciOne.ServiceBus.DynamoDbIntegration.Tests/Choir_Specs.cs` |
| `TF.Sagas.ChoirConcurrency.Tenor` | 2 | 2 | DynamoDbIntegration, Tests | `tests/Persistence/ViciOne.ServiceBus.DynamoDbIntegration.Tests/Choir_Specs.cs` |
| `TF.InMemoryContainerTestFixture` | 1 | 19 | Tests |  |
| `TF.TestConsumeContext` | 1 | 17 | Tests |  |
| `TF.Messages.MessageA` | 1 | 7 | Tests |  |
| `TF.Messages.MessageB` | 1 | 3 | Tests |  |
| `TF.Courier.SetVariableActivity` | 1 | 3 | Tests |  |
| `TF.Courier.SetVariableArguments` | 1 | 3 | Tests |  |
| `TF.Courier.SetVariablesFaultyActivity` | 1 | 3 | Tests |  |
| `TF.Courier.SetVariablesFaultyArguments` | 1 | 3 | Tests |  |
| `TF.Courier.FaultyCompensateActivity` | 1 | 2 | Tests | `tests/ViciOne.ServiceBus.Tests/Courier/Fault_Specs.cs` |
| `TF.Sagas.PublishTestStartedActivity` | 1 | 2 | Tests | `tests/ViciOne.ServiceBus.Tests/ContainerTests/Common_Tests/Common_SagaStateMachine.cs` |
| `TF.Courier.ReviseItineraryActivity` | 1 | 2 | Tests | `tests/ViciOne.ServiceBus.Tests/Courier/ItinerarySubscription_Specs.cs` |
| `TF.Sagas.TestInstance` | 1 | 2 | Tests | `tests/ViciOne.ServiceBus.Tests/ContainerTests/Common_Tests/Common_SagaStateMachine.cs` |
| `TF.Sagas.TestStateMachineSaga` | 1 | 2 | Tests | `tests/ViciOne.ServiceBus.Tests/ContainerTests/Common_Tests/Common_SagaStateMachine.cs` |
| `TF.ActivityTestContextConfigurator` | 1 | 1 | RabbitMqTransport | `tests/Transports/ViciOne.ServiceBus.RabbitMqTransport.Tests/RabbitMqActivityTestFixture.cs` |
| `TF.Courier.AddressActivity` | 1 | 1 | Tests | `tests/ViciOne.ServiceBus.Tests/Courier/UriArgument_Specs.cs` |
| `TF.Courier.AddressArguments` | 1 | 1 | Tests | `tests/ViciOne.ServiceBus.Tests/Courier/UriArgument_Specs.cs` |
| `TF.Courier.AddressLog` | 1 | 1 | Tests | `tests/ViciOne.ServiceBus.Tests/Courier/UriArgument_Specs.cs` |
| `TF.Futures.BatchCompleted` | 1 | 1 | Tests | `tests/ViciOne.ServiceBus.Tests/ContainerTests/Scenarios/WhenAllCompletedOrFaulted.cs` |
| `TF.Futures.BatchFaulted` | 1 | 1 | Tests | `tests/ViciOne.ServiceBus.Tests/ContainerTests/Scenarios/WhenAllCompletedOrFaulted.cs` |
| `TF.Futures.Tests.BatchFuture_Specs` | 1 | 1 | Tests | `tests/ViciOne.ServiceBus.Tests/ContainerTests/Scenarios/WhenAllCompletedOrFaulted.cs` |
| `TF.Futures.BatchRequest` | 1 | 1 | Tests | `tests/ViciOne.ServiceBus.Tests/ContainerTests/Scenarios/WhenAllCompletedOrFaulted.cs` |
| `TF.ForkJoint.Consumers.CookFryConsumer` | 1 | 1 | Azure.ServiceBus.Core | `tests/Transports/ViciOne.ServiceBus.Azure.ServiceBus.Core.Tests/Future_Specs.cs` |
| `TF.ForkJoint.Consumers.CookFryConsumerDefinition` | 1 | 1 | Azure.ServiceBus.Core | `tests/Transports/ViciOne.ServiceBus.Azure.ServiceBus.Core.Tests/Future_Specs.cs` |
| `TF.Courier.FirstFaultyCompensateActivity` | 1 | 1 | Tests | `tests/ViciOne.ServiceBus.Tests/Courier/RetryActivity_Specs.cs` |
| `TF.ForkJoint.Futures.FryFuture` | 1 | 1 | Azure.ServiceBus.Core | `tests/Transports/ViciOne.ServiceBus.Azure.ServiceBus.Core.Tests/Future_Specs.cs` |
| `TF.Messages.IMessageA` | 1 | 1 | Tests | `tests/ViciOne.ServiceBus.Tests/SendReceive_Specs.cs` |
| `TF.MediatorTestFixture` | 1 | 1 | Tests | `tests/ViciOne.ServiceBus.Tests/ResponsePatternMatching_Specs.cs` |
| `TF.Courier.NastyFaultyActivity` | 1 | 1 | Tests | `tests/ViciOne.ServiceBus.Tests/Courier/Fault_Specs.cs` |
| `TF.Courier.ObjectGraphActivityArguments` | 1 | 1 | Tests | `tests/ViciOne.ServiceBus.Tests/Courier/ObjectGraph_Specs.cs` |
| `TF.Courier.ObjectGraphTestActivity` | 1 | 1 | Tests | `tests/ViciOne.ServiceBus.Tests/Courier/ObjectGraph_Specs.cs` |
| `TF.ForkJoint.Contracts.OrderFry` | 1 | 1 | Azure.ServiceBus.Core | `tests/Transports/ViciOne.ServiceBus.Azure.ServiceBus.Core.Tests/Future_Specs.cs` |
| `TF.Courier.OuterObjectImpl` | 1 | 1 | Tests | `tests/ViciOne.ServiceBus.Tests/Courier/ObjectGraph_Specs.cs` |
| `TF.Messages.PingNotSupported` | 1 | 1 | Tests | `tests/ViciOne.ServiceBus.Tests/MessageContext_Specs.cs` |
| `TF.Courier.ReviseToEmptyItineraryActivity` | 1 | 1 | Tests | `tests/ViciOne.ServiceBus.Tests/Courier/ReviseItinerary_Specs.cs` |
| `TF.Courier.ReviseWithNoChangeItineraryActivity` | 1 | 1 | Tests | `tests/ViciOne.ServiceBus.Tests/Courier/ReviseItinerary_Specs.cs` |
| `TF.Courier.SetLargeVariableActivity` | 1 | 1 | Tests | `tests/ViciOne.ServiceBus.Tests/Courier/MessageDataArguments_Specs.cs` |
| `TF.Courier.SetLargeVariableArguments` | 1 | 1 | Tests | `tests/ViciOne.ServiceBus.Tests/Courier/MessageDataArguments_Specs.cs` |
| `TF.TestSymmetricKeyProvider` | 1 | 1 | Tests | `tests/ViciOne.ServiceBus.Tests/MessageData/DataBus_Specs.cs` |
| `TF.Sagas.UpdateTest` | 1 | 1 | Tests | `tests/ViciOne.ServiceBus.Tests/ContainerTests/Common_Tests/Common_SagaStateMachine.cs` |
### 4.2 Members referenced without naming their declaring type

Extension methods and statics do not show up in a type-name search. Owner counts are distinct
consuming test projects, derived with `git grep -lw <member> -- 'tests/**.cs'` and confirmed
against the declaring file.

| Member | Declaring file | Owners | Files | Consuming projects |
|---|---|---:|---:|---|
| `TestStateMachineExtensions.RaiseEvent` (4 overloads) | `TestStateMachineExtensions.cs` | 1 | 56 | Tests (SagaStateMachineTests/*, JobAttemptGeneration_Specs.cs) |
| `TestStateMachineExtensions.GetState` | `TestStateMachineExtensions.cs` | 1 | 8 | Tests (SagaStateMachineTests/*, Initializers/*) |
| `TestStateMachineExtensions.NextEvents` | `TestStateMachineExtensions.cs` | 1 | 5 | Tests (SagaStateMachineTests/*) |
| `TestStateMachineExtensions.TransitionToState` | `TestStateMachineExtensions.cs` | 1 | 2 | Tests (SagaStateMachineTests/{Automatonymous,Dynamic Modify}/Transition_Specs.cs) |
| `IntrospectionExtensions.ToJsonString` | `IntrospectionExtensions.cs` | 5 | 10 | ActiveMqTransport, AmazonSqsTransport, Azure.ServiceBus.Core, RabbitMqTransport, Tests |
| `IntrospectionExtensions.GetReceiveEndpointAddresses` | `IntrospectionExtensions.cs` | 1 | 1 | Tests (`Introspection_Specs.cs`) |
| `HealthCheckServiceExtensions.WaitForHealthStatus` | `HealthCheckServiceExtensions.cs` | 4 | 7 | ActiveMqTransport, EventHubIntegration, RabbitMqTransport, Tests |
| `BusTestFixture.ConfigureBusDiagnostics` (public static) | `BusTestFixture.cs` | 5 | 24 | ActiveMqTransport, AmazonSqsTransport, Azure.ServiceBus.Core, RabbitMqTransport, Tests |
| `BusTestFixture.LoggerFactory` (public static field) | `BusTestFixture.cs` | 6 | 32 | ActiveMqTransport, AmazonSqsTransport, Azure.ServiceBus.Core, RabbitMqTransport, EntityFrameworkCoreIntegration, Tests |
| `BusTestFixture.IsLogEnabled` (public static) | `BusTestFixture.cs` | 0 | 0 | none — unused public surface |
| `InMemoryActivityTestFixture.AddActivityContext` / `GetActivityContext` / `SetupActivities` | `InMemoryActivityTestFixture.cs` | 2 | 20–22 | QuartzIntegration, Tests (plus the RabbitMq copy, see F-19) |
| `FutureTestFixture` (via inheritance only) | `FutureTestFixture.cs` | 4 | 4 | Azure.Table, Azure.ServiceBus.Core, EntityFrameworkCoreIntegration, Tests |

### 4.3 What removal would break

Of the 11 files disposed `REMOVE`, **none has a confirmed consumer**:

- `ThreadSafeRandom.cs`, `ITestFixtureContainerFactory.cs`, `Messages/DeleteMessage.cs` —
  zero references anywhere in the repository.
- the four `Logging/` files — referenced only from inside the TestFramework
  (`BusTestFixture`, `MediatorTestFixture`, `InMemoryContainerTestFixture`,
  `TestConsumeContext`), all of which are themselves rebuilt.
- `ActivityTestContextConfigurator.cs` — one syntactic reference, and that implementation is
  never instantiated (F-15).
- the `.csproj`, the log4net XML and the lock file — they disappear with the project.

Removal of any of the 136 non-removed files would break a named consumer; those consumers are
listed per row in `LEDGER_DRAFT.jsonl` under `notes` (`consumers=…`).

## 5. What R0 did not measure

Stated so the Lead does not read more into this report than it carries.

- **No test was executed.** This cohort is read-only; no build, no restore, no run. Every
  claim about the sixteen cases is read out of the source and, where it concerns execution,
  cross-checked against the frozen anchor files — not against a run performed here.
- **No compilation check.** Statements like "never instantiated" or "never read" are the
  result of `git grep` plus reading the declaration and every hit, not of a compiler or
  analyzer pass. They are stated as such and each one names the file and line where it can be
  re-checked.
- **The defect claims are readings, not reproductions.** F-10 (null report),
  F-11 (unpersistable state), F-13 (patty never stored) and F-14 (transition from
  `Initial.Enter`) are derived from the code as written. None was reproduced with a failing
  test in this wave; each should be confirmed by a mutation or fault-injection probe when the
  corresponding capability is rebuilt.
- **Downstream duplicates were sampled, not censused.** F-11 names four projects with private
  duplicates of the saga types, confirmed by reading two of them
  (`Azure.Table.Tests/Saga/Container_Specs.cs:72`,
  `EntityFrameworkCoreIntegration.Tests/Container_Specs.cs:199`). A full duplicate census
  across `tests/**` is another cohort's scope.
