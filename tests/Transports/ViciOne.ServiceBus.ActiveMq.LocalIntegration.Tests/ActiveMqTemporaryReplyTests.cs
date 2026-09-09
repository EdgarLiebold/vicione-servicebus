using Apache.NMS;
using ViciOne.ServiceBus.ActiveMq.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMq.LocalIntegration.Tests;

public sealed class ActiveMqTemporaryReplyTests
{
    [Theory]
    [InlineData(ActiveMqBroker.OpenWireFlavor)]
    [InlineData(ActiveMqBroker.AmqpFlavor)]
    [RequirementCoverage("OBL-R0-BRK-0455", "envelope-request-uses-provider-temporary-reply-queue")]
    public Task EnvelopeRequest_UsesProviderTemporaryReplyQueueAsync(string flavor) =>
        AssertTemporaryReplyQueueAsync(flavor, rawSerializer: false);

    [Theory]
    [InlineData(ActiveMqBroker.OpenWireFlavor)]
    [InlineData(ActiveMqBroker.AmqpFlavor)]
    [RequirementCoverage("OBL-R0-BRK-0456", "raw-request-uses-provider-temporary-reply-queue")]
    public Task RawRequest_UsesProviderTemporaryReplyQueueAsync(string flavor) =>
        AssertTemporaryReplyQueueAsync(flavor, rawSerializer: true);

    private static async Task AssertTemporaryReplyQueueAsync(string flavor, bool rawSerializer)
    {
        using ActiveMqBroker fixture = ActiveMqBroker.Create(
            flavor,
            rawSerializer ? "reply-raw" : "reply-envelope");
        string queueName = fixture.Name("service");
        Guid correlationId = Guid.NewGuid();
        var replyObserved = NewObservation<ReplyObservation>();
        var responseCount = 0;
        IBusControl bus = Bus.Factory.CreateUsingActiveMq(configurator =>
        {
            fixture.ConfigureHost(configurator);
            if (rawSerializer)
                configurator.UseRawJsonSerializer();

            configurator.ReceiveEndpoint(queueName, endpoint => endpoint.Handler<ReplyRequest>(async context =>
            {
                ActiveMqReceiveContext transport = context.Advanced().ReceiveContext.GetPayload<ActiveMqReceiveContext>();
                IDestination replyTo = Assert.IsAssignableFrom<IDestination>(transport.TransportMessage.NMSReplyTo);
                replyObserved.TrySetResult(new ReplyObservation(
                    replyTo.IsTemporary,
                    replyTo.IsQueue,
                    replyTo.IsTopic,
                    ToEndpointAddress(replyTo),
                    context.Message.CorrelationId));
                Interlocked.Increment(ref responseCount);
                await context.RespondAsync(new ReplyResponse(context.Message.CorrelationId));
            }));
        });
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            IRequestClient<ReplyRequest> client = bus.CreateRequestClient<ReplyRequest>(
                new Uri($"queue:{queueName}"),
                new RequestTimeout(fixture.OperationTimeout));
            Response<ReplyResponse> response = await client.GetResponseAsync<ReplyResponse>(
                    new ReplyRequest(correlationId),
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            ReplyObservation observed = await replyObserved.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal(correlationId, response.Message.CorrelationId);
            Assert.Equal(correlationId, observed.CorrelationId);
            Assert.True(observed.IsTemporary);
            Assert.True(observed.IsQueue);
            Assert.False(observed.IsTopic);
            Assert.False(string.IsNullOrWhiteSpace(observed.Address.AbsolutePath.Trim('/')));
            Assert.DoesNotContain(queueName, observed.Address.ToString(), StringComparison.Ordinal);
            Assert.True(string.IsNullOrEmpty(observed.Address.UserInfo));
            Assert.Equal(1, Volatile.Read(ref responseCount));
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    private static TaskCompletionSource<T> NewObservation<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static Uri ToEndpointAddress(IDestination destination) => destination switch
    {
        IQueue queue => new Uri($"queue:{queue.QueueName}"),
        ITopic topic => new Uri($"topic:{topic.TopicName}"),
        _ => throw new InvalidDataException(
            $"Destination type '{destination.GetType().FullName}' is neither a queue nor a topic."),
    };

    public sealed record ReplyRequest(Guid CorrelationId);
    public sealed record ReplyResponse(Guid CorrelationId);

    private sealed record ReplyObservation(
        bool IsTemporary,
        bool IsQueue,
        bool IsTopic,
        Uri Address,
        Guid CorrelationId);
}
