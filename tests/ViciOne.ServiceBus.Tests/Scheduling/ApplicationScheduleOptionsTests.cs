using System.Reflection;
using ViciOne.ServiceBus.InMemoryTransport;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Scheduling;

public sealed class ApplicationScheduleOptionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-APPLICATION-SCHEDULE-OPTIONS", "public-entry-forwards-all-arguments-and-options")]
    public async Task ScheduleSendOptions_ReachTheProviderContextWithExactCallArgumentsAsync()
    {
        var provider = new RecordingScheduleProvider();
        IBusTopology topology = DispatchProxy.Create<IBusTopology, UnusedTopologyProxy>();
        IMessageScheduler scheduler = new MessageScheduler(provider, topology);
        Uri destination = new("loopback://localhost/scheduled-options");
        DateTimeOffset dueAt = new(2043, 4, 5, 6, 7, 8, TimeSpan.Zero);
        var message = new ScheduledPayload("refresh");
        TimeSpan lifetime = TimeSpan.FromMinutes(23);
        Guid correlationId = Guid.Parse("11000000-0000-0000-0000-000000000011");
        Guid conversationId = Guid.Parse("22000000-0000-0000-0000-000000000022");
        Guid messageId = Guid.Parse("33000000-0000-0000-0000-000000000033");
        Guid requestId = Guid.Parse("44000000-0000-0000-0000-000000000044");
        using var cancellation = new CancellationTokenSource();
        var options = new ScheduleOptions
        {
            Headers = new Dictionary<string, object?>
            {
                ["tenant"] = "north",
                ["attempt"] = 5,
                ["optional"] = null,
            },
            TimeToLive = lifetime,
            CorrelationId = correlationId,
            ConversationId = conversationId,
            MessageId = messageId,
            RequestId = requestId,
            PartitionKey = "tenant-42",
        };

        ScheduledMessage<ScheduledPayload> scheduled = await scheduler.ScheduleSendAsync(
            destination,
            dueAt,
            message,
            options,
            cancellation.Token);

        Assert.Equal(destination, provider.Destination);
        Assert.Equal(dueAt, provider.DueAt);
        Assert.Same(message, provider.Message);
        Assert.Equal(cancellation.Token, provider.CancellationToken);
        Assert.Equal(provider.TokenId, scheduled.TokenId);
        Assert.Equal(dueAt, scheduled.DueAt);
        Assert.Equal(destination, scheduled.Destination);
        Assert.Same(message, scheduled.Payload);
        InMemorySendContext<ScheduledPayload> context = Assert.IsType<InMemorySendContext<ScheduledPayload>>(provider.Context);
        Assert.Equal("north", context.Headers.Get<string>("tenant"));
        Assert.Equal(5, context.Headers.Get<int>("attempt"));
        Assert.False(context.Headers.TryGetHeader("optional", out _));
        Assert.Equal(lifetime, context.TimeToLive);
        Assert.Equal(correlationId, context.CorrelationId);
        Assert.Equal(conversationId, context.ConversationId);
        Assert.Equal(messageId, context.MessageId);
        Assert.Equal(requestId, context.RequestId);
        Assert.Equal("tenant-42", provider.PartitionKey);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-APPLICATION-SCHEDULE-OPTIONS", "null-options-fail-before-provider")]
    public async Task NullScheduleOptions_FailBeforeTheProviderIsCalledAsync()
    {
        var provider = new RecordingScheduleProvider();
        IBusTopology topology = DispatchProxy.Create<IBusTopology, UnusedTopologyProxy>();
        IMessageScheduler scheduler = new MessageScheduler(provider, topology);

        ArgumentNullException exception = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            scheduler.ScheduleSendAsync(
                new Uri("loopback://localhost/scheduled-options"),
                new DateTimeOffset(2043, 4, 5, 6, 7, 8, TimeSpan.Zero),
                new ScheduledPayload("refresh"),
                null!,
                TestContext.Current.CancellationToken));

        Assert.Equal("options", exception.ParamName);
        Assert.Null(provider.Context);
    }

    private sealed record ScheduledPayload(string Value);

    private sealed class RecordingPartitionKeyContext : PartitionKeySendContext
    {
        public string? PartitionKey { get; set; }
    }

    private sealed class RecordingScheduleProvider : IScheduleMessageProvider
    {
        public Guid TokenId { get; } = Guid.Parse("55000000-0000-0000-0000-000000000055");

        public Uri? Destination { get; private set; }

        public DateTimeOffset DueAt { get; private set; }

        public object? Message { get; private set; }

        public SendContext? Context { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        public string? PartitionKey { get; private set; }

        public async Task<ScheduledMessage<T>> ScheduleSendAsync<T>(
            Uri destinationAddress,
            DateTimeOffset dueAt,
            T message,
            IPipe<SendContext<T>> pipe,
            CancellationToken cancellationToken)
            where T : class
        {
            Destination = destinationAddress;
            DueAt = dueAt;
            Message = message;
            CancellationToken = cancellationToken;
            var context = new InMemorySendContext<T>(message, cancellationToken);
            var partition = new RecordingPartitionKeyContext();
            context.GetOrAddPayload(() => partition);
            Context = context;

            await pipe.SendAsync(context);
            PartitionKey = partition.PartitionKey;

            return new ScheduledMessageHandle<T>(TokenId, dueAt, destinationAddress, message);
        }

        public Task CancelScheduledSendAsync(Guid tokenId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task CancelScheduledSendAsync(Uri destinationAddress, Guid tokenId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private class UnusedTopologyProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"The topology must not be queried for an explicit destination ({targetMethod?.Name}).");
    }
}
