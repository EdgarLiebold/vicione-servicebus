using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Context.Consumption;

public sealed class ApplicationConsumeContextOutgoingTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-APPLICATION-CONSUME-OUTGOING", "all-send-publish-route-and-response-paths")]
    public async Task OutgoingAndResponseOperations_PreserveScopeRoutesAndApplicationOptionsAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new InMemoryTestHarness($"consume-outgoing-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        var sentSeen = new TaskCompletionSource<ConsumeContext<ScopedSent>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var publishedSeen = new TaskCompletionSource<ConsumeContext<ScopedPublished>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var routedSeen = new TaskCompletionSource<ConsumeContext<RoutedSent>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var defaultPublishedSeen = new TaskCompletionSource<ConsumeContext<DefaultPublished>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        EnvelopeExpectation sendExpected = EnvelopeExpectation.Create(1, includeRequestId: true);
        EnvelopeExpectation publishExpected = EnvelopeExpectation.Create(2, includeRequestId: true);
        EnvelopeExpectation responseExpected = EnvelopeExpectation.Create(3, includeRequestId: false);
        harness.InMemoryBusConfiguring += bus => bus.Route<RoutedSent>(harness.InputQueueAddress);
        harness.AddHandler<ScopedRequest>(async context =>
        {
            Assert.Same(context.Outgoing, context.Outgoing);
            await context.Outgoing.SendAsync(new RoutedSent(context.Message.Value), context.CancellationToken);
            await context.Outgoing.SendAsync(
                harness.InputQueueAddress,
                new ScopedSent(context.Message.Value),
                sendExpected.ToSendOptions(),
                context.CancellationToken);
            await context.Outgoing.PublishAsync(
                new ScopedPublished(context.Message.Value),
                publishExpected.ToPublishOptions(),
                context.CancellationToken);
            await context.Outgoing.PublishAsync(
                new DefaultPublished(context.Message.Value),
                context.CancellationToken);
            ConfigurationException missingRoute = await Assert.ThrowsAsync<ConfigurationException>(() =>
                context.Outgoing.SendAsync(new UnroutedSent(context.Message.Value), context.CancellationToken));
            Assert.Contains(typeof(UnroutedSent).FullName!, missingRoute.Message, StringComparison.Ordinal);
            await context.RespondAsync(
                new ScopedResponse(context.Message.Value),
                responseExpected.ToSendOptions());
        });
        harness.AddHandler<ScopedSent>(context =>
        {
            sentSeen.TrySetResult(context);
            return Task.CompletedTask;
        });
        harness.AddHandler<ScopedPublished>(context =>
        {
            publishedSeen.TrySetResult(context);
            return Task.CompletedTask;
        });
        harness.AddHandler<RoutedSent>(context =>
        {
            routedSeen.TrySetResult(context);
            return Task.CompletedTask;
        });
        harness.AddHandler<DefaultPublished>(context =>
        {
            defaultPublishedSeen.TrySetResult(context);
            return Task.CompletedTask;
        });

        await harness.StartAsync(cancellationToken);
        try
        {
            IRequestClient<ScopedRequest> client =
                harness.Bus.CreateRequestClient<ScopedRequest>(harness.InputQueueAddress, new RequestTimeout(timeout));

            Response<ScopedResponse> response = await client.GetResponseAsync<ScopedResponse>(
                new ScopedRequest("order-42"),
                cancellationToken);
            ConsumeContext<ScopedSent> sent = await sentSeen.Task.WaitAsync(timeout, cancellationToken);
            ConsumeContext<ScopedPublished> published = await publishedSeen.Task.WaitAsync(timeout, cancellationToken);
            ConsumeContext<RoutedSent> routed = await routedSeen.Task.WaitAsync(timeout, cancellationToken);
            ConsumeContext<DefaultPublished> defaultPublished = await defaultPublishedSeen.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal(new ScopedResponse("order-42"), response.Message);
            Assert.Equal(new RoutedSent("order-42"), routed.Message);
            Assert.Equal(new DefaultPublished("order-42"), defaultPublished.Message);
            AssertEnvelope(sent, sendExpected);
            AssertEnvelope(published, publishExpected);
            AssertEnvelope(response, responseExpected, assertRequestId: false);
            Assert.NotNull(response.RequestId);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static void AssertEnvelope<T>(ConsumeContext<T> context, EnvelopeExpectation expected)
        where T : class => AssertEnvelope((MessageContext)context, expected);

    private static void AssertEnvelope(MessageContext context, EnvelopeExpectation expected, bool assertRequestId = true)
    {
        Assert.Equal(expected.Headers["application"], context.Headers.Get<string>("application"));
        Assert.Equal(expected.Headers["attempt"], context.Headers.Get<int>("attempt"));
        TimeSpan observedLifetime = Assert.IsType<DateTimeOffset>(context.ExpirationTime)
            - Assert.IsType<DateTimeOffset>(context.SentTime);
        Assert.InRange(observedLifetime, expected.TimeToLive, expected.TimeToLive.Add(TimeSpan.FromSeconds(1)));
        Assert.Equal(expected.CorrelationId, context.CorrelationId);
        Assert.Equal(expected.ConversationId, context.ConversationId);
        Assert.Equal(expected.MessageId, context.MessageId);
        if (assertRequestId)
            Assert.Equal(expected.RequestId, context.RequestId);
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private sealed record ScopedRequest(string Value);

    private sealed record ScopedResponse(string Value);

    private sealed record ScopedSent(string Value);

    private sealed record ScopedPublished(string Value);

    private sealed record RoutedSent(string Value);

    private sealed record UnroutedSent(string Value);

    private sealed record DefaultPublished(string Value);

    private sealed record EnvelopeExpectation(
        IReadOnlyDictionary<string, object?> Headers,
        TimeSpan TimeToLive,
        Guid CorrelationId,
        Guid ConversationId,
        Guid MessageId,
        Guid? RequestId)
    {
        public static EnvelopeExpectation Create(int discriminator, bool includeRequestId) => new(
            new Dictionary<string, object?>
            {
                ["application"] = $"scope-{discriminator}",
                ["attempt"] = discriminator,
            },
            TimeSpan.FromMinutes(10 + discriminator),
            GuidFrom(discriminator, 1),
            GuidFrom(discriminator, 2),
            GuidFrom(discriminator, 3),
            includeRequestId ? GuidFrom(discriminator, 4) : null);

        public SendOptions ToSendOptions() => new()
        {
            Headers = Headers,
            TimeToLive = TimeToLive,
            CorrelationId = CorrelationId,
            ConversationId = ConversationId,
            MessageId = MessageId,
            RequestId = RequestId,
        };

        public PublishOptions ToPublishOptions() => new()
        {
            Headers = Headers,
            TimeToLive = TimeToLive,
            CorrelationId = CorrelationId,
            ConversationId = ConversationId,
            MessageId = MessageId,
            RequestId = RequestId,
        };

        private static Guid GuidFrom(int discriminator, int field) =>
            Guid.Parse($"{discriminator}{field}000000-0000-0000-0000-000000000000");
    }
}
