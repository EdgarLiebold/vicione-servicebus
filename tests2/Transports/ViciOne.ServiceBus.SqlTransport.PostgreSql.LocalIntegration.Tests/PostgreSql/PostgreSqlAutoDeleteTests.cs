namespace ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.PostgreSql;

using Npgsql;
using ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class PostgreSqlAutoDeleteTests
{
    [Theory]
    [InlineData(true, 300)]
    [InlineData(false, null)]
    [RequirementCoverage("OBL-R0-SQL-0115", "postgresql-native-owner")]
    public async Task EndpointDefinition_PersistsTheTemporaryDefaultOrNoAutoDelete(
        bool isTemporary,
        int? expectedAutoDeleteSeconds)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync(
            "auto-delete",
            cancellationToken);
        string queueName = fixture.Name(isTemporary ? "temporary" : "durable");
        var definition = new TestEndpointDefinition(queueName, isTemporary);
        IBusControl bus = SqlBusFactory.Create(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(definition, endpoint => endpoint.Handler<AutoDeleteMessage>(_ => Task.CompletedTask));
        });
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            await using NpgsqlConnection connection = fixture.CreateConnection();
            await connection.OpenWithin(fixture.OperationTimeout, cancellationToken);
            await using var command = new NpgsqlCommand(
                $"SELECT auto_delete FROM \"{fixture.Schema}\".queue WHERE name = @queue AND type = 1",
                connection);
            command.Parameters.AddWithValue("queue", queueName);
            object? actual = await command.ExecuteScalarAsync(cancellationToken);

            if (expectedAutoDeleteSeconds.HasValue)
                Assert.Equal(expectedAutoDeleteSeconds.Value, Assert.IsType<int>(actual));
            else
                Assert.Equal(DBNull.Value, actual);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    private sealed class TestEndpointDefinition(string name, bool isTemporary) : IEndpointDefinition
    {
        public bool IsTemporary => isTemporary;

        public int? PrefetchCount => null;

        public int? ConcurrentMessageLimit => null;

        public bool ConfigureConsumeTopology => true;

        public string GetEndpointName(IEndpointNameFormatter formatter) => name;

        public void Configure<T>(T configurator, IRegistrationContext? context = null)
            where T : IReceiveEndpointConfigurator
        {
        }
    }

    public sealed record AutoDeleteMessage(Guid Id);
}
