using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.InMemoryTransport;

public sealed class ApplicationOutgoingOptionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-APPLICATION-OUTGOING-OPTIONS", "send-publish-round-trip-and-capability-failure")]
    public async Task SendAndPublishOptions_ReachTheirEnvelopesAndRejectUnsupportedCapabilitiesAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new InMemoryTestHarness($"application-options-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        var sent = new TaskCompletionSource<ConsumeContext<SentMessage>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var published = new TaskCompletionSource<ConsumeContext<PublishedMessage>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        harness.OnConfigureInMemoryReceiveEndpoint += endpoint =>
        {
            endpoint.Handler<SentMessage>(context =>
            {
                sent.TrySetResult(context);
                return Task.CompletedTask;
            });
            endpoint.Handler<PublishedMessage>(context =>
            {
                published.TrySetResult(context);
                return Task.CompletedTask;
            });
        };

        await harness.StartAsync(cancellationToken);
        try
        {
            EnvelopeExpectation sendExpected = EnvelopeExpectation.Create(1);
            EnvelopeExpectation publishExpected = EnvelopeExpectation.Create(2);
            var sendOptions = new SendOptions
            {
                Headers = sendExpected.Headers,
                TimeToLive = sendExpected.TimeToLive,
                CorrelationId = sendExpected.CorrelationId,
                ConversationId = sendExpected.ConversationId,
                MessageId = sendExpected.MessageId,
                RequestId = sendExpected.RequestId,
            };
            var publishOptions = new PublishOptions
            {
                Headers = publishExpected.Headers,
                TimeToLive = publishExpected.TimeToLive,
                CorrelationId = publishExpected.CorrelationId,
                ConversationId = publishExpected.ConversationId,
                MessageId = publishExpected.MessageId,
                RequestId = publishExpected.RequestId,
            };

            await harness.InputQueueSendEndpoint.SendAsync(
                new SentMessage("send"),
                sendOptions,
                cancellationToken);
            await harness.Bus.PublishAsync(
                new PublishedMessage("publish"),
                publishOptions,
                cancellationToken);

            AssertEnvelope(await sent.Task.WaitAsync(timeout, cancellationToken), sendExpected);
            AssertEnvelope(await published.Task.WaitAsync(timeout, cancellationToken), publishExpected);

            await Assert.ThrowsAsync<NotSupportedException>(() => harness.InputQueueSendEndpoint.SendAsync(
                new SentMessage("unsupported"),
                new SendOptions { PartitionKey = "tenant-42" },
                cancellationToken));
            await Assert.ThrowsAsync<NotSupportedException>(() => harness.Bus.PublishAsync(
                new PublishedMessage("unsupported"),
                new PublishOptions { PartitionKey = "tenant-42" },
                cancellationToken));
            await Assert.ThrowsAsync<ArgumentNullException>(() => harness.InputQueueSendEndpoint.SendAsync(
                new SentMessage("null-options"),
                null!,
                cancellationToken));
            await Assert.ThrowsAsync<ArgumentNullException>(() => harness.Bus.PublishAsync(
                new PublishedMessage("null-options"),
                null!,
                cancellationToken));
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static void AssertEnvelope<T>(ConsumeContext<T> context, EnvelopeExpectation expected)
        where T : class
    {
        Assert.Equal(expected.Headers["application"], context.Headers.Get<string>("application"));
        Assert.Equal(expected.Headers["attempt"], context.Headers.Get<int>("attempt"));
        TimeSpan observedLifetime = Assert.IsType<DateTimeOffset>(context.ExpirationTime)
            - Assert.IsType<DateTimeOffset>(context.SentTime);
        Assert.InRange(observedLifetime, expected.TimeToLive, expected.TimeToLive.Add(TimeSpan.FromSeconds(1)));
        Assert.Equal(expected.CorrelationId, context.CorrelationId);
        Assert.Equal(expected.ConversationId, context.ConversationId);
        Assert.Equal(expected.MessageId, context.MessageId);
        Assert.Equal(expected.RequestId, context.RequestId);
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private sealed record SentMessage(string Value);

    private sealed record PublishedMessage(string Value);

    private sealed record EnvelopeExpectation(
        IReadOnlyDictionary<string, object?> Headers,
        TimeSpan TimeToLive,
        Guid CorrelationId,
        Guid ConversationId,
        Guid MessageId,
        Guid RequestId)
    {
        public static EnvelopeExpectation Create(int discriminator) => new(
            new Dictionary<string, object?>
            {
                ["application"] = $"app-{discriminator}",
                ["attempt"] = discriminator,
            },
            TimeSpan.FromMinutes(10 + discriminator),
            GuidFrom(discriminator, 1),
            GuidFrom(discriminator, 2),
            GuidFrom(discriminator, 3),
            GuidFrom(discriminator, 4));

        private static Guid GuidFrom(int discriminator, int field) =>
            Guid.Parse($"{discriminator}{field}000000-0000-0000-0000-000000000000");
    }
}
