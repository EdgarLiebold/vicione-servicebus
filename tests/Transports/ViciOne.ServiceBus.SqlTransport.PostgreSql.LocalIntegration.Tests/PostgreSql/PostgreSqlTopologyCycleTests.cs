using Npgsql;
using ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.PostgreSql;

public sealed class PostgreSqlTopologyCycleTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-POSTGRES-TOPIC-CYCLE", "cyclic-topic-graph-terminates-and-delivers-once")]
    public async Task PublishAcrossACyclicTopicGraph_TerminatesAndDeliversExactlyOnceAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync(
            "cyclic-topology",
            cancellationToken);
        string firstTopic = fixture.Name("cycle-first");
        string secondTopic = fixture.Name("cycle-second");
        string queueName = fixture.Name("cycle-input");
        await using NpgsqlConnection connection = fixture.CreateConnection();
        await connection.OpenWithinAsync(fixture.OperationTimeout, cancellationToken);

        await ExecuteAsync(connection, fixture.Schema, "create_topic", firstTopic, cancellationToken);
        await ExecuteAsync(connection, fixture.Schema, "create_topic", secondTopic, cancellationToken);
        await ExecuteAsync(connection, fixture.Schema, "create_queue", queueName, cancellationToken);
        await CreateTopicSubscriptionAsync(connection, fixture.Schema, firstTopic, secondTopic, cancellationToken);
        await CreateTopicSubscriptionAsync(connection, fixture.Schema, secondTopic, firstTopic, cancellationToken);
        await CreateQueueSubscriptionAsync(connection, fixture.Schema, secondTopic, queueName, cancellationToken);

        await using var publish = new NpgsqlCommand(
            $"SELECT \"{fixture.Schema}\".publish_message(entity_name => @topic, priority => 100, sent_time => clock_timestamp())",
            connection);
        publish.Parameters.AddWithValue("topic", firstTopic);
        long deliveries = Convert.ToInt64(
            await publish.ExecuteScalarAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken));

        Assert.Equal(1, deliveries);
        Assert.Equal(1, await connection.DeliveryCountAsync(fixture.Schema, queueName, 1, cancellationToken));
    }

    private static async Task ExecuteAsync(
        NpgsqlConnection connection,
        string schema,
        string routine,
        string name,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand($"SELECT \"{schema}\".{routine}(@name)", connection);
        command.Parameters.AddWithValue("name", name);
        await command.ExecuteScalarAsync(cancellationToken);
    }

    private static async Task CreateTopicSubscriptionAsync(
        NpgsqlConnection connection,
        string schema,
        string source,
        string destination,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            $"SELECT \"{schema}\".create_topic_subscription(@source, @destination, 1)",
            connection);
        command.Parameters.AddWithValue("source", source);
        command.Parameters.AddWithValue("destination", destination);
        await command.ExecuteScalarAsync(cancellationToken);
    }

    private static async Task CreateQueueSubscriptionAsync(
        NpgsqlConnection connection,
        string schema,
        string source,
        string destination,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            $"SELECT \"{schema}\".create_queue_subscription(@source, @destination, 1)",
            connection);
        command.Parameters.AddWithValue("source", source);
        command.Parameters.AddWithValue("destination", destination);
        await command.ExecuteScalarAsync(cancellationToken);
    }
}
