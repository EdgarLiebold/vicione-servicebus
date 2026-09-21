using Microsoft.Data.SqlClient;
using ViciOne.ServiceBus.SqlTransport.SqlServer.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.SqlServer.LocalIntegration.Tests.SqlServer;

public sealed class SqlServerSchedulingTests
{
    private static readonly TimeSpan ScheduledDelay = TimeSpan.FromSeconds(3);

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0087", "sqlserver-native-owner")]
    public Task DelayedSend_PersistsFutureEnqueueTimeAndDeliversExactlyOnceAsync() =>
        AssertDelayedDeliveryAsync(publish: false);

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0089", "sqlserver-native-owner")]
    public Task DelayedPublish_PersistsFutureEnqueueTimeAndDeliversExactlyOnceAsync() =>
        AssertDelayedDeliveryAsync(publish: true);

    private static async Task AssertDelayedDeliveryAsync(bool publish)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using SqlServerTestDatabase fixture = await SqlServerTestDatabase.CreateAsync(
            publish ? "delayed-publish" : "delayed-send",
            cancellationToken);
        string queueName = fixture.Name(publish ? "publish-input" : "send-input");
        var deliveries = 0;
        var blockerStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseBlocker = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var delivered = new TaskCompletionSource<ConsumeContext<ScheduledMessage>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        IBusControl bus = SqlBusFactory.Create(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.ConfigureConsumeTopology = publish;
                endpoint.PrefetchCount = 1;
                endpoint.ConcurrentMessageLimit = 1;
                endpoint.Handler<BlockerMessage>(async _ =>
                {
                    blockerStarted.TrySetResult();
                    await releaseBlocker.Task;
                });
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
            ISendEndpoint endpoint = await bus.GetSendEndpointAsync(new Uri($"queue:{queueName}"))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await endpoint.SendAsync(new BlockerMessage(), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await blockerStarted.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            await using SqlConnection connection = fixture.CreateConnection();
            await connection.OpenWithinAsync(fixture.OperationTimeout, cancellationToken);
            DateTime databaseBeforeSendUtc = await connection.DatabaseNowUtcAsync(cancellationToken);
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

            DateTime databaseAfterSendUtc = await connection.DatabaseNowUtcAsync(cancellationToken);
            SqlServerTransportInspection.ScheduledDelivery scheduled =
                await connection.ScheduledDeliveryForMessageAsync(fixture.Schema, messageId, cancellationToken);

            Assert.True(scheduled.EnqueueTimeUtc - databaseBeforeSendUtc >= ScheduledDelay);
            Assert.True(scheduled.EnqueueTimeUtc > databaseAfterSendUtc,
                $"Scheduled enqueue time {scheduled.EnqueueTimeUtc:O} was not after SQL Server time at send completion {databaseAfterSendUtc:O}.");
            releaseBlocker.TrySetResult();
            ConsumeContext<ScheduledMessage> context = await delivered.Task
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.Equal(message.Id, context.Message.Id);
        }
        finally
        {
            releaseBlocker.TrySetResult();
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }

        Assert.Equal(1, Volatile.Read(ref deliveries));
    }

    private sealed record ScheduledMessage(Guid Id);

    private sealed record BlockerMessage;
}
