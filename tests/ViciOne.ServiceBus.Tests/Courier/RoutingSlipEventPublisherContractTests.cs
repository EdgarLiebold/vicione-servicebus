using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class RoutingSlipEventPublisherContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ROUTING", "empty-next-addresses-return-null")]
    public void EmptyRoutingSlip_HasNoExecutionOrCompensationAddress()
    {
        RoutingSlip routingSlip = new RoutingSlipBuilder(NewId.NextGuid()).Build();

        Assert.True(routingSlip.RanToCompletion());
        Assert.Null(routingSlip.GetNextExecuteAddress());
        Assert.Null(routingSlip.GetNextCompensateAddress());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-SUBSCRIPTION", "compensation-data-content-flag")]
    public async Task CompensationSubscription_WithDataOnlyIncludesDataAndExcludesVariablesAsync()
    {
        var endpoint = new RecordingEndpointProvider();
        var publishEndpoint = new UnexpectedPublishEndpoint();
        var address = new Uri("loopback://localhost/courier-events");
        var builder = new RoutingSlipBuilder(NewId.NextGuid());
        builder.AddSubscription(
            address,
            RoutingSlipEvents.ActivityCompensated,
            RoutingSlipEventContents.Data);
        RoutingSlip routingSlip = builder.Build();
        var publisher = new RoutingSlipEventPublisher(endpoint, publishEndpoint, routingSlip);
        var variables = new Dictionary<string, object> { ["private"] = "variable" };
        var data = new Dictionary<string, object> { ["receipt"] = "compensated" };

        await publisher.PublishRoutingSlipActivityCompensatedAsync(
            "ChargeCard",
            NewId.NextGuid(),
            DateTimeOffset.UtcNow,
            TimeSpan.FromMilliseconds(27),
            variables,
            data,
            TestContext.Current.CancellationToken);

        RoutingSlipActivityCompensated message = Assert.IsAssignableFrom<RoutingSlipActivityCompensated>(
            Assert.Single(endpoint.Messages));
        Assert.Empty(message.Variables);
        Assert.Equal("compensated", Assert.IsType<string>(message.Data["receipt"]));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-SUBSCRIPTION", "excluded-content-isolation")]
    public async Task ExcludedContent_UsesAnIsolatedEmptyDictionaryForEveryEventAsync()
    {
        var endpoint = new RecordingEndpointProvider();
        var address = new Uri("loopback://localhost/courier-events");
        var builder = new RoutingSlipBuilder(NewId.NextGuid());
        builder.AddSubscription(
            address,
            RoutingSlipEvents.ActivityCompensated,
            RoutingSlipEventContents.None);
        var publisher = new RoutingSlipEventPublisher(endpoint, new UnexpectedPublishEndpoint(), builder.Build());

        for (var index = 0; index < 2; index++)
        {
            await publisher.PublishRoutingSlipActivityCompensatedAsync(
                "ChargeCard",
                NewId.NextGuid(),
                DateTimeOffset.UtcNow,
                TimeSpan.Zero,
                new Dictionary<string, object> { ["variable"] = index },
                new Dictionary<string, object> { ["data"] = index },
                TestContext.Current.CancellationToken);
        }

        RoutingSlipActivityCompensated[] messages = endpoint.Messages
            .Cast<RoutingSlipActivityCompensated>()
            .ToArray();
        Assert.Equal(2, messages.Length);
        Assert.NotSame(messages[0].Variables, messages[1].Variables);
        Assert.NotSame(messages[0].Data, messages[1].Data);

        messages[0].Variables["poison"] = true;
        messages[0].Data["poison"] = true;

        Assert.Empty(messages[1].Variables);
        Assert.Empty(messages[1].Data);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-CANCELLATION", "event-publication-observes-live-token")]
    public async Task EventPublication_ObservesCancellationRequestedDuringEndpointResolutionAsync()
    {
        TimeSpan timeout = CourierTestSupport.OperationTimeout();
        var endpointProvider = new BlockingEndpointProvider();
        var builder = new RoutingSlipBuilder(NewId.NextGuid());
        builder.AddSubscription(
            new Uri("loopback://localhost/courier-events"),
            RoutingSlipEvents.Completed,
            RoutingSlipEventContents.All);
        var publisher = new RoutingSlipEventPublisher(
            endpointProvider,
            new UnexpectedPublishEndpoint(),
            builder.Build());
        using var cancellation = new CancellationTokenSource();

        Task publication = publisher.PublishRoutingSlipCompletedAsync(
            DateTimeOffset.UtcNow,
            TimeSpan.Zero,
            new Dictionary<string, object>(),
            cancellation.Token);
        await endpointProvider.Entered.Task.WaitAsync(timeout, TestContext.Current.CancellationToken);
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => publication);
        Assert.Equal(cancellation.Token, endpointProvider.ObservedToken);
    }

    private sealed class RecordingEndpointProvider : ISendEndpointProvider, ISendEndpoint
    {
        public List<object> Messages { get; } = [];

        public Task<ISendEndpoint> GetSendEndpointAsync(Uri address, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(address);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<ISendEndpoint>(this);
        }

        public Task SendAsync<T>(T message, CancellationToken cancellationToken = default)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(message);
            cancellationToken.ThrowIfCancellationRequested();
            Messages.Add(message);
            return Task.CompletedTask;
        }

        public Task SendAsync<T>(T message, SendOptions options, CancellationToken cancellationToken = default)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(options);
            return SendAsync(message, cancellationToken);
        }

        public ConnectHandle ConnectSendObserver(ISendObserver observer) => throw new NotSupportedException();
    }

    private sealed class BlockingEndpointProvider : ISendEndpointProvider
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public CancellationToken ObservedToken { get; private set; }

        public async Task<ISendEndpoint> GetSendEndpointAsync(Uri address, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(address);
            ObservedToken = cancellationToken;
            Entered.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new InvalidOperationException("Cancellation should end endpoint resolution.");
        }

        public ConnectHandle ConnectSendObserver(ISendObserver observer) => throw new NotSupportedException();
    }

    private sealed class UnexpectedPublishEndpoint : IPublishEndpoint
    {
        public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
            where T : class =>
            throw new Xunit.Sdk.XunitException("A non-supplemental subscription must suppress topology publication.");

        public Task PublishAsync<T>(T message, PublishOptions options, CancellationToken cancellationToken = default)
            where T : class =>
            throw new Xunit.Sdk.XunitException("A non-supplemental subscription must suppress topology publication.");

        public ConnectHandle ConnectPublishObserver(IPublishObserver observer) => throw new NotSupportedException();
    }
}
