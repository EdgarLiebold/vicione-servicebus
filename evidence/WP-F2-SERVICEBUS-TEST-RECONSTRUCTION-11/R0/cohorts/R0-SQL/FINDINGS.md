# R0-SQL — findings

Cohort `R0-SQL`, baseline commit `ae73c6da748e3bc3257dffa4971ee8680e086207`. Every finding below was
read out of the 226 files in `READ_MANIFEST.tsv`; nothing was executed and no container was started.
Where a finding is an inference rather than a direct reading, it says so.

Sections: **P** provider differences (Lead plan section 5, "PostgreSQL und SQL Server getrennt und
real ausfuehrend"), **D** test and product defects for the rebuild (TLP-011, plan section 11), **C**
configuration and credentials (plan section 7), **S** security.

---

## P — differences between the PostgreSQL and the SQL Server implementation

These are what a rewrite loses first: none of them is visible in the C# transport, all of them live in
the two migrators' SQL, and only two of the seven have any inherited test at all.

### P-1 — `Pattern` subscriptions are POSIX regular expressions on PostgreSQL and `LIKE` patterns on SQL Server

`PostgresDatabaseMigrator.CreateInfrastructureSql`, `publish_message_v2`:

```sql
WHEN ts.sub_type = 3 THEN publish_message_v2.routing_key ~ ts.routing_key
```

`SqlServerDatabaseMigrator.SqlFnPublishV2`:

```sql
OR (ts.SubType = 3 AND @routingKey LIKE ts.RoutingKey)
```

`SqlSubscriptionType.Pattern` is one public enum value with two incompatible meanings. The inherited
case `When_routing_using_a_pattern` uses the patterns `^[A-Z]+$` and `^[0-9]+$`, which are regular
expressions; as `LIKE` patterns they are literals and would match nothing. That is why the case exists
only for PostgreSQL — and the absence of a SQL Server twin currently hides the difference rather than
documenting it. This is the single largest provider semantic in the cohort.

Same split, less visibly, for `topic_subscription.sub_type = 3` in the recursive topic fabric.

**For the rebuild:** the SQL Server owner needs its own pattern case using `LIKE` syntax, and the
shared documentation of `SqlSubscriptionType` must say that `Pattern` is engine-defined.

### P-2 — dead-letter maintenance throttles on PostgreSQL and never throttles on SQL Server

`SqlMessageReceiver.ReceiveMessages` (core, shared):

```csharp
count = await _client.DeadLetterQueue(_receiveSettings.QueueName, _receiveSettings.MaintenanceBatchSize);
if (count < _receiveSettings.MaintenanceBatchSize)
    _lastMaintenance = DateTime.UtcNow;
```

PostgreSQL's `dead_letter_messages` ends with `RETURN v_count`, and `PostgresClientContext` reads it
with `ExecuteScalarAsync<int?>` over `SELECT * FROM ... dead_letter_messages(...)`, so `count` is a
number and the 30-second throttle arms. SQL Server's `DeadLetterMessages` procedure has no `SELECT`
and no `RETURN`, so `SqlServerClientContext.DeadLetterQueue` yields `null`; `null < int` is `false` in
C#, `_lastMaintenance` stays `null`, and the dead-letter pass therefore runs on **every** idle poll of
**every** endpoint. Inferred from the code, not measured.

### P-3 — a delay under one second is lost on SQL Server

`PostgresClientContext.Send`/`Publish` pass `delay = context.Delay` (a `TimeSpan` bound to a PostgreSQL
`interval`). `SqlServerClientContext` passes `delay = (int?)context.Delay?.TotalSeconds`, and
`SendMessageV2` applies `DATEADD(SECOND, @delay, ...)`. Any delay below one second becomes zero.
`Using_delayed_send` and `Using_delayed_publish` both use three seconds, so neither engine's boundary
is touched.

### P-4 — the redelivery unlock delay is rounded up to a whole second on SQL Server

`SqlServerClientContext.Unlock`:

```csharp
delay = delay > TimeSpan.Zero ? Math.Max((int)delay.TotalSeconds, 1) : 0
```

PostgreSQL passes the `TimeSpan` through unchanged. A 200 ms `UnlockDelay`, or a sub-second
`UseDelayedRedelivery` interval, therefore behaves differently on the two engines. This is why the
inherited redelivery case (`Should_use_built_in_redelivery_to_redeliver_faulted_messages`, PostgreSQL
only) can use a one-second interval with a 150 ms tolerance and a SQL Server twin could not.

### P-5 — re-declaring a queue clears `auto_delete` on PostgreSQL and keeps it on SQL Server

`create_queue_v2`, `ON CONFLICT ... DO UPDATE`:

```sql
auto_delete = COALESCE(create_queue_v2.auto_delete, excluded.auto_delete)
```

`excluded.auto_delete` is the value being inserted, i.e. the parameter itself — so a `NULL` parameter
sets the column to `NULL`. `CreateQueueV2`, `WHEN MATCHED`:

```sql
AutoDelete = COALESCE(source.AutoDelete, target.AutoDelete)
```

`target` is the existing row, so a `NULL` parameter keeps the stored value. One bus declaring a queue
without an auto-delete window therefore disarms auto-deletion on PostgreSQL and leaves it armed on SQL
Server. Note that in the *same statement* `MaxDeliveryCount` collapses to `10` on both engines
(Q-2 in `RECONCILIATION.md`), which is a third behaviour again.

### P-6 — queue-name uniqueness is a constraint on PostgreSQL and only an index on SQL Server

PostgreSQL creates `unique_queue` as a `UNIQUE` constraint and uses it as the `ON CONFLICT` target, so
a duplicate `(type, name)` is impossible. SQL Server creates `IX_Queue_Name_Type` as a **non-unique**
index and relies on `MERGE ... WITH (ROWLOCK)`, which is not a uniqueness guarantee: two concurrent
`CreateQueueV2` calls can each miss the other's uncommitted insert. Every later lookup is
`SELECT @queueId = q.Id FROM Queue q WHERE ...`, which would silently pick one of the duplicates.

The two inherited provisioning cases already record the asymmetry in what they assert — PostgreSQL
asserts `unique_queue` with the reason "so a queue name could be created twice", SQL Server asserts
`ix_queue_name_type` with the reason "so every queue lookup would scan the table". The test difference
is honest; the product difference behind it is the finding.

### P-7 — `unlock_message` does not clear `lock_id` on PostgreSQL

`unlock_message` sets `enqueue_time`, `consumer_id = NULL` and `transport_headers`, but never
`lock_id`. `UnlockMessage` on SQL Server sets `LockId = NULL` as well. Two consequences, both read
directly out of the schema:

1. `requeue_messages` / `requeue_message` filter `mdx.lock_id IS NULL AND mdx.consumer_id IS NULL`, so
   on PostgreSQL a delivery that was unlocked after a fault can never be requeued.
2. The `queues` view classifies a row with `lock_id IS NOT NULL AND enqueue_time > now` as
   `message_locked`, so on PostgreSQL a message merely waiting out its redelivery delay is reported as
   locked, while on SQL Server the same message is reported as `message_scheduled`.

### Smaller provider differences, recorded for completeness

- **Maintenance contention.** PostgreSQL's `process_metrics` takes `LOCK TABLE ... IN EXCLUSIVE MODE`
  and blocks. SQL Server's `ProcessMetrics`, `PurgeTopology` and `RemoveOrphanedMessages` take
  `sp_getapplock` with a 500 ms timeout and `RETURN` silently when they cannot get it.
- **Observability window.** The `queues` view sums metrics over the last minute on PostgreSQL; the
  `Queues` view takes `MAX` over the newest row within the last five minutes on SQL Server, and adds a
  `CountStartTime` column PostgreSQL does not have.
- **Blocking during dead-lettering.** PostgreSQL's `dead_letter_messages` uses
  `FOR UPDATE SKIP LOCKED`; SQL Server's `DeadLetterMessages` uses `WITH (ROWLOCK, UPDLOCK)` **without**
  `READPAST`, so it blocks where PostgreSQL skips. The fetch procedures do use `READPAST` on both.
- **Topic delete cascade.** PostgreSQL uses two `ON DELETE CASCADE` foreign keys; SQL Server cannot
  (multiple cascade paths) and substitutes an `INSTEAD OF DELETE` trigger `DELETE_Topic`.
- **Body storage.** `jsonb` on PostgreSQL (through `JsonParameter` / `NpgsqlDbType.Jsonb`), which
  normalises key order and rejects invalid JSON, versus `nvarchar(max)` on SQL Server, which does
  neither. `Using_json_extension_data` runs on both but asserts nothing that would show the difference.
- **Serialization-failure code.** PostgreSQL error `40001` versus SQL Server error `1205`; both are
  converted to an empty fetch batch, but by different `catch` filters.

---

## D — defects to fix in the rebuild (TLP-011, plan section 11)

### D-1 — two cases mutate process environment variables (`RunnerContract_Specs.cs`)

`Should_name_every_missing_variable_and_no_secret_when_the_contract_is_incomplete` clears all four
`VICIONE_SERVICEBUS_PG_*` variables and `Should_refuse_a_port_the_runner_published_as_nonsense`
overwrites the port variable; both restore in a `finally`. Plan section 8 states that tests do not
change process culture, time zone or environment variables themselves. Under any parallel execution
this removes the runner contract from every other fixture in the process for the duration of the case.
The same two cases reach a private static through `BindingFlags.NonPublic` reflection.

**Rebuild:** give `TestRunnerContract` an injectable variable reader and assert against that.

### D-2 — three test paths drop a shared database

`MigrationHostedService<TDbContext>.StartAsync` calls `Database.EnsureDeletedAsync()` followed by
`EnsureCreatedAsync()`. On Npgsql and SqlClient `EnsureDeleted` drops the **database**, not the schema.

| registration | database dropped |
|---|---|
| `BusOutbox_Specs` -> `MigrationHostedService<ReliableDbContext>` | `ViciOneServiceBus_transport_tests` — the transport database every other SQL transport fixture uses |
| `JobConsumer_Specs` -> `MigrationHostedService<JobServiceSagaDbContext>` (PostgreSQL) | `ViciOneServiceBusUnitTests` — `TestDatabase.PostgresPersistence`, shared with the persistence cohort |
| `JobConsumer_Specs` -> same (SQL Server) | `ViciOneServiceBusUnitTests_v12_2015` — `TestDatabase.Persistence`, same |

The bus-outbox case happens to survive because `AddBusOutboxServices()` is registered before
`ConfigurePostgresTransport()`, so the transport migration re-provisions afterwards. Nothing protects
any *other* fixture, and the seven job-consumer methods each drop and recreate a persistence database.
This is exactly the "two concurrent runs corrupt each other" case TLP-011 asks about, and it also
crosses cohort boundaries into `R0-PER`.

`MigrationHostedService` additionally never disposes `_scope` or `_context`, and its `StopAsync` is
declared `async Task` with an empty body (CS1998).

### D-3 — `PartitionKey_Specs` shares a static counter across both provider fixtures

```csharp
class PartitionedConsumer : IConsumer<PartitionedTestMessage>
{
    static int _index = MessageLimit;          // initialised once, at type load
    ...
    if (Interlocked.Decrement(ref _index) <= 0) _taskCompletionSource.TrySetResult(context);
```

There is no `Reset()`. The `Postgres` and `SqlServer` fixtures are two closed generics over the same
open class, so they share the static. Whichever fixture runs second starts with `_index <= 0` and
completes its `TaskCompletionSource` on the first consumed message, so
`await provider.GetTask<...>()` returns long before the 30 messages have arrived and
`Assert.That(receivedMessages, Has.Count.EqualTo(MessageLimit))` becomes a race. Order-dependent and
shared-mutable-state, both forbidden.

`Expiration_Specs.LimitedConsumer`, `Purge_Specs.RecordingConsumer` and `RenewLock_Specs.SlowConsumer`
have the same shared-static shape but do call a `Reset()` at the start of the case, so they survive a
sequential run and would still break under parallelism.

### D-4 — SQL Server records the dead-letter metric only when nothing was dead lettered

`SqlServerDatabaseMigrator.SqlFnDeadLetterMessages`:

```sql
SELECT @vRowCount = @@ROWCOUNT;
IF @vRowCount = 0
BEGIN
    INSERT INTO {0}.QueueMetricCapture (..., DeadLetterCount) VALUES (..., @vRowCount);
END;
```

The condition is inverted relative to PostgreSQL (`IF v_count > 0 THEN INSERT ... v_count`), and the
value inserted inside the branch is necessarily zero. `Queues.DeadLetterCount` is therefore always
zero on SQL Server.

### D-5 — SQL Server never runs orphaned-message cleanup

`SqlServerDbConnectionContext.MaintenanceAgent.PerformMaintenance`:

```csharp
await _context.Query((x, t) => x.ExecuteScalarAsync<long?>(removeOrphanedMessagesSql,
    new { RowLimit = ... }, t), Stopping);
```

Every other maintenance call in that class goes through `Execute<T>`, which passes
`commandType: CommandType.StoredProcedure`. This one does not, so Dapper sends the string
`transport.RemoveOrphanedMessages` as command text. PostgreSQL formats a real `SELECT` and does run.

### D-6 — five SQL Server procedures report their result in a way Dapper cannot read

`SendMessageV2`, `PublishMessageV2`, `DeleteMessage`, `TouchQueue` and `DeadLetterMessages` end with
`RETURN <value>` or with nothing at all; `SqlServerClientContext` reads them with
`ExecuteScalarAsync<T?>`, which reads the first column of the first result set and never the procedure
return code. All five therefore yield `null`. The visible consequence today is that
`ClientContext.DeleteMessage` always evaluates to `false` on SQL Server —
`SqlReceiveLockContext.Complete()` ignores the result, which is the only reason this is currently
invisible. The PostgreSQL counterparts all return through a `SELECT`-able function.
(`CreateQueueV2`, `CreateTopic`, `FetchMessages`, `RenewMessageLock`, `UnlockMessage`, `MoveMessage`,
`PurgeQueue`, `DeleteScheduledMessage` and `RemoveOrphanedMessages` do end with a `SELECT` and are
fine.)

### D-7 — `SqlServerSqlHostSettings.TrimHost` returns the port as the host

```csharp
var hostSplit = host.Trim().Split(':');
return hostSplit.Length == 1 ? hostSplit[0] : hostSplit[1];
```

For `"localhost:1433"` this returns `"1433"` as the host name. The PostgreSQL twin
(`PostgresSqlHostSettings.ParseHost`) treats the same input as host plus port. Untested on both sides.

### D-8 — `SqlServer/PortAddress_Specs.Should_not_include_the_port` configures the wrong bus

```csharp
x.UsingPostgres();                                            // inside a SQL Server fixture
...
var connection = SqlServerSqlTransportConnection.GetDatabaseConnection(options);
```

The case passes only because `GetDatabaseConnection` is a static that reads the options directly; the
SQL Server bus factory is never exercised. A copy-paste defect, not a deliberate cross-check.

Related, in `Redelivery_Specs.cs`: the fixture carries a third, bare `[TestFixture]` on an open generic
class, which produces no identity at all (the anchor confirms only the two typed ones), and the case
builds a plain `new ServiceCollection()` instead of `_configuration.Create()` unlike every sibling — it
works only because `IDatabaseTestConfiguration.Configure` registers the options on the same collection.

### D-9 — fixed sleeps used as synchronisation

| location | sleep |
|---|---|
| `Scheduler_Specs.Should_be_supported` | `await Task.Delay(1000)` inside the handler |
| `Scheduler_Specs.Should_be_supported_from_outside_the_consumer` | `await Task.Delay(500)` |
| `JobConsumer_Specs.Should_cancel_the_job_and_retry_it` | `await Task.Delay(500)` |
| `PartitionKey_Specs` consumer | `await Task.Delay(4)` per message |
| `RenewLock_Specs` | `3 x await Task.Delay(LockDuration)` — documented as the situation under test, and the only defensible one of the five |

Both `Scheduler_Specs` cases additionally prove an absence with a five-second `OrTimeout` that must
elapse in full, twice per provider.

### D-10 — a wall clock is used to measure an elapsed duration

`DelayedDelivery_Specs` (both cases) computes `DateTime.UtcNow` before and after and asserts
`then - now >= 2 s` for a configured 3 s delay. A monotonic clock is required for a distance between
two moments, and the assertion is a full second weaker than the contract. `Fault_Specs` gets this
right with a `Stopwatch` and says why in a comment — that is the pattern to keep.

### D-11 — fixed entity names in a database that outlives the run

| fixture | name | shared with |
|---|---|---|
| `RenewLock_Specs` | `slow-consumer-queue` | both provider fixtures, and every run against the same engine |
| `PgSqlBus_Specs.Should_support_standard_syntax_with_consumers` | `input-queue` | `SubscriptionType_Specs` x2 |
| `SubscriptionType_Specs` (both) | `input-queue` | as above |
| `DelayedDelivery_Specs` | `delayed-input-queue`, `delayed-publish-input-queue` | both provider fixtures |
| `PartitionKey_Specs` | `partitioned-input-queue` | both provider fixtures |
| `Scheduler_Specs` | `schedule-input` | both cases, both providers |
| `Provision_Specs`, `SqlServer/Provision_Specs` | database `ViciOneServiceBus_provisioning_tests` | both, and it is **dropped** |

Ten other fixtures already use `$"...-{NewId.Next().ToString("N")}"` and say in a comment why. The
provisioning database is the worst case: two concurrent runs drop each other's database.

### D-12 — order-dependent cases

`Provision_Specs` and `SqlServer/Provision_Specs` each use `[Order(1)]` / `[Order(2)]`, and the
`Order(2)` case depends on the database the `Order(1)` case created.

### D-13 — assertion-free and unchecked paths

- `PgSql/Migration_Specs.Should_work_with_data_source` has no assertion at all: it starts and stops a
  harness. A bus that provisioned nothing would pass.
- `Request_Specs` sets `MaxDeliveryCount = 5` and `DeadLetterExpiredMessages = true` and asserts
  nothing about either, writes timings to `Console` with no assertion, and never stops the harness.
- `ExtensionData_Specs`, `Redelivery_Specs`, `Scheduler_Specs`, `PartitionKey_Specs`,
  `PgSql/MultiHost_Specs`, `PgSql/PortAddress_Specs` and the `SqlServer` address specs never call
  `harness.Stop()`; they rely on `await using var provider` and never check the stop result.
- `async` lambdas with no `await` (CS1998): the `Fault<MemberUpdateCommand>` handlers in `Fault_Specs`
  and `Redelivery_Specs`, and the three `ChannelName_Specs` methods, which are `async Task` around
  purely synchronous assertions.

### D-14 — a test helper writes to product tables

`TransportInspection.ExhaustDeliveryAttempts` issues an `UPDATE ... SET delivery_count =
max_delivery_count`. It is documented and it is the only way to reach the limit boundary without
waiting out as many lock expiries as the limit allows, so it is defensible — but the rebuild should
own it as a named arrange helper of the provider owner rather than as a method on an inspection class
whose contract says "read only apart from two".

### D-15 — two `TestConfigurationExtensions` classes with the same name

`ViciOne.ServiceBus.SqlTransport.Tests.TestConfigurationExtensions` (PostgreSQL) and
`ViciOne.ServiceBus.SqlTransport.Tests.SqlServer.TestConfigurationExtensions` (SQL Server, declared
inside `SqlServer/Provision_Specs.cs`). Which one an ambiguous call resolves to depends on the `using`
directives of the calling file. Give each provider owner its own distinctly named entry point.

---

## C — configuration and credentials (plan section 7)

### C-1 — hard-coded credentials in seven test files

Every direct credential read or literal, as required:

| file | literal |
|---|---|
| `TestConfigurationExtensions.cs` | `Username = "unit_tests"`, `Password = "H4rd2Gu3ss!"`, `Schema = "transport"`, `Role = "transport"` |
| `SqlServer/Provision_Specs.cs` (`ConfigureSqlServerTransport`) | the same four |
| `PgSql/Migration_Specs.cs` | the same four, plus `AdminUsername`/`AdminPassword` taken from the parsed connection string |
| `PgSql/MultiHost_Specs.cs` (4 cases) | `unit_tests` / `H4rd2Gu3ss!` **and** `AdminUsername = "sa"`, `AdminPassword = "Password12!"` |
| `PgSql/PortAddress_Specs.cs` (2 cases) | the same six |
| `SqlServer/InstanceName_Specs.cs` (3 cases) | the same six |
| `SqlServer/PortAddress_Specs.cs` (3 cases) | the same six |

`unit_tests` / `H4rd2Gu3ss!` is **not** synthetic: it is the transport login that
`PostgresDatabaseMigrator.GrantAccess` and `SqlServerDatabaseMigrator.CreateDatabaseIfNotExist`
actually create on the fixture, and that the bus then connects with. `sa` / `Password12!` never
connects — the four files that use it only build a connection string — but it is byte-identical to the
password in the test project's own `docker-compose.yml` (C-3), so it is a real fixture secret, not a
parser value.

The genuinely synthetic values, which plan section 7 explicitly permits to stay visible because their
value *is* the semantics, are the three connection strings in
`Migration_Specs.Should_parse_connection_string_into_options` (`password=2Legit2Quit`), the
`a-secret` / `S3cret-that-must-not-leak` strings in `RunnerContract_Specs`, and `Secret Value` in
`RoutingSlip_Specs` (a message payload, not a credential).

**Rebuild:** all of these belong in `SqlTestOptions` under the single configuration owner named in
plan section 7, with the run-scoped values supplied by the fixture.

### C-2 — how the tests obtain host, port, user and password today

There is exactly one correct path and it is already in place. `RunScopedTransportEndpoint` (test
project) delegates everything to `TestDatabase` and `TestRunnerContract`
(`tests/ViciOne.ServiceBus.TestInfrastructure`, outside this cohort's write scope). `TestRunnerContract`
is the **only** place in the chain that calls `Environment.GetEnvironmentVariable`, reading
`VICIONE_SERVICEBUS_PG_{HOST,PORT,USER,PASS}` and `VICIONE_SERVICEBUS_MSSQL_{HOST,PORT,USER,PASS}`.
It is fail-closed with no default host, port, account or secret, raises `TestRunnerContractException`
naming every missing variable before any connection is attempted, and `DatabaseEndpoint.ToString()`
deliberately omits the password.

That design already satisfies most of plan section 7. Two gaps remain:

1. the run-scoped endpoint covers only host, port and the **admin** account; the *transport* account
   (`unit_tests` / `H4rd2Gu3ss!`) and the schema and role names are still literals in seven files
   (C-1);
2. the two `RunnerContract_Specs` cases reach around the contract by writing process environment
   variables (D-1).

No test in this cohort reads a user secret, and none reads an environment variable directly other than
those two cases.

### C-3 — the test project carries a second, unpinned, credential-bearing infrastructure definition

`tests/Transports/ViciOne.ServiceBus.SqlTransport.Tests/docker-compose.yml`:

```yaml
mssql:   image: "mcr.microsoft.com/azure-sql-edge"      # no tag, no digest
         environment: SA_PASSWORD=Password12!
         ports: "1433:1433"
postgres: image: "postgres"                             # no tag, no digest
         environment: POSTGRES_PASSWORD=Password12!
         ports: "5432:5432"
```

Superseded in every respect by `build/test-infrastructure/compose.yaml`, which pins
`postgres:16@sha256:9520...74b0b` and
`mcr.microsoft.com/mssql/server:2025-CU8-ubuntu-24.04@sha256:4bab...12e1`, takes both passwords from
the run-scoped environment with `:?` guards, and publishes ephemeral ports. The file in the test
project is referenced by no runner, uses a different engine (Azure SQL Edge, not SQL Server), pins
nothing, and hard-codes fixed ports and a fixed password. Plan section 6 item 4 requires images to be
version- and digest-bound.

**Proposal:** remove it with the old test project; it carries no obligation. Confirmation is a Lead
call (Q-5).

### C-4 — the test project's database names are partly constants and partly literals

`TestDatabase` names the four databases centrally, and `RunScopedTransportEndpoint` re-exports two of
them. But `JobServiceSagaDbContextFactory` and `ReliableDbContextFactory` both hard-code the literal
`"ViciOneServiceBus_transport_tests"` instead of using `TestDatabase.SqlTransport`, and
`PostgresDatabaseTestConfiguration.Apply` hard-codes `"ViciOneServiceBusUnitTests"` instead of
`TestDatabase.PostgresPersistence`. The result is that the job-saga `DbContext` lands in the
*persistence* database while the bus-outbox `DbContext` lands in the *transport* database, for no
stated reason — and it is those literals that make D-2 hard to see.

---

## S — security

### S-1 — both migrators interpolate the account password into the SQL statement text

`PostgresDatabaseMigrator`:

```csharp
const string CreateUserSql = """CREATE USER "{1}" WITH PASSWORD '{2}'; GRANT "{0}" TO "{1}";""";
```

`SqlServerDatabaseMigrator`:

```csharp
const string CreateLoginSql = @"CREATE LOGIN {0} WITH PASSWORD = '{1}';";
```

Both are `string.Format`ed with `options.Password` and executed as command text. Two consequences:

1. **Injection / breakage.** A password containing a single quote terminates the literal. Plan
   section 7 requires locally generated, per-run, strong container credentials, which is exactly the
   population most likely to contain quotes unless the generator excludes them — an implicit and
   unstated constraint on the credential generator.
2. **Disclosure.** The password reaches the server-side statement text, and therefore
   `pg_stat_activity` / the PostgreSQL log on one side and the SQL Server query store, DMV plan cache
   and (with `log_statement` or an extended-events session) the error log on the other. Plan section 7
   requires that run credentials are not logged.

This is product code, not test code, so it is not fixed by rewriting tests. Recorded as an obligation
(`OBL-R0-SQL-0129`) and raised here.

### S-2 — `TrustServerCertificate = true` is forced, not chosen

`SqlServerSqlTransportConnection.CreateBuilder` sets `TrustServerCertificate = true` on the builder
before the caller's connection string is merged, and `SqlServerSqlHostSettings.GetConnectionString`
sets it again when it builds a connection string from parts. A caller cannot turn it off through
`SqlTransportOptions`. Appropriate for a local container fixture; it is a product default, and no test
records it either way.
