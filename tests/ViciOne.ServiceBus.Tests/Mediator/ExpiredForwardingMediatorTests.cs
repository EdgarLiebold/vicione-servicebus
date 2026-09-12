using ViciOne.ServiceBus.Mediator;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Mediator;

public sealed class ExpiredForwardingMediatorTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-FORWARDING", "expired-forward-discarded-before-mediator")]
    public async Task ExpiredMessage_IsDiscardedBeforeMediatorDispatchAsync()
    {
        TimeSpan operationTimeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observer = new CountingSendObserver();
        var deliveryCount = 0;
        var mediator = MediatorFactory.Create(configuration => configuration.Limits(MessageLimits.Conservative));
        using ConnectHandle observerHandle = mediator.ConnectSendObserver(observer);
        using ConnectHandle handlerHandle = mediator.ConnectHandler<ForwardMessage>(async context =>
        {
            if (Interlocked.Increment(ref deliveryCount) == 1)
                await context.ForwardAsync(new Uri("loopback://mediator/forward")).ConfigureAwait(false);
        });

        await mediator.SendAsync(
                new ForwardMessage { Value = "expired-mediator" },
                context => context.TimeToLive = TimeSpan.FromSeconds(-30),
                cancellationToken)
            .WaitAsync(operationTimeout, cancellationToken);

        Assert.Equal(1, Volatile.Read(ref deliveryCount));
        Assert.Equal(1, observer.PreSendCount);
        Assert.Equal(1, observer.PostSendCount);
        Assert.Equal(0, observer.SendFaultCount);
    }

    private sealed class CountingSendObserver : ISendObserver
    {
        private int _postSendCount;
        private int _preSendCount;
        private int _sendFaultCount;

        public int PostSendCount => Volatile.Read(ref _postSendCount);
        public int PreSendCount => Volatile.Read(ref _preSendCount);
        public int SendFaultCount => Volatile.Read(ref _sendFaultCount);

        public Task PreSendAsync<T>(SendContext<T> context)
            where T : class
        {
            Interlocked.Increment(ref _preSendCount);
            return Task.CompletedTask;
        }

        public Task PostSendAsync<T>(SendContext<T> context)
            where T : class
        {
            Interlocked.Increment(ref _postSendCount);
            return Task.CompletedTask;
        }

        public Task SendFaultAsync<T>(SendContext<T> context, Exception exception)
            where T : class
        {
            Interlocked.Increment(ref _sendFaultCount);
            return Task.CompletedTask;
        }
    }

    private sealed class ForwardMessage
    {
        public string Value { get; init; } = string.Empty;
    }
}
