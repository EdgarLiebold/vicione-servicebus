using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.InMemoryTransport;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class MinimalEnvelopeRedeliveryTests
{
    private static readonly TimeSpan RedeliveryInterval = TimeSpan.FromHours(1);
    private static readonly Guid ExpectedCorrelationId =
        Guid.Parse("9bbcf5f0-596e-4e90-b3c5-1b27358c5aeb");

    [Fact]
    [RequirementCoverage("REQ-VSB-MINIMAL-ENVELOPE-REDELIVERY", "exact-virtual-delay")]
    public async Task MinimalEnvelope_IsConsumedAndRedeliveredAfterTheExactConfiguredDelay()
    {
        TimeSpan operationTimeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var firstDelivery = new TaskCompletionSource<ConsumeContext<MinimalEnvelopeMessage>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var secondDelivery = new TaskCompletionSource<ConsumeContext<MinimalEnvelopeMessage>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var unexpectedDelivery = new TaskCompletionSource<ConsumeContext<MinimalEnvelopeMessage>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var deliveryNumber = 0;

        await using var provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(operationTimeout, operationTimeout);
                configuration.AddHandler<MinimalEnvelopeMessage>(context =>
                {
                    switch (Interlocked.Increment(ref deliveryNumber))
                    {
                        case 1:
                            firstDelivery.TrySetResult(context);
                            return Task.FromException(new ExpectedRedeliveryException());
                        case 2:
                            secondDelivery.TrySetResult(context);
                            return Task.CompletedTask;
                        default:
                            unexpectedDelivery.TrySetResult(context);
                            return Task.CompletedTask;
                    }
                });
                configuration.AddConfigureEndpointsCallback((_, _, endpoint) =>
                    endpoint.UseDelayedRedelivery(redelivery =>
                        redelivery.Intervals(RedeliveryInterval)));
            })
            .BuildServiceProvider(validateScopes: true);
        ITestHarness harness = await provider.StartTestHarness()
            .WaitAsync(operationTimeout, cancellationToken);
        var redeliveryObserver = new ScheduledRedeliveryObserver();
        using ConnectHandle observerHandle = harness.Bus.ConnectSendObserver(redeliveryObserver);

        try
        {
            string messageUrn = MessageUrn.ForTypeString<MinimalEnvelopeMessage>();
            string body = $$"""
                {
                  "message": {
                    "correlationId": "{{ExpectedCorrelationId:D}}"
                  },
                  "messageType": [
                    "{{messageUrn}}"
                  ]
                }
                """;

            await harness.Bus.Publish<MinimalEnvelopeMessage>(
                    new { CorrelationId = Guid.Empty },
                    context => context.Serializer = new CopyBodySerializer(
                        SystemTextJsonMessageSerializer.JsonContentType,
                        new StringMessageBody(body)),
                    cancellationToken)
                .WaitAsync(operationTimeout, cancellationToken);

            ConsumeContext<MinimalEnvelopeMessage> first = await firstDelivery.Task.WaitAsync(
                operationTimeout,
                cancellationToken);
            SendContext scheduled = await redeliveryObserver.Scheduled.WaitAsync(
                operationTimeout,
                cancellationToken);

            Assert.Equal(RedeliveryInterval, scheduled.Delay);
            Assert.False(secondDelivery.Task.IsCompleted);

            IInMemoryDelayProvider delayProvider =
                provider.GetRequiredService<IInMemoryDelayProvider>();
            delayProvider.Advance(RedeliveryInterval);

            ConsumeContext<MinimalEnvelopeMessage> second = await secondDelivery.Task.WaitAsync(
                operationTimeout,
                cancellationToken);

            AssertDelivery(first, expectedRedeliveryCount: 0, messageUrn);
            AssertDelivery(second, expectedRedeliveryCount: 1, messageUrn);
            Assert.False(unexpectedDelivery.Task.IsCompleted);
        }
        finally
        {
            await harness.Stop(CancellationToken.None)
                .WaitAsync(operationTimeout, CancellationToken.None);
        }
    }

    private static void AssertDelivery(
        ConsumeContext<MinimalEnvelopeMessage> context,
        int expectedRedeliveryCount,
        string expectedMessageUrn)
    {
        Assert.Equal(ExpectedCorrelationId, context.Message.CorrelationId);
        Assert.Equal(expectedRedeliveryCount, context.GetRedeliveryCount());
        Assert.Equal(
            SystemTextJsonMessageSerializer.JsonContentType,
            context.ReceiveContext.ContentType);
        Assert.Contains(
            expectedMessageUrn,
            context.SupportedMessageTypes,
            StringComparer.Ordinal);
    }

    public interface MinimalEnvelopeMessage
    {
        Guid CorrelationId { get; }
    }

    private sealed class ScheduledRedeliveryObserver : ISendObserver
    {
        private readonly TaskCompletionSource<SendContext> _scheduled = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<SendContext> Scheduled => _scheduled.Task;

        public Task PreSend<T>(SendContext<T> context)
            where T : class => Task.CompletedTask;

        public Task PostSend<T>(SendContext<T> context)
            where T : class
        {
            if (typeof(T) == typeof(MinimalEnvelopeMessage) && context.Delay.HasValue)
                _scheduled.TrySetResult(context);

            return Task.CompletedTask;
        }

        public Task SendFault<T>(SendContext<T> context, Exception exception)
            where T : class
        {
            if (typeof(T) == typeof(MinimalEnvelopeMessage) && context.Delay.HasValue)
                _scheduled.TrySetException(exception);

            return Task.CompletedTask;
        }
    }

    private sealed class ExpectedRedeliveryException : Exception;
}
