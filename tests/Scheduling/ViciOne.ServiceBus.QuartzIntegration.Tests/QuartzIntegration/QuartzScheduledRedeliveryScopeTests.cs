using System.Collections.Concurrent;
using ViciOne.ServiceBus.QuartzIntegration.Tests.Testing;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.QuartzIntegration.Tests.QuartzIntegration;

[Collection(QuartzIntegrationCollection.Name)]
public sealed class QuartzScheduledRedeliveryScopeTests
{
    [Theory]
    [InlineData(RedeliveryScope.Handler)]
    [InlineData(RedeliveryScope.ConsumerMessage)]
    [InlineData(RedeliveryScope.Endpoint)]
    [InlineData(RedeliveryScope.Bus)]
    [RequirementCoverage("REQ-VSB-QUARTZ-REDELIVERY-SCOPE", "handler-consumer-message-endpoint-and-bus")]
    public async Task ConfiguredScope_RedeliversTwiceThroughQuartzAndPreservesTheCounterAsync(RedeliveryScope scope)
    {
        TimeSpan timeout = OperationTimeout();
        var probe = new RedeliveryProbe();
        string queueName = $"quartz-redelivery-{scope.ToString().ToLowerInvariant()}";
        var inputAddress = new Uri($"loopback://localhost/{queueName}");
        await using QuartzTestBus fixture = await QuartzTestBus.StartAsync(
            timeout,
            configure: configurator =>
            {
                if (scope == RedeliveryScope.Bus)
                    configurator.UseScheduledRedelivery(ConfigureIntervals);

                configurator.ReceiveEndpoint(queueName, endpoint =>
                {
                    if (scope == RedeliveryScope.Endpoint)
                        endpoint.UseScheduledRedelivery(ConfigureIntervals);

                    if (scope == RedeliveryScope.Handler)
                    {
                        endpoint.Handler<RedeliveryPayload>(
                            probe.ConsumeAsync,
                            handler => handler.UseScheduledRedelivery(ConfigureIntervals));
                    }
                    else
                    {
                        endpoint.Consumer<RedeliveryConsumer>(
                            () => new RedeliveryConsumer(probe),
                            consumer =>
                            {
                                if (scope == RedeliveryScope.ConsumerMessage)
                                {
                                    consumer.Message<RedeliveryPayload>(message =>
                                        message.UseScheduledRedelivery(ConfigureIntervals));
                                }
                            });
                    }
                });
            });
        var scheduledCommands = new ConsumeCompletionObserver<ScheduleMessage>(_ => true, expectedCount: 2);
        using ConnectHandle observer = fixture.Bus.ConnectConsumeObserver(scheduledCommands);
        ISendEndpoint input = await fixture.Bus.GetSendEndpointAsync(inputAddress, TestContext.Current.CancellationToken).WaitAsync(timeout, TestContext.Current.CancellationToken);

        await input.SendAsync(new RedeliveryPayload(scope.ToString()), TestContext.Current.CancellationToken);
        int deliveredRedeliveryCount = await probe.Delivered
            .WaitAsync(timeout, TestContext.Current.CancellationToken);
        await scheduledCommands.Completed.WaitAsync(timeout, TestContext.Current.CancellationToken);

        Assert.Equal([0, 1, 2], probe.RedeliveryCounts.ToArray());
        Assert.Equal(2, deliveredRedeliveryCount);
        Assert.Equal(2, scheduledCommands.ObservedCount);
    }

    private static void ConfigureIntervals(IRetryConfigurator redelivery) =>
        redelivery.Intervals(TimeSpan.Zero, TimeSpan.Zero);

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    public enum RedeliveryScope
    {
        Handler,
        ConsumerMessage,
        Endpoint,
        Bus,
    }

    private sealed record RedeliveryPayload(string Value);

    private sealed class RedeliveryConsumer(RedeliveryProbe probe) : IConsumer<RedeliveryPayload>
    {
        public Task ConsumeAsync(ConsumeContext<RedeliveryPayload> context) => probe.ConsumeAsync(context);
    }

    private sealed class RedeliveryProbe
    {
        private readonly TaskCompletionSource<int> _delivered =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _attempts;

        public Task<int> Delivered => _delivered.Task;
        public ConcurrentQueue<int> RedeliveryCounts { get; } = new();

        public Task ConsumeAsync(ConsumeContext<RedeliveryPayload> context)
        {
            RedeliveryCounts.Enqueue(context.Advanced().GetRedeliveryCount());
            if (Interlocked.Increment(ref _attempts) <= 2)
                throw new InvalidOperationException("Intentional redelivery failure.");

            _delivered.TrySetResult(context.Advanced().GetRedeliveryCount());
            return Task.CompletedTask;
        }
    }
}
