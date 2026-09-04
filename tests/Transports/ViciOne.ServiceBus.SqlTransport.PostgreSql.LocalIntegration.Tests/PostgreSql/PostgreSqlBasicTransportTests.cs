using System.Collections.Concurrent;
using Npgsql;
using ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.PostgreSql;

public sealed class PostgreSqlBasicTransportTests
{
    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0025", "postgresql-native-owner")]
    public async Task CallerSuppliedDataSource_CarriesTheRunScopedEndpointAndDelivers()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync(
            "supplied-datasource",
            cancellationToken);
        await using NpgsqlDataSource dataSource = fixture.CreateDataSource();
        string queueName = fixture.Name("datasource-input");
        var delivered = NewObservation<ConsumeContext<BasicMessage>>();
        IBusControl bus = SqlBusFactory.Create(configurator =>
        {
            configurator.UsePostgres(dataSource);
            configurator.ReceiveEndpoint(queueName, endpoint => endpoint.Handler<BasicMessage>(context =>
            {
                delivered.TrySetResult(context);
                return Task.CompletedTask;
            }));
        });
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            Guid id = Guid.NewGuid();
            ISendEndpoint endpoint = await bus.GetSendEndpoint(new Uri($"queue:{queueName}"))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await endpoint.Send(new BasicMessage(id, "from-data-source"), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            ConsumeContext<BasicMessage> context = await delivered.Task
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            var projected = new NpgsqlConnectionStringBuilder(dataSource.ConnectionString);

            Assert.Equal(id, context.Message.Id);
            Assert.Equal(fixture.Database, projected.Database);
            Assert.Equal(fixture.Options.Host, projected.Host);
            Assert.Equal(fixture.Options.Port, projected.Port);
            Assert.Equal(fixture.Options.Username, projected.Username);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0033", "postgresql-native-owner")]
    public async Task DirectSend_WithConsumeTopologyDisabled_PopulatesTransportAddressesAndIds()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync(
            "direct-send",
            cancellationToken);
        string queueName = fixture.Name("direct-input");
        var delivered = NewObservation<ConsumeContext<BasicMessage>>();
        IBusControl bus = SqlBusFactory.Create(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.ConfigureConsumeTopology = false;
                endpoint.Handler<BasicMessage>(context =>
                {
                    delivered.TrySetResult(context);
                    return Task.CompletedTask;
                });
            });
        });
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            ISendEndpoint endpoint = await bus.GetSendEndpoint(new Uri($"queue:{queueName}"))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            Guid id = Guid.NewGuid();
            await endpoint.Send(new BasicMessage(id, "direct"), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            ConsumeContext<BasicMessage> context = await delivered.Task
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal(id, context.Message.Id);
            Assert.NotNull(context.MessageId);
            Assert.NotNull(context.ConversationId);
            Assert.Equal(queueName, context.DestinationAddress?.AbsolutePath.Trim('/'));
            Assert.NotNull(context.SourceAddress);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0034", "postgresql-native-owner")]
    public async Task PublishDerivedMessage_AutomaticTopologyDeliversToBaseContract()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync(
            "derived-publish",
            cancellationToken);
        string queueName = fixture.Name("base-input");
        var delivered = NewObservation<ConsumeContext<BaseMessage>>();
        IBusControl bus = SqlBusFactory.Create(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint => endpoint.Handler<BaseMessage>(context =>
            {
                delivered.TrySetResult(context);
                return Task.CompletedTask;
            }));
        });
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            Guid correlationId = Guid.NewGuid();
            await bus.Publish(new DerivedMessage(correlationId), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            ConsumeContext<BaseMessage> context = await delivered.Task
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal(correlationId, context.Message.CorrelationId);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0035", "postgresql-native-owner")]
    public async Task HostOnlyBus_SendCreatesOneDeliveryInTheNamedQueue()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync(
            "host-only-send",
            cancellationToken);
        string queueName = fixture.Name("standard-input");
        IBusControl bus = SqlBusFactory.Create(fixture.ConfigureHost);
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            ISendEndpoint endpoint = await bus.GetSendEndpoint(new Uri($"queue:{queueName}"))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await endpoint.Send(new BasicMessage(Guid.NewGuid(), "one"), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await using NpgsqlConnection connection = fixture.CreateConnection();
            await connection.OpenWithin(fixture.OperationTimeout, cancellationToken);

            Assert.Equal(1, await connection.DeliveryCount(fixture.Schema, queueName, 1, cancellationToken));
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0036", "postgresql-native-owner")]
    public async Task HostOnlyBus_ThreeSendsCreateOneDeliveryInEachNamedQueue()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync(
            "three-queues",
            cancellationToken);
        string[] queues = [fixture.Name("queue-a"), fixture.Name("queue-b"), fixture.Name("queue-c")];
        IBusControl bus = SqlBusFactory.Create(fixture.ConfigureHost);
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            foreach (string queue in queues)
            {
                ISendEndpoint endpoint = await bus.GetSendEndpoint(new Uri($"queue:{queue}"))
                    .WaitAsync(fixture.OperationTimeout, cancellationToken);
                await endpoint.Send(new BasicMessage(Guid.NewGuid(), queue), cancellationToken)
                    .WaitAsync(fixture.OperationTimeout, cancellationToken);
            }
            await using NpgsqlConnection connection = fixture.CreateConnection();
            await connection.OpenWithin(fixture.OperationTimeout, cancellationToken);
            var counts = new List<long>();
            foreach (string queue in queues)
                counts.Add(await connection.DeliveryCount(fixture.Schema, queue, 1, cancellationToken));

            Assert.Equal([1L, 1L, 1L], counts);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    private static TaskCompletionSource<T> NewObservation<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public sealed record BasicMessage(Guid Id, string Value);

    public record BaseMessage(Guid CorrelationId);

    public sealed record DerivedMessage(Guid CorrelationId) : BaseMessage(CorrelationId);
}
