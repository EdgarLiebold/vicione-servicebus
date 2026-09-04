using ViciOne.ServiceBus.RabbitMq.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.LocalIntegration.Tests;

public sealed class RabbitMqSchedulingTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-NATIVE-SCHEDULING", "delayed-exchange-owns-future-delivery-and-header")]
    public async Task FutureSchedule_IsProviderOwnedThenDeliveredExactlyOnceAsync()
    {
        using RabbitMqBroker fixture = RabbitMqBroker.Create("schedule");
        string queue = fixture.Name("input");
        Guid expected = NewId.NextGuid();
        var received = new TaskCompletionSource<ConsumeContext<ScheduledMessage>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        int entries = 0;
        IBusControl bus = Bus.Factory.CreateUsingRabbitMq(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.UseDelayedMessageScheduler();
            configurator.ReceiveEndpoint(queue, endpoint =>
            {
                endpoint.Durable = true;
                endpoint.AutoDelete = false;
                endpoint.Handler<ScheduledMessage>(context =>
                {
                    if (Interlocked.Increment(ref entries) != 1)
                        received.TrySetException(new InvalidDataException("The scheduled message was delivered more than once."));
                    else
                        received.TrySetResult(context);
                    return Task.CompletedTask;
                });
            });
        });
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            IMessageScheduler scheduler = bus.CreateDelayedMessageScheduler();
            await scheduler.ScheduleSendAsync(
                    new Uri($"queue:{queue}"),
                    TimeProvider.System.GetUtcNow().UtcDateTime.AddSeconds(1),
                    new ScheduledMessage(expected),
                    Pipe.Execute<SendContext<ScheduledMessage>>(
                        context => context.Headers.Set("scheduled-marker", "exact-marker")),
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            ConsumeContext<ScheduledMessage> actual = await received.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            RabbitMqBasicConsumeContext provider = actual.GetPayload<RabbitMqBasicConsumeContext>();
            Assert.Equal(expected, actual.Message.CorrelationId);
            Assert.Equal("exact-marker", actual.Headers.Get<string>("scheduled-marker"));
            Assert.NotNull(provider.Properties.Headers);
            Assert.True(provider.Properties.Headers!.TryGetValue("x-delay", out object? delay));
            Assert.True(Convert.ToInt64(delay) < 0, $"Expected the broker-negated x-delay header, actual: {delay}.");
            RabbitMqBroker.ExchangeState exchange = await fixture.ExchangeAsync(queue + "_delay", cancellationToken);
            Assert.True(exchange.Exists);
            Assert.Equal("x-delayed-message", exchange.Type);

            await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            started = false;
            Assert.Equal(1, entries);
            Assert.Equal(0, (await fixture.QueueAsync(queue, cancellationToken)).Messages);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            await fixture.CleanupAsync();
        }
    }

    private sealed record ScheduledMessage(Guid CorrelationId);
}
