using Npgsql;
using ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.PostgreSql;

public sealed class PostgreSqlSchedulingTests
{
    private static readonly TimeSpan ScheduledDelay = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan MinimumStoredDelay = TimeSpan.FromSeconds(2);

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0086", "postgresql-native-owner")]
    public Task DelayedSend_PersistsFutureEnqueueTimeAndDeliversExactlyOnceAsync() =>
        AssertDelayedDeliveryAsync(publish: false);

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0088", "postgresql-native-owner")]
    public Task DelayedPublish_PersistsFutureEnqueueTimeAndDeliversExactlyOnceAsync() =>
        AssertDelayedDeliveryAsync(publish: true);

    private static async Task AssertDelayedDeliveryAsync(bool publish)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync(
            publish ? "delayed-publish" : "delayed-send",
            cancellationToken);
        string queueName = fixture.Name(publish ? "publish-input" : "send-input");
        var deliveries = 0;
        var delivered = new TaskCompletionSource<ConsumeContext<ScheduledMessage>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        IBusControl bus = SqlBusFactory.Create(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.ConfigureConsumeTopology = publish;
                endpoint.Handler<ScheduledMessage>(context =>
                {
                    Interlocked.Increment(ref deliveries);
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
            Guid messageId = Guid.NewGuid();
            var message = new ScheduledMessage(Guid.NewGuid());
            if (publish)
            {
                await bus.PublishAsync(
                        message,
                        context =>
                        {
                            context.MessageId = messageId;
                            context.Delay = ScheduledDelay;
                        },
                        cancellationToken)
                    .WaitAsync(fixture.OperationTimeout, cancellationToken);
            }
            else
            {
                ISendEndpoint endpoint = await bus.GetSendEndpointAsync(new Uri($"queue:{queueName}"))
                    .WaitAsync(fixture.OperationTimeout, cancellationToken);
                await endpoint.SendAsync(
                        message,
                        context =>
                        {
                            context.MessageId = messageId;
                            context.Delay = ScheduledDelay;
                        },
                        cancellationToken)
                    .WaitAsync(fixture.OperationTimeout, cancellationToken);
            }

            await using NpgsqlConnection connection = fixture.CreateConnection();
            await connection.OpenWithinAsync(fixture.OperationTimeout, cancellationToken);
            PostgreSqlTransportInspection.ScheduledDelivery scheduled =
                await connection.ScheduledDeliveryForMessageAsync(fixture.Schema, messageId, cancellationToken);

            Assert.True(scheduled.EnqueueTimeUtc - scheduled.DatabaseNowUtc >= MinimumStoredDelay);
            ConsumeContext<ScheduledMessage> context = await delivered.Task
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.Equal(message.Id, context.Message.Id);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }

        Assert.Equal(1, Volatile.Read(ref deliveries));
    }

    private sealed record ScheduledMessage(Guid Id);
}
