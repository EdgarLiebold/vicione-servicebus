using System.Data;
using Npgsql;
using ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.PostgreSql;

public sealed class PostgreSqlIsolationTests
{
    [Theory]
    [InlineData(false, IsolationLevel.RepeatableRead, "repeatable read")]
    [InlineData(true, IsolationLevel.Serializable, "serializable")]
    [RequirementCoverage("OBL-R0-SQL-0108", "postgresql-native-owner")]
    public async Task MessageInsert_UsesTheDefaultOrExplicitHostIsolationAsync(
        bool configureIsolation,
        IsolationLevel isolationLevel,
        string expectedProviderIsolation)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync(
            "transaction-isolation",
            cancellationToken);
        await using NpgsqlConnection connection = fixture.CreateConnection();
        await connection.OpenWithinAsync(fixture.OperationTimeout, cancellationToken);
        await CreateIsolationAuditAsync(connection, fixture.Schema, cancellationToken);
        string queueName = fixture.Name("isolation-input");
        IBusControl bus = SqlBusFactory.Create(configurator =>
        {
            configurator.UsePostgres(fixture.ConnectionString, host =>
            {
                host.Schema = fixture.Schema;
                if (configureIsolation)
                    host.IsolationLevel = isolationLevel;
            });
        });
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            Guid messageId = Guid.NewGuid();
            ISendEndpoint endpoint = await bus.GetSendEndpointAsync(new Uri($"queue:{queueName}"), TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            await endpoint.SendAsync(
                    new IsolationMessage(messageId),
                    context => context.MessageId = messageId,
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await using var command = new NpgsqlCommand(
                $"SELECT isolation FROM \"{fixture.Schema}\".isolation_audit WHERE message_id = @messageId",
                connection);
            command.Parameters.AddWithValue("messageId", messageId);

            Assert.Equal(expectedProviderIsolation, Assert.IsType<string>(await command.ExecuteScalarAsync(cancellationToken)));
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    private static async Task CreateIsolationAuditAsync(
        NpgsqlConnection connection,
        string schema,
        CancellationToken cancellationToken)
    {
        string text = $"""
            CREATE TABLE "{schema}".isolation_audit
            (
                message_id uuid not null,
                isolation text not null
            );

            CREATE FUNCTION "{schema}".capture_message_isolation()
                RETURNS trigger
                LANGUAGE plpgsql
            AS
            $$
            BEGIN
                INSERT INTO "{schema}".isolation_audit(message_id, isolation)
                VALUES (NEW.message_id, current_setting('transaction_isolation'));
                RETURN NEW;
            END;
            $$;

            CREATE TRIGGER capture_message_isolation
                AFTER INSERT ON "{schema}".message
                FOR EACH ROW EXECUTE FUNCTION "{schema}".capture_message_isolation();
            """;
        await using var command = new NpgsqlCommand(text, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public sealed record IsolationMessage(Guid Id);
}
