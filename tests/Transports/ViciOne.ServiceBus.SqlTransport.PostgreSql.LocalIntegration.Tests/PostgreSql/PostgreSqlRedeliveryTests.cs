namespace ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.PostgreSql;

using System.Collections.Concurrent;
using ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class PostgreSqlRedeliveryTests
{
    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0102", "postgresql-native-owner")]
    public async Task RedeliveryHeader_IsConsumedInternallyAndDoesNotLeakToPublishedMessage()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync(
            "redelivery-header",
            cancellationToken);
        string inputQueue = fixture.Name("redelivery-input");
        string outputQueue = fixture.Name("redelivery-output");
        var attempts = new ConcurrentQueue<int>();
        var rawHeaderObservations = new ConcurrentQueue<(bool Found, string? Value)>();
        var mappedDeliveryCounts = new ConcurrentQueue<int>();
        var attemptNumber = 0;
        var persistedHeaders = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var persistedAttempt = new TaskCompletionSource<long>(TaskCreationOptions.RunContinuationsAsynchronously);
        var outbound = new TaskCompletionSource<ConsumeContext<OutboundMessage>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        IBusControl bus = SqlBusFactory.Create(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(inputQueue, endpoint =>
            {
                endpoint.UseDelayedRedelivery(redelivery =>
                    redelivery.Interval(10, TimeSpan.FromSeconds(1)));
                endpoint.Handler<InboundMessage>(async context =>
                {
                    int count = context.GetRedeliveryCount();
                    attempts.Enqueue(count);
                    bool found = context.ReceiveContext.TransportHeaders.TryGetHeader(
                        MessageHeaders.RedeliveryCount,
                        out object? rawHeader);
                    rawHeaderObservations.Enqueue((found, rawHeader?.ToString()));
                    mappedDeliveryCounts.Enqueue(
                        context.ReceiveContext.TransportHeaders.Get("DeliveryCount", default(int?)) ?? -1);
                    if (Interlocked.Increment(ref attemptNumber) == 2)
                    {
                        await using var connection = fixture.CreateConnection();
                        await connection.OpenWithin(fixture.OperationTimeout, context.CancellationToken);
                        persistedHeaders.TrySetResult(await connection.TransportHeadersForMessage(
                            fixture.Schema,
                            context.MessageId!.Value,
                            context.CancellationToken));
                        persistedAttempt.TrySetResult(await connection.DeliveryAttemptForMessage(
                            fixture.Schema,
                            context.MessageId.Value,
                            context.CancellationToken));
                        await context.Publish(new OutboundMessage(context.Message.CorrelationId));
                        return;
                    }

                    throw new ExpectedRedeliveryException(context.Message.CorrelationId);
                });
            });
            configurator.ReceiveEndpoint(outputQueue, endpoint =>
            {
                endpoint.Handler<OutboundMessage>(context =>
                {
                    outbound.TrySetResult(context);
                    return Task.CompletedTask;
                });
            });
        });
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            Guid correlationId = Guid.NewGuid();
            await bus.Publish(new InboundMessage(correlationId), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            ConsumeContext<OutboundMessage> context = await outbound.Task
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal(correlationId, context.Message.CorrelationId);
            Assert.Null(context.GetHeader(MessageHeaders.RedeliveryCount, default(int?)));
            Assert.Contains(MessageHeaders.RedeliveryCount, await persistedHeaders.Task);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }

        Assert.Equal(2, await persistedAttempt.Task);
        Assert.Equal([1, 2], mappedDeliveryCounts);
        Assert.Equal([(false, null), (true, "1")], rawHeaderObservations);
        Assert.Equal([0, 1], attempts);
    }

    private sealed record InboundMessage(Guid CorrelationId);
    private sealed record OutboundMessage(Guid CorrelationId);

    private sealed class ExpectedRedeliveryException(Guid correlationId)
        : Exception($"Expected first delivery failure for {correlationId:D}.");
}
