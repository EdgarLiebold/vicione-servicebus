# R0-PER — Findings

Cohort `R0-PER`, baseline commit `ae73c6da748e3bc3257dffa4971ee8680e086207`.
Every finding names a file and a line at that commit. Nothing here is a disposition; §9 of the Lead plan
reserves that. Nothing here is a guess unless it says so.

## 1. Configuration and credential inventory (plan §7)

The target architecture has exactly one typed configuration owner and no per-test credential access.
Measured state of this cohort:

### 1.1 EF Core — already centralised, and fail-closed

`tests/Persistence/ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Tests` reads **no** environment
variable, **no** connection string and **no** secret directly. All four `ITestDbParameters`
implementations and every `IDesignTimeDbContextFactory` route through
`tests/ViciOne.ServiceBus.TestInfrastructure/TestDatabase.cs`, which routes through
`TestRunnerContract.cs`. That contract reads eight variables —
`VICIONE_SERVICEBUS_MSSQL_{HOST,PORT,USER,PASS}` and `VICIONE_SERVICEBUS_PG_{HOST,PORT,USER,PASS}` —
in one place, has no default, no probe and no fallback, and raises `TestRunnerContractException`
naming the missing variables before the first connection is attempted. The exception carries no secret.

This is close to what §7 asks for. Two deltas remain: the owner lives in `tests/ViciOne.ServiceBus.TestInfrastructure`
rather than under `tests2/Testing/…/Configuration`, and the variable namespace is
`VICIONE_SERVICEBUS_…` rather than the `VICIONE_TESTS__…` namespace §7 prescribes. Both are renames,
not redesigns.

### 1.2 Direct credential reads — the actual findings

