using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Topology.Configuration;

public sealed class CorrelationIdConventionTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CORRELATION-ID", "correlated-contract-send-and-publish")]
    public async Task CorrelatedByGuid_DrivesBothSendAndPublish()
    {
        using var harness = CreateHarness();
        HandlerTestHarness<CorrelatedMessage> handler = harness.Handler<CorrelatedMessage>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var sent = new CorrelatedMessage(NewId.NextGuid(), "sent");
        var published = new CorrelatedMessage(NewId.NextGuid(), "published");

        await harness.Start(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.Send(sent, cancellationToken);
            await harness.Bus.Publish(published, cancellationToken);

            ConsumeContext<CorrelatedMessage> sentContext = (await handler.Consumed
                .SelectAsync(observation => observation.Context.Message.CorrelationId == sent.CorrelationId, cancellationToken)
                .First()).Context;
            ConsumeContext<CorrelatedMessage> publishedContext = (await handler.Consumed
                .SelectAsync(observation => observation.Context.Message.CorrelationId == published.CorrelationId, cancellationToken)
                .First()).Context;

            Assert.Equal(sent.CorrelationId, sentContext.CorrelationId);
            Assert.Equal(published.CorrelationId, publishedContext.CorrelationId);
            Assert.NotEqual(sentContext.CorrelationId, publishedContext.CorrelationId);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CORRELATION-ID", "named-guid-property-precedence")]
    public async Task NamedGuidProperties_FollowCorrelationThenEventThenCommandPrecedence()
    {
        using var harness = CreateHarness();
        HandlerTestHarness<CorrelationEventCommandMessage> correlationHandler =
            harness.Handler<CorrelationEventCommandMessage>();
        HandlerTestHarness<EventCommandMessage> eventHandler = harness.Handler<EventCommandMessage>();
        HandlerTestHarness<CommandMessage> commandHandler = harness.Handler<CommandMessage>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var correlationMessage = new CorrelationEventCommandMessage(
            NewId.NextGuid(), NewId.NextGuid(), NewId.NextGuid());
        var eventMessage = new EventCommandMessage(NewId.NextGuid(), NewId.NextGuid());
        var commandMessage = new CommandMessage(NewId.NextGuid());

        await harness.Start(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.Send(correlationMessage, cancellationToken);
            await harness.InputQueueSendEndpoint.Send(eventMessage, cancellationToken);
            await harness.InputQueueSendEndpoint.Send(commandMessage, cancellationToken);

            Assert.Equal(correlationMessage.CorrelationId,
                (await correlationHandler.Consumed.SelectAsync(cancellationToken).First()).Context.CorrelationId);
            Assert.Equal(eventMessage.EventId,
                (await eventHandler.Consumed.SelectAsync(cancellationToken).First()).Context.CorrelationId);
            Assert.Equal(commandMessage.CommandId,
                (await commandHandler.Consumed.SelectAsync(cancellationToken).First()).Context.CorrelationId);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CORRELATION-ID", "nullable-class-and-interface-property")]
    public async Task NullableCorrelationProperty_IsReadFromAClassAndAnImplementedInterface()
    {
        using var harness = CreateHarness();
        HandlerTestHarness<NullableCorrelationMessage> classHandler = harness.Handler<NullableCorrelationMessage>();
        HandlerTestHarness<InterfaceCorrelationMessage> interfaceHandler = harness.Handler<InterfaceCorrelationMessage>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var classMessage = new NullableCorrelationMessage(NewId.NextGuid());
        var interfaceMessage = new InterfaceCorrelationMessage(NewId.NextGuid());

        await harness.Start(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.Send(classMessage, cancellationToken);
            await harness.InputQueueSendEndpoint.Send(interfaceMessage, cancellationToken);

            Assert.Equal(classMessage.CorrelationId,
                (await classHandler.Consumed.SelectAsync(cancellationToken).First()).Context.CorrelationId);
            Assert.Equal(interfaceMessage.CorrelationId,
                (await interfaceHandler.Consumed.SelectAsync(cancellationToken).First()).Context.CorrelationId);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CORRELATION-ID", "empty-selected-contract-does-not-fall-through")]
    public async Task EmptySelectedCorrelationContract_DoesNotSwitchIdentityKindsPerMessageInstance()
    {
        using var harness = CreateHarness();
        HandlerTestHarness<CorrelationEventCommandMessage> handler = harness.Handler<CorrelationEventCommandMessage>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var message = new CorrelationEventCommandMessage(Guid.Empty, NewId.NextGuid(), NewId.NextGuid());

        await harness.Start(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.Send(message, cancellationToken);
            ConsumeContext<CorrelationEventCommandMessage> context =
                (await handler.Consumed.SelectAsync(cancellationToken).First()).Context;

            Assert.Null(context.CorrelationId);
            Assert.NotEqual(message.EventId, context.CorrelationId);
            Assert.NotEqual(message.CommandId, context.CorrelationId);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CORRELATION-ID", "explicit-topology-selector-precedence")]
    public async Task ExplicitMessageTopologySelector_PrecedesEveryBuiltInConvention()
    {
        using var harness = CreateHarness();
        harness.OnConfigureInMemoryBus += configurator =>
            configurator.Send<ExplicitSelectorMessage>(topology =>
                topology.UseCorrelationId(message => message.TransactionId));
        HandlerTestHarness<ExplicitSelectorMessage> handler = harness.Handler<ExplicitSelectorMessage>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var message = new ExplicitSelectorMessage(NewId.NextGuid(), NewId.NextGuid());

        await harness.Start(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.Send(message, cancellationToken);
            ConsumeContext<ExplicitSelectorMessage> context =
                (await handler.Consumed.SelectAsync(cancellationToken).First()).Context;

            Assert.Equal(message.TransactionId, context.CorrelationId);
            Assert.NotEqual(message.CorrelationId, context.CorrelationId);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CORRELATION-ID", "caller-override-precedence")]
    public async Task ExplicitSendContextValue_IsAppliedAfterTheMessageConvention()
    {
        using var harness = CreateHarness();
        HandlerTestHarness<CommandMessage> handler = harness.Handler<CommandMessage>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var message = new CommandMessage(NewId.NextGuid());
        Guid explicitCorrelationId = NewId.NextGuid();

        await harness.Start(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.Send(
                message,
                context => context.CorrelationId = explicitCorrelationId,
                cancellationToken);
            ConsumeContext<CommandMessage> context =
                (await handler.Consumed.SelectAsync(cancellationToken).First()).Context;

            Assert.Equal(explicitCorrelationId, context.CorrelationId);
            Assert.NotEqual(message.CommandId, context.CorrelationId);
        }
        finally
        {
            await harness.Stop();
        }
    }

    private static InMemoryTestHarness CreateHarness()
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        return new InMemoryTestHarness($"correlation-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
    }

    private sealed record CorrelatedMessage(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;

    private sealed record CorrelationEventCommandMessage(Guid CorrelationId, Guid EventId, Guid CommandId);

    private sealed record EventCommandMessage(Guid EventId, Guid CommandId);

    private sealed record CommandMessage(Guid CommandId);

    private sealed record NullableCorrelationMessage(Guid? CorrelationId);

    private interface InterfaceCorrelationContract
    {
        Guid? CorrelationId { get; }
    }

    private sealed record InterfaceCorrelationMessage(Guid? CorrelationId) : InterfaceCorrelationContract;

    private sealed record ExplicitSelectorMessage(Guid CorrelationId, Guid TransactionId);
}
