using System.Net.Mime;
using Dapper;
using Npgsql;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Providers.Transports;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.SqlTransport.Configuration;
using ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.SqlTransport.Topology;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.PostgreSql;

public sealed class PostgreSqlDatabaseBoundaryContractTests
{
    [Theory]
    [InlineData("UTC", 0)]
    [InlineData("UTC", 1)]
    [InlineData("UTC", 2)]
    [InlineData("UTC", 3)]
    [InlineData("America/New_York", 0)]
    [InlineData("America/New_York", 1)]
    [InlineData("America/New_York", 2)]
    [InlineData("America/New_York", 3)]
    [RequirementCoverage("REQ-VSB-POSTGRES-UTC-INSTANTS", "session-timezone-does-not-change-topology-send-or-lock-instants")]
    public async Task Timezone_PreservesRealInstantsForTopologySendFetchAndRenewAsync(string timezone, int stage)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync("timezone-boundary", token);
        await using NpgsqlConnection connection = fixture.CreateConnection();
        await connection.OpenAsync(token);
        string queue = fixture.Name("queue");
        await WithClientAsync(fixture, timezone, async (context, client) =>
        {
            string effective = Assert.IsType<string>(await context.QueryAsync((db, transaction) => db.ExecuteScalarAsync<object>(
                new CommandDefinition("SHOW TIME ZONE", transaction: transaction, cancellationToken: token)), token));
            Assert.Equal(timezone, effective);
            DateTime before;
            DateTime after;
            DateTime actual;
            TimeSpan interval = TimeSpan.Zero;
            if (stage == 0)
            {
                before = await ClockAsync(connection, token);
                long queueId = await client.CreateQueueAsync(new QueueDefinition(queue), token);
                after = await ClockAsync(connection, token);
                actual = await ScalarAsync<DateTime>(connection,
                    $"SELECT updated FROM \"{fixture.Schema}\".queue WHERE id=@Id", new { Id = queueId }, token);
            }
            else
            {
                await client.CreateQueueAsync(new QueueDefinition(queue), token);
                var message = Message(token);
                if (stage == 1)
                {
                    before = await ClockAsync(connection, token);
                    await client.SendAsync(queue, message, token);
                    after = await ClockAsync(connection, token);
                }
                else
                {
                    await client.SendAsync(queue, message, token);
                    // Establish an actually due delivery independently of the suspect Send clock.
                    await ExecuteAsync(connection,
                        $"UPDATE \"{fixture.Schema}\".message_delivery SET enqueue_time=clock_timestamp()-interval '1 minute' WHERE transport_message_id=@Id",
                        new { Id = message.TransportMessageId }, token);
                    interval = TimeSpan.FromSeconds(30);
                    before = await ClockAsync(connection, token);
                    SqlTransportMessage locked = Assert.Single(await client.ReceiveMessagesAsync(queue,
                        SqlReceiveMode.Normal, 1, 1, interval, token));
                    if (stage == 3)
                    {
                        before = await ClockAsync(connection, token);
                        Assert.True(await client.RenewLockAsync(locked.LockId!.Value, locked.MessageDeliveryId, interval, token));
                    }
                    after = await ClockAsync(connection, token);
                }
                actual = await ScalarAsync<DateTime>(connection,
                    $"SELECT enqueue_time FROM \"{fixture.Schema}\".message_delivery WHERE transport_message_id=@Id",
                    new { Id = message.TransportMessageId }, token);
            }
            Assert.Equal(DateTimeKind.Utc, actual.Kind);
            Assert.InRange(actual, before + interval, after + interval);
        }, token);
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(false, 3)]
    [InlineData(false, 4)]
    [InlineData(true, 0)]
    [InlineData(true, 1)]
    [InlineData(true, 2)]
    [InlineData(true, 3)]
    [InlineData(true, 4)]
    [RequirementCoverage("REQ-VSB-POSTGRES-BIGINT-TOPOLOGY", "create-and-touch-complete-for-bigint-topology-identities")]
    public async Task BigintTopology_CreateAndTouchReturnOrPersistTheRealIdentityAsync(bool high, int operation)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync("bigint-topology", token);
        await using NpgsqlConnection connection = fixture.CreateConnection();
        await connection.OpenAsync(token);
        long start = high ? (long)int.MaxValue + 31 : 31;
        string queue = fixture.Name("existing-queue");
        string source = fixture.Name("source");
        string destination = fixture.Name("destination");
        await WithClientAsync(fixture, "UTC", async (_, client) =>
        {
            long existing = await client.CreateQueueAsync(new QueueDefinition(queue), token);
            await client.CreateTopicAsync(new TopicDefinition(source), token);
            await client.CreateTopicAsync(new TopicDefinition(destination), token);
            await ExecuteAsync(connection, $"ALTER SEQUENCE \"{fixture.Schema}\".topology_seq RESTART WITH {start}", null, token);
            long? returned = null;
            string table;
            string name;
            Exception? failure;
            switch (operation)
            {
                case 0:
                    table = "queue"; name = fixture.Name("created-queue");
                    failure = await Record.ExceptionAsync(async () =>
                    {
                        returned = await client.CreateQueueAsync(new QueueDefinition(name), token);
                    });
                    break;
                case 1:
                    table = "topic"; name = fixture.Name("created-topic");
                    failure = await Record.ExceptionAsync(async () =>
                    {
                        returned = await client.CreateTopicAsync(new TopicDefinition(name), token);
                    });
                    break;
                case 2:
                    table = "queue_subscription"; name = "";
                    failure = await Record.ExceptionAsync(async () =>
                    {
                        returned = await client.CreateQueueSubscriptionAsync(new QueueSubscriptionDefinition(
                            new TopicDefinition(source), new QueueDefinition(queue), SqlSubscriptionType.All, ""), token);
                    });
                    break;
                case 3:
                    table = "topic_subscription"; name = "";
                    failure = await Record.ExceptionAsync(async () =>
                    {
                        returned = await client.CreateTopicSubscriptionAsync(new TopicSubscriptionDefinition(
                            new TopicDefinition(source), new TopicDefinition(destination), SqlSubscriptionType.All, ""), token);
                    });
                    break;
                default:
                    table = "queue"; name = queue;
                    await ExecuteAsync(connection, $"UPDATE \"{fixture.Schema}\".queue SET id=@Start WHERE id=@Existing",
                        new { Start = start, Existing = existing }, token);
                    failure = await Record.ExceptionAsync(() => client.TouchQueueAsync(queue, token));
                    break;
            }
            // Capture actual committed state before failure assertions, also exposing rollback/commit discrepancies.
            long[] persisted = (await connection.QueryAsync<long>(new CommandDefinition(
                $"SELECT id FROM \"{fixture.Schema}\".{table}" + (table is "queue" or "topic" ? " WHERE name=@Name" : ""),
                new { Name = name }, cancellationToken: token))).ToArray();
            Assert.Null(failure);
            long actual = operation == 0 || operation == 4
                ? Assert.Single(persisted, id => id == start) : Assert.Single(persisted);
            Assert.Equal(start, actual);
            if (operation == 4)
            {
                // TouchQueue returns Task, not Task<long>: inspect its persisted metric effect instead.
                Assert.Equal(start, await ScalarAsync<long>(connection,
                    $"SELECT queue_id FROM \"{fixture.Schema}\".queue_metric_capture", null, token));
            }
            else
                Assert.Equal(actual, returned);
        }, token);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [RequirementCoverage("REQ-VSB-POSTGRES-QUEUE-METRICS", "backlog-does-not-multiply-consume-error-and-deadletter-counts")]
    public async Task QueueMetrics_RemainExactWhenPendingDeliveryCountChangesAsync(int pending)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync("metric-multiplicity", token);
        await using NpgsqlConnection connection = fixture.CreateConnection();
        await connection.OpenAsync(token);
        await ExecuteAsync(connection, "SET TIME ZONE 'UTC'", null, token);
        string queue = fixture.Name("queue");
        await WithClientAsync(fixture, "UTC", async (_, client) =>
        {
            long queueId = await client.CreateQueueAsync(new QueueDefinition(queue), token);
            for (int disposition = 0; disposition < 3; disposition++)
            {
                var message = Message(token);
                await client.SendAsync(queue, message, token);
                SqlTransportMessage locked = Assert.Single(await client.ReceiveMessagesAsync(queue,
                    SqlReceiveMode.Normal, 1, 1, TimeSpan.FromSeconds(30), token));
                Assert.Equal(message.TransportMessageId, locked.TransportMessageId);
                if (disposition == 0)
                    Assert.True(await client.DeleteMessageAsync(locked.LockId!.Value, locked.MessageDeliveryId, token));
                else
                    Assert.True(await client.MoveMessageAsync(locked.LockId!.Value, locked.MessageDeliveryId, queue,
                        disposition == 1 ? SqlQueueType.ErrorQueue : SqlQueueType.DeadLetterQueue,
                        null, new DictionarySendHeaders(), token));
            }
            await ExecuteAsync(connection, $"SELECT \"{fixture.Schema}\".process_metrics(1000)", null, token);
            MetricCounts stored = await connection.QuerySingleAsync<MetricCounts>(new CommandDefinition(
                $"SELECT sum(consume_count)::bigint AS ConsumeCount, sum(error_count)::bigint AS ErrorCount, sum(dead_letter_count)::bigint AS DeadLetterCount FROM \"{fixture.Schema}\".queue_metric WHERE queue_id=@QueueId",
                new { QueueId = queueId }, cancellationToken: token));
            Assert.Equal(1L, stored.ConsumeCount);
            Assert.Equal(1L, stored.ErrorCount);
            Assert.Equal(1L, stored.DeadLetterCount);
            for (int index = 0; index < pending; index++)
                await client.SendAsync(queue, Message(token), token);

            // The view exposes the latest eligible minute, not the sum of every settled minute.
            // Both reads share transaction now(), including eligibility at a minute boundary.
            await using var metricSnapshot = await connection.BeginTransactionAsync(token);
            MetricCounts expectedMinute = await connection.QuerySingleAsync<MetricCounts>(new CommandDefinition(
                $"SELECT consume_count AS ConsumeCount, error_count AS ErrorCount, dead_letter_count AS DeadLetterCount FROM \"{fixture.Schema}\".queue_metric WHERE queue_id=@QueueId AND start_time >= (now() at time zone 'utc')-interval '1 minutes' ORDER BY start_time DESC LIMIT 1",
                new { QueueId = queueId }, transaction: metricSnapshot, cancellationToken: token));
            Assert.True(expectedMinute.ConsumeCount > 0 || expectedMinute.ErrorCount > 0 || expectedMinute.DeadLetterCount > 0,
                "The latest eligible metric bucket must contain an actual settlement; expired fixture data is not a product defect oracle.");
            QueueCounts view = await connection.QuerySingleAsync<QueueCounts>(new CommandDefinition(
                $"SELECT ready AS Ready, errored AS Errored, dead_lettered AS DeadLettered, consume_count AS ConsumeCount, error_count AS ErrorCount, dead_letter_count AS DeadLetterCount FROM \"{fixture.Schema}\".queues WHERE queue_name=@Name",
                new { Name = queue }, transaction: metricSnapshot, cancellationToken: token));
            Assert.Equal((long)pending, view.Ready);
            Assert.Equal(1L, view.Errored);
            Assert.Equal(1L, view.DeadLettered);
            Assert.Equal(expectedMinute.ConsumeCount, view.ConsumeCount);
            Assert.Equal(expectedMinute.ErrorCount, view.ErrorCount);
            Assert.Equal(expectedMinute.DeadLetterCount, view.DeadLetterCount);
            await metricSnapshot.CommitAsync(token);
        }, token);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-POSTGRES-BIGINT-TOPOLOGY", "legacy-upgrade-preserves-data-owner-and-execute-grant-chain")]
    public async Task LegacyIntegerFunctions_UpgradeAndReapplyPreserveDataAndExecutePrivilegesAsync()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        await WithUpgradeFixtureAsync("legacy-bigint-upgrade", async (fixture, connection, roles) =>
        {
            string queue = fixture.Name("queue");
            string source = fixture.Name("source");
            string destination = fixture.Name("destination");
            long[] identities = new long[5];
            await WithClientAsync(fixture, "UTC", async (_, client) =>
            {
                identities[0] = await client.CreateQueueAsync(new QueueDefinition(queue), token);
                identities[1] = await client.CreateTopicAsync(new TopicDefinition(source), token);
                identities[2] = await client.CreateTopicAsync(new TopicDefinition(destination), token);
                identities[3] = await client.CreateQueueSubscriptionAsync(new QueueSubscriptionDefinition(
                    new TopicDefinition(source), new QueueDefinition(queue), SqlSubscriptionType.All, ""), token);
                identities[4] = await client.CreateTopicSubscriptionAsync(new TopicSubscriptionDefinition(
                    new TopicDefinition(source), new TopicDefinition(destination), SqlSubscriptionType.All, ""), token);
            }, token);
            string[] stored = await TopologyRowsAsync(connection, fixture.Schema, token);
            long sequence = await ScalarAsync<long>(connection, $"SELECT last_value FROM \"{fixture.Schema}\".topology_seq", null, token);
            await MakeLegacyTopologyFunctionsAsync(connection, fixture.Schema, token);
            Assert.Equal(Enumerable.Repeat("integer", 4), await TopologyReturnTypesAsync(connection, fixture.Schema, token));

            string allowed = fixture.Name("allowed");
            string delegated = fixture.Name("delegated");
            string denied = fixture.Name("denied");
            foreach (string role in new[] { allowed, delegated, denied })
            {
                await ExecuteAsync(connection, $"CREATE ROLE \"{role}\"", null, token);
                roles.Add(role);
                // All roles have identical table/sequence rights; only EXECUTE differs.
                await ExecuteAsync(connection,
                    $"GRANT USAGE ON SCHEMA \"{fixture.Schema}\" TO \"{role}\"; GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA \"{fixture.Schema}\" TO \"{role}\"; GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA \"{fixture.Schema}\" TO \"{role}\"", null, token);
            }
            foreach (string signature in TopologySignatures(fixture.Schema))
                await ExecuteAsync(connection, $"REVOKE EXECUTE ON FUNCTION {signature} FROM PUBLIC; GRANT EXECUTE ON FUNCTION {signature} TO \"{allowed}\" WITH GRANT OPTION", null, token);
            await ExecuteAsync(connection, $"SET ROLE \"{allowed}\"", null, token);
            try
            {
                foreach (string signature in TopologySignatures(fixture.Schema))
                    await ExecuteAsync(connection, $"GRANT EXECUTE ON FUNCTION {signature} TO \"{delegated}\"", null, token);
            }
            finally
            {
                await ExecuteAsync(connection, "RESET ROLE", null, CancellationToken.None);
            }
            string[] originalAcl = await TopologyAclAsync(connection, fixture.Schema, token);
            string[] originalOwners = await TopologyOwnersAsync(connection, fixture.Schema, token);
            var migrator = new PostgreSqlDatabaseMigrator(Microsoft.Extensions.Logging.Abstractions.NullLogger<PostgreSqlDatabaseMigrator>.Instance);
            for (int pass = 0; pass < 2; pass++)
            {
                await migrator.CreateInfrastructureAsync(fixture.Options, token);
                Assert.Equal(Enumerable.Repeat("bigint", 4), await TopologyReturnTypesAsync(connection, fixture.Schema, token));
                Assert.Equal(stored, await TopologyRowsAsync(connection, fixture.Schema, token));
                Assert.Equal(sequence, await ScalarAsync<long>(connection, $"SELECT last_value FROM \"{fixture.Schema}\".topology_seq", null, token));
                Assert.Equal(originalOwners, await TopologyOwnersAsync(connection, fixture.Schema, token));
                Assert.Equal(originalAcl, await TopologyAclAsync(connection, fixture.Schema, token));
            }

            foreach (string role in new[] { allowed, delegated })
            {
                await ExecuteAsync(connection, $"SET ROLE \"{role}\"", null, token);
                try
                {
                    Assert.Equal(identities[0], await ScalarAsync<long>(connection,
                        $"SELECT \"{fixture.Schema}\".create_queue(@Queue, NULL, 10)", new { Queue = queue }, token));
                    Assert.Equal(identities[1], await ScalarAsync<long>(connection,
                        $"SELECT \"{fixture.Schema}\".create_topic(@Topic)", new { Topic = source }, token));
                    Assert.Equal(identities[3], await ScalarAsync<long>(connection,
                        $"SELECT \"{fixture.Schema}\".create_queue_subscription(@Source,@Destination,1,'','{{}}'::jsonb)",
                        new { Source = source, Destination = queue }, token));
                    Assert.Equal(identities[4], await ScalarAsync<long>(connection,
                        $"SELECT \"{fixture.Schema}\".create_topic_subscription(@Source,@Destination,1,'','{{}}'::jsonb)",
                        new { Source = source, Destination = destination }, token));
                }
                finally
                {
                    await ExecuteAsync(connection, "RESET ROLE", null, CancellationToken.None);
                }
            }
            foreach (string signature in TopologySignatures(fixture.Schema))
                Assert.False(await ScalarAsync<bool>(connection,
                    "SELECT has_function_privilege(@Role,@Signature,'EXECUTE')", new { Role = denied, Signature = signature }, token));
            await ExecuteAsync(connection, $"SET ROLE \"{denied}\"", null, token);
            try
            {
                PostgresException rejection = await Assert.ThrowsAsync<PostgresException>(() => connection.ExecuteScalarAsync<long>(
                    new CommandDefinition($"SELECT \"{fixture.Schema}\".create_topic(@Topic)", new { Topic = source }, cancellationToken: token)));
                Assert.Equal("42501", rejection.SqlState);
                Assert.Contains("create_topic", rejection.MessageText, StringComparison.Ordinal);
            }
            finally
            {
                await ExecuteAsync(connection, "RESET ROLE", null, CancellationToken.None);
            }
            long high = (long)int.MaxValue + 51;
            await ExecuteAsync(connection, $"ALTER SEQUENCE \"{fixture.Schema}\".topology_seq RESTART WITH {high}", null, token);
            await WithClientAsync(fixture, "UTC", async (_, client) =>
            {
                string created = fixture.Name("bigint-after-upgrade");
                long actual = await client.CreateTopicAsync(new TopicDefinition(created), token);
                Assert.Equal(high, actual);
                Assert.Equal(actual, await ScalarAsync<long>(connection,
                    $"SELECT id FROM \"{fixture.Schema}\".topic WHERE name=@Name", new { Name = created }, token));
            }, token);
        }, token);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-POSTGRES-BIGINT-TOPOLOGY", "legacy-dependency-aborts-upgrade-atomically-without-cascade")]
    public async Task LegacyFunctionDependency_FailsWithoutPartialUpgradeOrDataLossAsync()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        await WithUpgradeFixtureAsync("legacy-dependency", async (fixture, connection, _) =>
        {
            string queue = fixture.Name("queue");
            long identity = await ScalarAsync<long>(connection, $"SELECT \"{fixture.Schema}\".create_queue(@Queue,NULL,10)", new { Queue = queue }, token);
            await MakeLegacyTopologyFunctionsAsync(connection, fixture.Schema, token);
            string[] originalTypes = await TopologyReturnTypesAsync(connection, fixture.Schema, token);
            string[] originalAcl = await TopologyAclAsync(connection, fixture.Schema, token);
            await ExecuteAsync(connection,
                $"CREATE VIEW \"{fixture.Schema}\".legacy_dependency AS SELECT \"{fixture.Schema}\".create_queue('{queue}',NULL,10) AS id", null, token);
            var migrator = new PostgreSqlDatabaseMigrator(Microsoft.Extensions.Logging.Abstractions.NullLogger<PostgreSqlDatabaseMigrator>.Instance);
            PostgresException rejection = await Assert.ThrowsAsync<PostgresException>(() => migrator.CreateInfrastructureAsync(fixture.Options, token));
            Assert.Equal("2BP01", rejection.SqlState);
            Assert.Equal(originalTypes, await TopologyReturnTypesAsync(connection, fixture.Schema, token));
            Assert.Equal(originalAcl, await TopologyAclAsync(connection, fixture.Schema, token));
            Assert.Equal(identity, await ScalarAsync<long>(connection, $"SELECT id FROM \"{fixture.Schema}\".queue WHERE name=@Name AND type=1", new { Name = queue }, token));
            // The preceding upgrade drops all three nonqueue routines before queue cleanup hits this dependency.
            // The retained view and original types/ACL prove those earlier drops were rolled back.
            Assert.Equal(checked((int)identity), await ScalarAsync<int>(connection, $"SELECT id FROM \"{fixture.Schema}\".legacy_dependency", null, token));
        }, token);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [RequirementCoverage("REQ-VSB-POSTGRES-UTC-INSTANTS", "legacy-table-defaults-survive-reapply-and-preserve-nonutc-instant")]
    public async Task LegacyTimestampDefaults_UseActualInstantsAfterRepeatedMigrationAsync(int target)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        await WithUpgradeFixtureAsync("legacy-default", async (fixture, connection, _) =>
        {
            string[] tables = ["queue", "topic", "topic_subscription", "queue_subscription", "message"];
            foreach (string table in tables)
                await ExecuteAsync(connection, $"ALTER TABLE \"{fixture.Schema}\".{table} ALTER COLUMN {(table == "message" ? "sent_time" : "updated")} SET DEFAULT (now() at time zone 'utc')", null, token);
            var migrator = new PostgreSqlDatabaseMigrator(Microsoft.Extensions.Logging.Abstractions.NullLogger<PostgreSqlDatabaseMigrator>.Instance);
            await migrator.CreateInfrastructureAsync(fixture.Options, token);
            await migrator.CreateInfrastructureAsync(fixture.Options, token);
            string source = fixture.Name("source");
            string destination = fixture.Name("destination");
            string queue = fixture.Name("queue");
            long sourceId = await ScalarAsync<long>(connection, $"SELECT \"{fixture.Schema}\".create_topic(@Name)", new { Name = source }, token);
            long destinationId = await ScalarAsync<long>(connection, $"SELECT \"{fixture.Schema}\".create_topic(@Name)", new { Name = destination }, token);
            long queueId = await ScalarAsync<long>(connection, $"SELECT \"{fixture.Schema}\".create_queue(@Name,NULL,10)", new { Name = queue }, token);
            await ExecuteAsync(connection, "SET TIME ZONE 'America/New_York'", null, token);
            string sql = target switch
            {
                0 => $"INSERT INTO \"{fixture.Schema}\".queue(name,type) VALUES (@Name,1) RETURNING updated",
                1 => $"INSERT INTO \"{fixture.Schema}\".topic(name) VALUES (@Name) RETURNING updated",
                2 => $"INSERT INTO \"{fixture.Schema}\".topic_subscription(source_id,destination_id,sub_type,routing_key,filter) VALUES (@Source,@Destination,1,'legacy-default','{{}}'::jsonb) RETURNING updated",
                3 => $"INSERT INTO \"{fixture.Schema}\".queue_subscription(source_id,destination_id,sub_type,routing_key,filter) VALUES (@Source,@Queue,1,'legacy-default','{{}}'::jsonb) RETURNING updated",
                _ => $"INSERT INTO \"{fixture.Schema}\".message(transport_message_id) VALUES (@MessageId) RETURNING sent_time"
            };
            DateTime before = await ClockAsync(connection, token);
            DateTime actual = await ScalarAsync<DateTime>(connection, sql,
                new { Name = fixture.Name("default"), Source = sourceId, Destination = destinationId, Queue = queueId, MessageId = Guid.NewGuid() }, token);
            DateTime after = await ClockAsync(connection, token);
            Assert.Equal(DateTimeKind.Utc, actual.Kind);
            Assert.InRange(actual, before, after);
        }, token);
    }

    private static string[] TopologySignatures(string schema) =>
    [
        $"\"{schema}\".create_queue(text,integer,integer)",
        $"\"{schema}\".create_topic(text)",
        $"\"{schema}\".create_queue_subscription(text,text,integer,text,jsonb)",
        $"\"{schema}\".create_topic_subscription(text,text,integer,text,jsonb)"
    ];

    private static async Task MakeLegacyTopologyFunctionsAsync(NpgsqlConnection connection, string schema, CancellationToken token)
    {
        foreach (string signature in TopologySignatures(schema))
        {
            string definition = await ScalarAsync<string>(connection, "SELECT pg_get_functiondef(to_regprocedure(@Signature))", new { Signature = signature }, token);
            Assert.True(definition.Contains("RETURNS bigint", StringComparison.Ordinal) || definition.Contains("RETURNS integer", StringComparison.Ordinal));
            string legacy = definition.Replace("RETURNS bigint", "RETURNS integer", StringComparison.Ordinal);
            await ExecuteAsync(connection, $"DROP FUNCTION {signature} RESTRICT", null, token);
            await ExecuteAsync(connection, legacy, null, token);
        }
    }

    private static async Task<string[]> TopologyReturnTypesAsync(NpgsqlConnection connection, string schema, CancellationToken token) =>
        (await connection.QueryAsync<string>(new CommandDefinition(
            "SELECT format_type(p.prorettype,NULL) FROM pg_proc p WHERE p.oid=ANY(SELECT to_regprocedure(signature)::oid FROM unnest(@Signatures::text[]) AS signatures(signature)) ORDER BY p.proname",
            new { Signatures = TopologySignatures(schema) }, cancellationToken: token))).ToArray();

    private static async Task<string[]> TopologyOwnersAsync(NpgsqlConnection connection, string schema, CancellationToken token) =>
        (await connection.QueryAsync<string>(new CommandDefinition(
            "SELECT p.proname||':'||pg_get_userbyid(p.proowner) FROM pg_proc p WHERE p.oid=ANY(SELECT to_regprocedure(signature)::oid FROM unnest(@Signatures::text[]) AS signatures(signature)) ORDER BY p.proname",
            new { Signatures = TopologySignatures(schema) }, cancellationToken: token))).ToArray();

    private static async Task<string[]> TopologyAclAsync(NpgsqlConnection connection, string schema, CancellationToken token) =>
        (await connection.QueryAsync<string>(new CommandDefinition(
            "SELECT p.proname||':'||pg_get_userbyid(acl.grantor)||':'||CASE WHEN acl.grantee=0 THEN 'PUBLIC' ELSE pg_get_userbyid(acl.grantee)::text END||':'||acl.privilege_type||':'||acl.is_grantable::text FROM pg_proc p CROSS JOIN LATERAL aclexplode(COALESCE(p.proacl,acldefault('f',p.proowner))) acl WHERE p.oid=ANY(SELECT to_regprocedure(signature)::oid FROM unnest(@Signatures::text[]) AS signatures(signature)) ORDER BY p.proname,acl.grantor,acl.grantee,acl.is_grantable",
            new { Signatures = TopologySignatures(schema) }, cancellationToken: token))).ToArray();

    private static async Task<string[]> TopologyRowsAsync(NpgsqlConnection connection, string schema, CancellationToken token) =>
        (await connection.QueryAsync<string>(new CommandDefinition(
            $"SELECT 'queue:'||id||':'||name||':'||type FROM \"{schema}\".queue UNION ALL SELECT 'topic:'||id||':'||name FROM \"{schema}\".topic UNION ALL SELECT 'queue_subscription:'||id||':'||source_id||':'||destination_id FROM \"{schema}\".queue_subscription UNION ALL SELECT 'topic_subscription:'||id||':'||source_id||':'||destination_id FROM \"{schema}\".topic_subscription ORDER BY 1",
            cancellationToken: token))).ToArray();

    private static async Task WithUpgradeFixtureAsync(string purpose,
        Func<PostgreSqlTestDatabase, NpgsqlConnection, List<string>, Task> action, CancellationToken token)
    {
        PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync(purpose, token);
        var roles = new List<string>();
        var failures = new List<Exception>();
        try
        {
            await using NpgsqlConnection connection = fixture.CreateConnection();
            await connection.OpenAsync(token);
            await action(fixture, connection, roles);
        }
        catch (Exception exception)
        {
            failures.Add(exception);
        }
        finally
        {
            try
            {
                // Drop this fixture database first, removing only these roles' fixture ACL dependencies.
                await fixture.DisposeAsync();
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
            if (roles.Count > 0)
            {
                using var cleanup = new CancellationTokenSource(fixture.OperationTimeout);
                try
                {
                    await using var server = new NpgsqlConnection(fixture.ServerConnectionString);
                    await server.OpenAsync(cleanup.Token);
                    foreach (string role in roles.AsEnumerable().Reverse())
                    {
                        try
                        {
                            await ExecuteAsync(server, $"DROP ROLE \"{role}\"", null, cleanup.Token);
                        }
                        catch (Exception exception)
                        {
                            failures.Add(exception);
                        }
                    }
                }
                catch (Exception exception)
                {
                    failures.Add(exception);
                }
            }
        }
        if (failures.Count > 1)
            throw new AggregateException("SQL upgrade test or owned fixture cleanup failed.", failures);
        if (failures.Count == 1)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failures[0]).Throw();
    }
    private static async Task WithClientAsync(PostgreSqlTestDatabase fixture, string timezone,
        Func<ConnectionContext, ClientContext, Task> action, CancellationToken token)
    {
        var builder = new NpgsqlConnectionStringBuilder(fixture.ConnectionString) { Timezone = timezone };
        await using var dataSource = NpgsqlDataSource.Create(builder.ConnectionString);
        var bus = new SqlBusConfiguration(new SqlTopologyConfiguration(SqlBusFactory.CreateMessageTopology()));
        var host = Assert.IsType<SqlHostConfiguration>(bus.HostConfiguration);
        host.Settings = new PostgreSqlHostSettings(dataSource) { Schema = fixture.Schema, MaintenanceEnabled = false };
        IConnectionContextSupervisor supervisor = host.ConnectionContextSupervisor;
        try
        {
            await supervisor.SendAsync(Pipe.ExecuteAwaited<ConnectionContext>(context =>
                action(context, context.CreateClientContext(token))), token);
        }
        finally
        {
            // Notification listener is owned by the real supervisor; stop before disposing the provided datasource.
            await supervisor.StopAsync("PostgreSQL boundary test cleanup", CancellationToken.None);
        }
    }

    private static SqlMessageSendContext<BoundaryMessage> Message(CancellationToken token) =>
        new(new BoundaryMessage(Guid.NewGuid()), token) { Serializer = new BinarySerializer(), Priority = 0 };

    private static Task<DateTime> ClockAsync(NpgsqlConnection connection, CancellationToken token) =>
        ScalarAsync<DateTime>(connection, "SELECT clock_timestamp()", null, token);

    private static async Task<T> ScalarAsync<T>(NpgsqlConnection connection, string sql, object? parameters, CancellationToken token) =>
        Assert.IsType<T>(await connection.ExecuteScalarAsync<object>(new CommandDefinition(sql, parameters, cancellationToken: token)));

    private static Task<int> ExecuteAsync(NpgsqlConnection connection, string sql, object? parameters, CancellationToken token) =>
        connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: token));

    private sealed record QueueDefinition(string QueueName) : Queue
    {
        public TimeSpan? AutoDeleteOnIdle => null;
        public int? MaxDeliveryCount => 10;
    }
    private sealed record TopicDefinition(string TopicName) : Topic;
    private sealed record QueueSubscriptionDefinition(Topic Source, Queue Destination,
        SqlSubscriptionType SubscriptionType, string RoutingKey) : TopicToQueueSubscription;
    private sealed record TopicSubscriptionDefinition(Topic Source, Topic Destination,
        SqlSubscriptionType SubscriptionType, string RoutingKey) : TopicToTopicSubscription;
    private sealed class MetricCounts
    {
        public long ConsumeCount { get; set; }
        public long ErrorCount { get; set; }
        public long DeadLetterCount { get; set; }
    }
    private sealed class QueueCounts
    {
        public long Ready { get; set; }
        public long Errored { get; set; }
        public long DeadLettered { get; set; }
        public long ConsumeCount { get; set; }
        public long ErrorCount { get; set; }
        public long DeadLetterCount { get; set; }
    }
    public sealed record BoundaryMessage(Guid Id);
    private sealed class BinarySerializer : IMessageSerializer
    {
        public ContentType ContentType { get; } = new("application/octet-stream");
        public MessageBody GetMessageBody<T>(SendContext<T> context) where T : class => new BinaryMessageBody(new byte[] { 7, 19, 233 });
    }
}
