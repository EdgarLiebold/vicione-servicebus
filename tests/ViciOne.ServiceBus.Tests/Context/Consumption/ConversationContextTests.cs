using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Context.Consumption;

public sealed class ConversationContextTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CONVERSATION-ID", "root-send-and-publish")]
    public async Task RootSendAndPublish_CreateIndependentNonEmptyConversationIdsAsync()
    {
        using var harness = CreateHarness();
        HandlerTestHarness<RootMessage> handler = harness.AddHandler<RootMessage>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var sent = new RootMessage(NewId.NextGuid(), "sent");
        var published = new RootMessage(NewId.NextGuid(), "published");

        await harness.StartAsync(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.SendAsync(sent, cancellationToken);
            await harness.Bus.PublishAsync(published, cancellationToken);

            ConsumeContext<RootMessage> sentContext = (await handler.Consumed
                .SelectAsync(observation => observation.Context.Message.CorrelationId == sent.CorrelationId, cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken)).Context;
            ConsumeContext<RootMessage> publishedContext = (await handler.Consumed
                .SelectAsync(observation => observation.Context.Message.CorrelationId == published.CorrelationId, cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken)).Context;

            Assert.NotNull(sentContext.ConversationId);
            Assert.NotEqual(Guid.Empty, sentContext.ConversationId);
            Assert.NotNull(publishedContext.ConversationId);
            Assert.NotEqual(Guid.Empty, publishedContext.ConversationId);
            Assert.NotEqual(sentContext.ConversationId, publishedContext.ConversationId);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONVERSATION-ID", "consumer-inheritance-and-source")]
    public async Task MessageProducedInsideAConsumer_InheritsConversationAndEndpointSourceAsync()
    {
        using var harness = CreateHarness();
        harness.AddHandler<ParentMessage>(context =>
            context.Advanced().PublishAsync(new ChildMessage(context.Message.CorrelationId, "inherited")));
        HandlerTestHarness<ChildMessage> childHandler = harness.AddHandler<ChildMessage>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Guid conversationId = NewId.NextGuid();
        var parent = new ParentMessage(NewId.NextGuid());

        await harness.StartAsync(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.SendAsync(
                parent,
                context => context.ConversationId = conversationId,
                cancellationToken);
            ConsumeContext<ChildMessage> child =
                (await childHandler.Consumed.SelectAsync(cancellationToken).FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken)).Context;

            Assert.Equal(conversationId, child.ConversationId);
            Assert.Equal(parent.CorrelationId, child.InitiatorId);
            Assert.Equal(harness.InputQueueAddress, child.SourceAddress);
            Assert.Null(child.Headers.Get<Guid>(MessageHeaders.InitiatingConversationId));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONVERSATION-ID", "consumer-starts-new-conversation")]
    public async Task StartNewConversationInsideAConsumer_RecordsThePreviousConversationAsync()
    {
        using var harness = CreateHarness();
        harness.AddHandler<ParentMessage>(context =>
            context.Advanced().PublishAsync(
                new ChildMessage(context.Message.CorrelationId, "new"),
                sendContext => sendContext.StartNewConversation()));
        HandlerTestHarness<ChildMessage> childHandler = harness.AddHandler<ChildMessage>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Guid originalConversationId = NewId.NextGuid();
        var parent = new ParentMessage(NewId.NextGuid());

        await harness.StartAsync(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.SendAsync(
                parent,
                context => context.ConversationId = originalConversationId,
                cancellationToken);
            ConsumeContext<ChildMessage> child =
                (await childHandler.Consumed.SelectAsync(cancellationToken).FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken)).Context;

            Assert.NotNull(child.ConversationId);
            Assert.NotEqual(Guid.Empty, child.ConversationId);
            Assert.NotEqual(originalConversationId, child.ConversationId);
            Assert.Equal(originalConversationId,
                child.Headers.Get<Guid>(MessageHeaders.InitiatingConversationId));
            Assert.Equal(parent.CorrelationId, child.InitiatorId);
            Assert.Equal(harness.InputQueueAddress, child.SourceAddress);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONVERSATION-ID", "explicit-root-conversation")]
    public async Task ExplicitRootConversation_UsesTheExactIdWithoutAnInitiatingHeaderAsync()
    {
        using var harness = CreateHarness();
        HandlerTestHarness<RootMessage> handler = harness.AddHandler<RootMessage>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Guid conversationId = NewId.NextGuid();
        var message = new RootMessage(NewId.NextGuid(), "explicit");

        await harness.StartAsync(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.SendAsync(
                message,
                context => context.StartNewConversation(conversationId),
                cancellationToken);
            ConsumeContext<RootMessage> context =
                (await handler.Consumed.SelectAsync(cancellationToken).FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken)).Context;

            Assert.Equal(conversationId, context.ConversationId);
            Assert.Null(context.Headers.Get<Guid>(MessageHeaders.InitiatingConversationId));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    private static InMemoryTestHarness CreateHarness()
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        return new InMemoryTestHarness($"conversation-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
    }

    private sealed record RootMessage(Guid CorrelationId, string Value) : ICorrelatedBy<Guid>;

    private sealed record ParentMessage(Guid CorrelationId) : ICorrelatedBy<Guid>;

    private sealed record ChildMessage(Guid ParentCorrelationId, string Value);
}