| # | location | finding |
|---|---|---|
| C-01 | `tests/Persistence/ViciOne.ServiceBus.Azure.Table.Tests/Configuration.cs:5` | `public static string StorageAccount => "UseDevelopmentStorage=true"`. A hard-coded shortcut that expands inside the Azure SDK to the well-known Azurite account name and key on `127.0.0.1:10002`. No environment variable, no per-run value, no way to point the suite at anything else. Read by `AzureTableInMemoryTestFixture.cs:20`, `Future_Specs.cs:34` and `JobConsumer_Specs.cs:65`. |
| C-02 | `tests/Persistence/ViciOne.ServiceBus.DynamoDbIntegration.Tests/SagaPersistenceTests.cs:110`, `Container_Specs.cs:109`, `Choir_Specs.cs:124` | `new AmazonDynamoDBConfig { ServiceURL = "http://localhost:4566" }` — the LocalStack endpoint hard-coded three times, once per file. No credentials are supplied at all, so the run depends on the ambient AWS SDK credential chain (`AWS_ACCESS_KEY_ID` etc. from the developer's shell). Fail-open: a machine with real AWS credentials and no LocalStack on 4566 will attempt to reach AWS. |
| C-03 | `tests/Persistence/ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Tests/docker-compose.yml:6,12` | `SA_PASSWORD=Password12!` and `POSTGRES_PASSWORD=Password12!` with fixed host ports `1433:1433` and `5432:5432`. This file is a legacy artefact: the pinned fixture is `build/test-infrastructure/compose.yaml`, and `PostgresTestDbParameters.cs:25-27` documents in a comment that the literal which stood there "measured whatever held that port rather than the pinned fixture". The compose file was not removed with it. |
| C-04 | `tests/Persistence/ViciOne.ServiceBus.Azure.Table.Tests/docker-compose.yml:5-8` | Azurite with fixed host ports `10000/10001/10002` and no digest pin, contradicting §6 nr. 4 (images version- and digest-bound) and the `EPHEMERAL_LOOPBACK_PORT` policy in `tools/ci/policies/__init__.py`. |
| C-05 | `tests/Persistence/ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Tests/ReliableMessaging/BusOutbox_Specs.cs:641` | A commented-out connection string carrying a password: `builder.UseNpgsql("host=localhost;user id=postgres;password=Password12!;database=ViciOneServiceBusUnitTests;", …)`. Dead code with a credential literal in it. |
| C-06 | `ReliableDbContextFactory.cs:34`, `Outbox_Specs.cs:426` (`ResponsibleDbContextFactory`), `OutboxTransactionFault_Specs.cs:34` | `builder.EnableSensitiveDataLogging()` in three places. EF Core then writes parameter values — including anything the saga state carries — into the log. §7 requires evidence to contain no secret values. |
| C-07 | `tests/Scheduling/ViciOne.ServiceBus.QuartzIntegration.Tests/Utils.cs:13` | `if (Debugger.IsAttached) return TimeSpan.FromMinutes(10);` — the test timeout depends on how the process was started. Not a credential, but the same class of hidden environment coupling. |

**No `TestContext`-based configuration and no SAS token exists anywhere in this cohort.** `TestContext`
appears only for output (`TestContext.Out.WriteLine`, 8 occurrences) and once for a cancellation token
(`BusOutboxDeliveryContext_Specs.cs:86`). `client.p12` is not in this cohort's scope.

Consequence for the rebuild: **two** direct credential-access sites have to disappear (C-01, C-02),
**two** stale compose files have to go (C-03, C-04), and three `EnableSensitiveDataLogging` calls have
to be removed or scoped. The EF Core path needs a rename, not a redesign.

## 2. Quality defects for the rebuild (TLP-011)

### 2.1 Fixed sleeps used as synchronisation — 14 occurrences

| file:line | value | what it stands in for |
|---|---|---|
| `EFCore/AuditStore/AuditStore_Specs.cs:24,32` | 2000 ms | waiting for the audit writer |
| `EFCore/AuditStore/AuditStore_Specs.cs:91` | 100 ms | poll interval of a hand-written retry loop |
| `EFCore/SlowConcurrentSaga/SlowConcurrentSagaStateMachine.cs:26` | 5000 ms | **inside the saga**, defines the deadlock window; exceeds the fixture's own 3 s `TestInactivityTimeout` |
| `EFCore/ReliableMessaging/InboxLock_Specs.cs:32,35` | 500 / 50 ms | places the duplicate deliveries inside and outside the inbox lock window — the mechanism of the case |
| `EFCore/ReliableMessaging/BusOutbox_Specs.cs:398` | 2 s | measurement window for the anti-spin regression |
| `AzureTable/SlowConcurrentSaga/SlowConcurrentSagaStateMachine.cs:27` | 3000 ms | same role as the EF twin |
| `Quartz/MissingInstanceRedelivery_Specs.cs:19` | 500 ms | window in which the saga instance does not yet exist |
| `Quartz/ScheduleTimeout_Specs.cs:32` | 3000 ms | **stands in for the negative assertion** — nothing is asserted after it |
| `Quartz/TwoMessage_Specs.cs:24` | 1000 ms | quiescence before advancing the clock |
| `Quartz/SchedulerLoadInMemory_Specs.cs:29` | 1000 ms | quiescence before reading the repository count |
| `Quartz/Service_Specs.cs:51` | 1000 ms | places the cancel inside the schedule window |
| `Quartz/PastEvent_Specs.cs:183` | 2000 ms | separates the two schedules of the reschedule case |

Five of these are not incidental: they *are* the mechanism of their case (`InboxLock`, both
`SlowConcurrentSaga` state machines, `MissingInstanceRedelivery`, `Service_Specs`). Replacing them with
an observable barrier is a semantic change and must be done deliberately, not by deletion.

`Quartz/Recurring_Specs.cs` and the two abandoned-job fixtures in `Quartz/Turnout/Faulted_Specs.cs`
already demonstrate the correct pattern — a counting observer that yields awaitable barriers plus a
scheduled horizon marker — and should be generalised rather than reinvented.

### 2.2 Unbounded waits

- `EFCore/ReliableMessaging/OutboxScopedFilter_Specs.cs:67` — `await taskCompletionSource.Task` with no
  token; a broken scope makes the case hang instead of failing.
- `EFCore/ReliableMessaging/BusOutbox_Specs.cs:213` — `await source.Task` for the telemetry baggage.
- Every assertion-free Turnout case (`await _completed;` and friends, 40 occurrences) hangs rather than
  failing when its event never arrives.

### 2.3 `async void` — none

Zero occurrences across all four test projects. Two related defects instead:
`EFCore/ReliableMessaging/MigrationHostedService.cs:38` is an `async Task StopAsync` with no `await`
and an empty body (so nothing is cleaned up), and
`EFCore/AuditStore/AuditStore_Specs.cs:36-39` is an `async Task` `[SetUp]` with an empty body.

### 2.4 Swallowed exceptions and unobserved failures

- `EFCore/ReliableMessaging/Outbox_Specs.cs:50-53` — `harness.Bus.ConnectHandler<Fault<Start>>(async context => { Interlocked.Increment(ref count); })`:
  an `async` lambda with no `await`, and the returned connect handle is never disposed.
- Product side, reported because a test must decide whether they are intended:
  `EntityFrameworkSagaRepositoryContextFactory.WithinTransaction` rollback (lines 180-190),
  `BusOutboxDeliveryService.RollbackTransaction` (lines 348-358),
  `EntityFrameworkOutboxContextFactory` rollback (lines 117-124),
  `AzureTableSagaRepositoryContext.Insert` (lines 52-57, swallows **every** exception including
  cancellation), `AmazonS3MessageDataRepository.PreStart` (lines 76-79 and 103-106),
  `AzureStorageMessageDataRepository.PreStart` (lines 76-85). All six catch and continue.

### 2.5 Tautological, misnamed or assertion-free cases

**Tautologies.** `EFCore/TransactionConfiguration/RepositoryFactory_TransactionConfiguration_Specs.cs`
— all six cases assert only `Assert.That(repository, Is.Not.Null)` on a factory method whose last
statement is `new SagaRepository<TSaga>(…)`. The assertion cannot fail. Each case name claims something
about transaction defaults that is never checked. Parameterised over three providers, so **18 of the
160 EF Core identities (11 %) are unfalsifiable**.

`EFCore/TransactionConfiguration/Configurator_TransactionConfiguration_Specs.cs` — all four cases assert
only `Assert.DoesNotThrow` over a plain field assignment, and the names say `configure_transaction`
while the code calls `SetOptimisticConcurrency`.

`EFCore/TransactionConfiguration/LockStrategy_…_Specs.cs` — two cases named `…_Should_Have_Transaction_Enabled_By_Default`
pass the value explicitly, so the default is not tested. Provider parameterisation turns 6 cases into 18
identities that behave identically; the parameterisation adds no coverage here.

**Names that do not match the code.**
`Quartz/JobDetail_Specs.Should_return_the_properties_with_custom_factory` is byte-identical to its
neighbour and installs no custom factory (Q-PER-07).
`EFCore/ReliableMessaging/OutboxTransactionFault_Specs.Should_throw_typed_exception` never asserts an
exception type, and its `TestMessage.ThrowInConsumer` flag is never set to `true` anywhere — dead.
`Quartz/Turnout/Complete_Specs.Submitting_a_bunch_of_jobs` is the only fixture with
`SetConcurrentJobLimit(3)` and never observes concurrency.
`Quartz/Turnout/Complete_Specs.Submitting_a_job_to_turnout_with_status_checks` never observes a status check.
`AzureTable/Saga/Container_Specs.Using_optimistic_concurrency` never provokes an ETag conflict.
`DynamoDb/Container_Specs.Using_optimistic_concurrency` is assertion-free.

**Conditional assertions that vanish.** `Quartz/ScheduleMessage_Specs.cs:26-27` and `76-77`:
`if (_secondActivityId != null && _firstActivityId != null) Assert.That(…)`. Without an ambient
`ActivityListener` both ids are null and the case asserts nothing. Two of the 89 Quartz identities are
assertion-free in the normal run while appearing to test trace propagation.

**Assertion-free success paths.** Measured by brace-matching every `[Test]` body in the four projects
and testing it for the token `Assert.`: **53 of the 182 declared case bodies contain no assertion at
all** (EF Core 16, Quartz 36, DynamoDb 1, Azure.Table 0). Expanded through the fixture parameterisation
that is **68 of the 249 anchored identities (27 %)** — 46 of those 68 are the `[Order]`-sequenced
Turnout cases that only `await` a fixture task. The remainder are
`EFCore/Container_Specs` (both concurrency cases, 6 identities),
`EFCore/ReliableMessaging/Outbox_Specs.Should_start_successfully` and `…_with_middleware`,
all four `Quartz/Courier_Specs` cases, three of the four `Quartz/PastEvent_Specs` cases,
`Quartz/OutboxScheduler_Specs.Should_not_cancel_the_message`, `Quartz/QuartzPublish_Specs`,
`Quartz/RequestRequest_Specs.Should_complete_the_request`,
`Quartz/ScheduleTimeout_Specs.Should_receive_the_timeout`,
`Quartz/Service_Specs.Using_the_quartz_service_with_json`, `Quartz/TwoMessage_Specs`,
and — unanchored — `DynamoDb/Container_Specs.Using_optimistic_concurrency`.

Per §9 none of this is a deletion ground. It does mean the anchor's 249 identities overstate the
inherited assurance: measured, 68 of them (27 %) assert nothing at all and a further 18 EF identities
assert something unfalsifiable — 86 of 249, a third of the inherited evidence, carries no falsifiable
claim.

### 2.6 Execution-order dependence

`[Order(n)]` appears **58 times**, in **14 fixture classes across 6 files**, all of them Turnout (4
classes in the EF cohort, 10 in the Quartz cohort). The pattern is always the same:
`Should_get_the_job_accepted` drives the entire
scenario and completes three fixture-level `Task` fields; the other three or five cases only `await`
those fields. Consequences:

- If the first case fails, the rest **hang** until the harness timeout instead of reporting.
- The cases cannot be run individually, filtered, or parallelised.
- `#pragma warning disable NUnit1032` is used in **4** files to hide the resulting undisposed-task
  warning: `EFCore/TransactionalBusOutbox_Specs.cs` and the three EF Turnout files. The Quartz Turnout
  files hold the same undisposed task fields **without** the pragma — they rely on the repository-wide
  `.editorconfig` suppression instead.

`.editorconfig:18` carries `dotnet_diagnostic.NUnit1032.severity = none`, the repository-wide
suppression §6 nr. 10 removes. Once it is gone, every one of these fixtures reports the warning.

### 2.7 Shared mutable state

| location | state |
|---|---|
| `Quartz/JobDetail_Specs.cs:72-76` | `static ManualResetEvent Signaled` and `static string SignaledBody`, **never reset**. The second case passes on the first case's signal; either case can pass without its own job running. |
| `Quartz/PastEvent_Specs.cs:177` | `ScheduleTokenId.UseTokenId<A>(x => x.Id)` in a fixture constructor mutates a **process-global** registration for a type named `A` that also exists in the neighbouring fixture of the same file. |
| `src/…/QuartzIntegration/QuartzTimeAdjustment.cs:18-19` | writes the process-global `SystemTime.UtcNow` / `SystemTime.Now`. Used by 12 Quartz cases and by `EFCore/ReliableMessaging/QuartzOutbox_Specs`. |
| `Quartz/FrozenSchedulerClockTestFixture.cs:119-120` | same global clock, held frozen. Correctly restored in `Dispose`, but two fixtures must never overlap. |
| `EFCore/ReliableMessaging/OutboxScopedFilter_Specs.cs:195` | `static readonly TaskCompletionSource<SimplerConsumer> _consumerCreated`, never reset between the two cases. |
| EF Core databases | `SqlServerTestDbParameters` and `SqlServerResiliencyTestDbParameters` both resolve to `TestDatabase.SqlServer()` = `ViciOneServiceBusUnitTests_v12_2015`. Two parameterised fixture variants therefore `EnsureDeleted` **the same physical database**. Every EF fixture that uses both variants is a database-level race with itself. |
| `AzureTable/FixtureSetUp.cs:3-4` | the whole Azure.Table assembly is `[Parallelizable(ParallelScope.None)]` with `LevelOfParallelism(1)` — an assembly-wide serialisation that is the current answer to all of the above. |

### 2.8 Cleanup that does not run or is not checked

- `EFCore/ReliableMessaging/MigrationHostedService.StopAsync` — empty; the `ReliableDbContext` and
  `ResponsibleDbContext` databases are created per run and never dropped.
- `AzureTable/Future_Specs.cs:60-63` — `OneTimeTearDown` returns `Task.CompletedTask`; the
  `futurestate` table survives the run.
- `DynamoDb` — all three fixtures create tables in `[SetUp]` and never delete them; `[TearDown] ClearTable()`
  removes rows only. `SagaPersistenceTests.cs:64` and the other two call the `[TearDown]` method
  **directly from `[SetUp]`**, so a teardown doubles as a setup helper.
- `EFCore/ReliableMessaging/BusOutbox_Specs.Should_support_delayed_message_scheduler` (lines 505-530) —
  no `try`/`finally`; `harness.Stop()` is skipped on the failure path.
- `Quartz/JobDetail_Specs` — two `StdSchedulerFactory` default schedulers are started and never shut down.
- No cleanup result is checked anywhere; §6 nr. 4 requires cleanup failures to be red.

### 2.9 Wall-clock assertions

`EFCore/ReliableMessaging/Outbox_Specs.cs:139-146` (`DateTime.UtcNow` arithmetic, `>= 2.9 s`),
`EFCore/ReliableMessaging/BusOutbox_Specs.cs:398-404` (2 s window, magic threshold `< 30`),
`EFCore/AuditStore/AuditStore_Specs.cs:76-79` (`DateTime.Now` deadline loop),
`Quartz/DelayRetry_Specs` (6 cases, `Stopwatch` lower bounds only),
`Quartz/ScheduledRedelivery_Specs.cs:24-26` (`DateTime.Now` timestamps, lower bounds only),
`Quartz/ScheduleMessage_Specs.cs:135` (`DateTime.UtcNow` compared against a clock the scheduler has adjusted).

All lower-bound-only: an interval ten times too long passes. §8 requires `TimeProvider` and observable
state instead.

## 3. EF Core specifics required by the task (model, provider matrix, concurrency, transactions)

### 3.1 Provider matrix as it actually stands

Three `ITestDbParameters` implementations, differing only in the connection and the lock statement
provider: SQL Server, SQL Server **with `EnableRetryOnFailure()`**, PostgreSQL. There is **no
in-memory provider** anywhere in the suite; `BusOutboxDeliveryContext_Specs` uses a `DbContextOptions`
with no provider at all, which works only because it never opens a connection.

Coverage per capability, measured:

| capability | SQL Server | SQL Server + retry | PostgreSQL |
|---|---|---|---|
| saga locate / insert / update | yes | yes | yes |
| custom include (query customization) | yes | yes | yes |
| optimistic & pessimistic container registration | yes | yes | yes, but the pessimistic variant compensates with `UseInMemoryOutbox` (Q-PER-01) |
| audit store | yes | yes | yes |
| ReadOnly saga events | yes | **no** | yes |
| Turnout / job service | yes | **no** | yes |
| Bus outbox, inbox lock, reliable messaging | yes (hard-coded `UseSqlServer`) | — | **no** |
| Futures | yes (hard-coded `UseSqlServer` + `SqlServerLockStatementProvider`) | — | **no** |
| `TransactionalBusOutbox` (System.Transactions) | yes | — | **no** |
| MySQL / Oracle / SQLite lock statements | **no** | — | — |

So the whole ReliableMessaging surface — outbox, inbox, delivery service, scoped filters — is
**SQL-Server-only**, even though `UsePostgres()` ships and `PostgresLockStatementFormatter` generates a
different outbox statement (`FOR UPDATE SKIP LOCKED` versus `WITH (UPDLOCK, ROWLOCK, READPAST)`).
`BusOutbox_Specs.cs:424` even contains a commented-out `o.UsePostgres();`. This is the largest single
provider gap in the cohort and is captured as gap obligations `OBL-R0-PER-0100..0102` (the untested
formatters) and noted on every ReliableMessaging row.

### 3.2 Migration and model semantics

Every fixture uses `EnsureDeletedAsync` + `EnsureCreatedAsync`; **no test ever runs a migration**,
although every `ITestDbParameters` configures `MigrationsAssembly(…)` and a per-context
`MigrationsHistoryTable($"__{dbContextType.Name}")`, and the project references
`Microsoft.EntityFrameworkCore.Design`. The migration configuration is therefore inert — it is
configured, never exercised, and no migration files are tracked in the project.

The model shape itself — the keys, unique indexes, optional foreign keys and 256-character address
columns defined in `EntityFrameworkOutboxConfigurationExtensions.ConfigureInboxStateEntity` /
`ConfigureOutboxStateEntity` / `ConfigureOutboxMessageEntity`, and the `OptOutOfEntityFrameworkConventions`
call that clears every `MaxLength` — has **no assertion anywhere**. Nothing would notice if a unique
index or a foreign key were dropped. This is `OBL-R0-PER-0113`, and it is the single most valuable
hermetic gap in the cohort: model shape is exactly what a rewrite loses silently. `JobSagaMap`,
`JobTypeSagaMap`, `JobAttemptSagaMap` (`OBL-R0-PER-0117`) and the two `Optimistic*DbContext` variants
(`OBL-R0-PER-0118`, referenced by **no test at all**) are in the same class.

### 3.3 Optimistic concurrency

`RowVersion` is declared `IsRowVersion()` on both `InboxState` and `OutboxState`, and
`OptimisticSagaRepositoryLockStrategy` runs at `IsolationLevel.ReadCommitted`. What is proven today:
that a saga configured as optimistic can be created and updated. What is **not** proven anywhere:

- that a `DbUpdateConcurrencyException` is ever raised, caught, or rolled back;
- that `RowVersion` changes;
- that two concurrent creators of the same `CorrelationId` produce one instance;
- that the optimistic path differs observably from the pessimistic path at all.

`Container_Specs.Using_optimistic_concurrency` and `…pessimistic_concurrency` have **identical case
bodies** and identical (empty) assertions; only the configuration differs. A mutation that swapped the
two modes would not turn either red. Captured as `OBL-R0-PER-0115`.

### 3.4 Transactions and isolation levels

Product defaults, read from the source:

| path | isolation level | transaction |
|---|---|---|
| `EntityFrameworkSagaRepositoryConfigurator` ctor | `Serializable` | pessimistic |
| …after `ConcurrencyMode = Optimistic` | downgraded to `ReadCommitted` **only if still Serializable** | — |
| …after `SetOptimisticConcurrency(bool)` | **not** downgraded | flag honoured only in optimistic mode |
| `EntityFrameworkSagaRepository.CreateOptimistic` | `ReadCommitted` | flag honoured |
| `EntityFrameworkSagaRepository.CreatePessimistic` | `Serializable` | always on, flag ignored |
| `EntityFrameworkOutboxConfigurator` ctor | `RepeatableRead` | — |
| `EntityFrameworkOutboxOptions<T>` default | `RepeatableRead` | — |

**No test observes a single one of these values.** The `LockStrategy_…` cases read back the constructor
argument they just passed; `WithinTransaction` is never inspected. Three consequences worth the Lead's
attention:

1. The order dependence between `ConcurrencyMode` and `IsolationLevel` (`OBL-R0-PER-0105`, Q-PER-05).
2. `CreatePessimisticLockStrategy` silently ignores `_isTransactionEnabled` (`OBL-R0-PER-0106`).
3. `Optimistic_saga_with_transactions_disabled` and `…_enabled` assert the same observable outcome, so
   nothing distinguishes the two configurations — a mutation probe on `isTransactionEnabled` would find
   no red case (`OBL-R0-PER-0104`).

`TransactionalBusOutbox_Specs` is the only place where `System.Transactions` / `TransactionScope`
semantics are exercised, SQL Server only, and its own comment (lines 113-119) documents a previously
repaired order dependence — the repair should survive the rebuild.

### 3.5 Background services

`InboxCleanupService<TDbContext>` has **no test at all**: every case that could reach it calls
`DisableInboxCleanupService()`. `BusOutboxDeliveryService` is covered only through
`BusOutbox_Specs`; its `IsTransientFailure` classifier matches on the English substrings
`"concurrent update"` and `"transient failure"` in `InnerException.Message` (lines 204-209) — locale-
and version-dependent string matching that decides whether an exception is swallowed or logged as a
fault, with no coverage (`OBL-R0-PER-0109`).

## 4. Product findings worth a Lead decision

| id | finding |
|---|---|
| P-01 | `SqlLockStatementProvider.TableNames` is a `protected static` cache keyed by entity type alone, shared process-wide across DbContexts and models. The XML docs of `UseSqlServer(bool enableSchemaCaching)` say "Set to false when using multiple DbContexts", implying the default (`true`) is unsafe. → Q-PER-04, `OBL-R0-PER-0103`. |
| P-02 | `CreatePessimisticLockStrategy` never reads `_isTransactionEnabled`. → Q-PER-05, `OBL-R0-PER-0106`. |
| P-03 | `AzureTableSagaRepositoryContext.Insert` swallows every exception and returns `default`, making a hard failure indistinguishable from a duplicate-key conflict. → Q-PER-08, `OBL-R0-PER-0453`. |
| P-04 | `CosmosTableSagaRepositoryContext<TSaga>` ships and is referenced by nothing. → `OBL-R0-PER-0455`, External. |
| P-05 | `AzureStorageMessageDataRepository.Get` decompresses when `blobName.EndsWith(".gz") \|\| _compress`, so a `compress:true` repository decompresses blobs it did not write. → `OBL-R0-PER-0651`. |
| P-06 | `DynamoDbDatabaseContext.Update` increments `instance.Version` **after** building the condition expression, so a failed update leaves the in-memory instance with an incremented version. → `OBL-R0-PER-0520`. |
| P-07 | `QuartzTimeAdjustment` — test infrastructure inside the shipped product package, mutating a process-global clock. → Q-PER-06, `OBL-R0-PER-0307`. |
| P-08 | `ResumeScheduledMessageConsumer` ships and is exercised by nothing: `Recurring_Specs` proves a schedule can be paused but never that it can be resumed. → `OBL-R0-PER-0305`. |
| P-09 | `MySqlLockStatementFormatter`, `OracleLockStatementFormatter` and `SqliteLockStatementFormatter` ship with public `UseMySql`/`UseOracle`/`UseSqlite` configuration and have zero coverage; the Oracle formatter carries a behavioural decision (no `FOR UPDATE`, because of ORA-00907) recorded only in a code comment. → `OBL-R0-PER-0100..0102`. |
| P-10 | Two obligations are not provable in **any** profile, not even against the real cloud, because both depend on an asynchronous service sweeper with a delay of up to 48 hours: DynamoDB TTL deletion (`OBL-R0-PER-0523`) and S3 lifecycle expiration (`OBL-R0-PER-0603`). They are candidates for a documented limitation plus a configuration-correctness case, not for a wave-C4b run. |

## 5. Framework contract at the baseline (context for §6)

All four test projects reference `Microsoft.NET.Test.Sdk`, `NUnit`, `NUnit.Analyzers` and
`NUnit3TestAdapter` directly — exactly the four direct references §6 nr. 1 forbids. None references any
xUnit package. `docker-compose.yml` files exist in two of the four projects with unpinned images and
fixed host ports (C-03, C-04). `AuditStore_Filter_Specs .cs` in the Azure.Table project has a space
before its extension. The `ViciOne.ServiceBus.DynamoDbIntegration.Tests.csproj` sets
`RootNamespace = ViciOne.ServiceBus.DynamoDb.Tests`, which is why its identities would not sort with the
project name.
