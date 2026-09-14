using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Topology.Configuration;

public sealed class CorrelationIdConventionTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CORRELATION-ID", "global-selector-overloads-reject-null")]
    public void GlobalSelectors_RejectMissingRequiredAndOptionalSelectors()
    {
        Assert.Equal("getCorrelationId", Assert.Throws<ArgumentNullException>(() =>
            MessageCorrelation.UseCorrelationId<RequiredSelectorMessage>((Func<RequiredSelectorMessage, Guid>)null!)).ParamName);
        Assert.Equal("getCorrelationId", Assert.Throws<ArgumentNullException>(() =>
            MessageCorrelation.UseCorrelationId<OptionalSelectorMessage>((Func<OptionalSelectorMessage, Guid?>)null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CORRELATION-ID", "optional-global-selector-preserves-present-and-absent-identities")]
    public async Task OptionalGlobalSelector_PreservesPresentAndAbsentCorrelationIdentitiesAsync()
    {
        using var harness = CreateHarness();
        HandlerTestHarness<OptionalGlobalSelectorMessage> handler = harness.AddHandler<OptionalGlobalSelectorMessage>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Guid selectedCorrelationId = NewId.NextGuid();
        var selected = new OptionalGlobalSelectorMessage(selectedCorrelationId, "selected");
        var absent = new OptionalGlobalSelectorMessage(null, "absent");

        await harness.StartAsync(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.SendAsync(selected, cancellationToken);
            await harness.InputQueueSendEndpoint.SendAsync(absent, cancellationToken);

            ConsumeContext<OptionalGlobalSelectorMessage> selectedContext = (await handler.Consumed
                .SelectAsync(observation => observation.Context.Message.Label == selected.Label, cancellationToken)
                .FirstObservedAsync(cancellationToken: cancellationToken)).Context;
            ConsumeContext<OptionalGlobalSelectorMessage> absentContext = (await handler.Consumed
                .SelectAsync(observation => observation.Context.Message.Label == absent.Label, cancellationToken)
                .FirstObservedAsync(cancellationToken: cancellationToken)).Context;

            Assert.Equal(selectedCorrelationId, selectedContext.CorrelationId);
            Assert.Null(absentContext.CorrelationId);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CORRELATION-ID", "correlated-contract-send-and-publish")]
    public async Task CorrelatedByGuid_DrivesBothSendAndPublishAsync()
    {
        using var harness = CreateHarness();
        HandlerTestHarness<CorrelatedMessage> handler = harness.AddHandler<CorrelatedMessage>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var sent = new CorrelatedMessage(NewId.NextGuid(), "sent");
        var published = new CorrelatedMessage(NewId.NextGuid(), "published");

        await harness.StartAsync(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.SendAsync(sent, cancellationToken);
            await harness.Bus.PublishAsync(published, cancellationToken);

            ConsumeContext<CorrelatedMessage> sentContext = (await handler.Consumed
                .SelectAsync(observation => observation.Context.Message.CorrelationId == sent.CorrelationId, cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken)).Context;
            ConsumeContext<CorrelatedMessage> publishedContext = (await handler.Consumed
                .SelectAsync(observation => observation.Context.Message.CorrelationId == published.CorrelationId, cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken)).Context;

            Assert.Equal(sent.CorrelationId, sentContext.CorrelationId);
            Assert.Equal(published.CorrelationId, publishedContext.CorrelationId);
            Assert.NotEqual(sentContext.CorrelationId, publishedContext.CorrelationId);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CORRELATION-ID", "named-guid-property-precedence")]
    public async Task NamedGuidProperties_FollowCorrelationThenEventThenCommandPrecedenceAsync()
    {
        using var harness = CreateHarness();
        HandlerTestHarness<CorrelationEventCommandMessage> correlationHandler =
            harness.AddHandler<CorrelationEventCommandMessage>();
        HandlerTestHarness<EventCommandMessage> eventHandler = harness.AddHandler<EventCommandMessage>();
        HandlerTestHarness<CommandMessage> commandHandler = harness.AddHandler<CommandMessage>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var correlationMessage = new CorrelationEventCommandMessage(
            NewId.NextGuid(), NewId.NextGuid(), NewId.NextGuid());
        var eventMessage = new EventCommandMessage(NewId.NextGuid(), NewId.NextGuid());
        var commandMessage = new CommandMessage(NewId.NextGuid());

        await harness.StartAsync(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.SendAsync(correlationMessage, cancellationToken);
            await harness.InputQueueSendEndpoint.SendAsync(eventMessage, cancellationToken);
            await harness.InputQueueSendEndpoint.SendAsync(commandMessage, cancellationToken);

            Assert.Equal(correlationMessage.CorrelationId,
                (await correlationHandler.Consumed.SelectAsync(cancellationToken).FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken)).Context.CorrelationId);
            Assert.Equal(eventMessage.EventId,
                (await eventHandler.Consumed.SelectAsync(cancellationToken).FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken)).Context.CorrelationId);
            Assert.Equal(commandMessage.CommandId,
                (await commandHandler.Consumed.SelectAsync(cancellationToken).FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken)).Context.CorrelationId);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CORRELATION-ID", "nullable-class-and-interface-property")]
    public async Task NullableCorrelationProperty_IsReadFromAClassAndAnImplementedInterfaceAsync()
    {
        using var harness = CreateHarness();
        HandlerTestHarness<NullableCorrelationMessage> classHandler = harness.AddHandler<NullableCorrelationMessage>();
        HandlerTestHarness<InterfaceCorrelationMessage> interfaceHandler = harness.AddHandler<InterfaceCorrelationMessage>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var classMessage = new NullableCorrelationMessage(NewId.NextGuid());
        var interfaceMessage = new InterfaceCorrelationMessage(NewId.NextGuid());

        await harness.StartAsync(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.SendAsync(classMessage, cancellationToken);
            await harness.InputQueueSendEndpoint.SendAsync(interfaceMessage, cancellationToken);

            Assert.Equal(classMessage.CorrelationId,
                (await classHandler.Consumed.SelectAsync(cancellationToken).FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken)).Context.CorrelationId);
            Assert.Equal(interfaceMessage.CorrelationId,
                (await interfaceHandler.Consumed.SelectAsync(cancellationToken).FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken)).Context.CorrelationId);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CORRELATION-ID", "empty-selected-contract-does-not-fall-through")]
    public async Task EmptySelectedCorrelationContract_DoesNotSwitchIdentityKindsPerMessageInstanceAsync()
    {
        using var harness = CreateHarness();
        HandlerTestHarness<CorrelationEventCommandMessage> handler = harness.AddHandler<CorrelationEventCommandMessage>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var message = new CorrelationEventCommandMessage(Guid.Empty, NewId.NextGuid(), NewId.NextGuid());

        await harness.StartAsync(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.SendAsync(message, cancellationToken);
            ConsumeContext<CorrelationEventCommandMessage> context =
                (await handler.Consumed.SelectAsync(cancellationToken).FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken)).Context;

            Assert.Null(context.CorrelationId);
            Assert.NotEqual(message.EventId, context.CorrelationId);
            Assert.NotEqual(message.CommandId, context.CorrelationId);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CORRELATION-ID", "explicit-topology-selector-precedence")]
    public async Task ExplicitMessageTopologySelector_PrecedesEveryBuiltInConventionAsync()
    {
        using var harness = CreateHarness();
        harness.InMemoryBusConfiguring += configurator =>
            configurator.Send<ExplicitSelectorMessage>(topology =>
                topology.UseCorrelationId(message => message.TransactionId));
        HandlerTestHarness<ExplicitSelectorMessage> handler = harness.AddHandler<ExplicitSelectorMessage>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var message = new ExplicitSelectorMessage(NewId.NextGuid(), NewId.NextGuid());

        await harness.StartAsync(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.SendAsync(message, cancellationToken);
            ConsumeContext<ExplicitSelectorMessage> context =
                (await handler.Consumed.SelectAsync(cancellationToken).FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken)).Context;

            Assert.Equal(message.TransactionId, context.CorrelationId);
            Assert.NotEqual(message.CorrelationId, context.CorrelationId);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CORRELATION-ID", "caller-override-precedence")]
    public async Task ExplicitSendContextValue_IsAppliedAfterTheMessageConventionAsync()
    {
        using var harness = CreateHarness();
        HandlerTestHarness<CommandMessage> handler = harness.AddHandler<CommandMessage>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var message = new CommandMessage(NewId.NextGuid());
        Guid explicitCorrelationId = NewId.NextGuid();

        await harness.StartAsync(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.SendAsync(
                message,
                context => context.CorrelationId = explicitCorrelationId,
                cancellationToken);
            ConsumeContext<CommandMessage> context =
                (await handler.Consumed.SelectAsync(cancellationToken).FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken)).Context;

            Assert.Equal(explicitCorrelationId, context.CorrelationId);
            Assert.NotEqual(message.CommandId, context.CorrelationId);
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
        return new InMemoryTestHarness($"correlation-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
    }

    private sealed record CorrelatedMessage(Guid CorrelationId, string Value) : ICorrelatedBy<Guid>;

    private sealed record RequiredSelectorMessage;

    private sealed record OptionalSelectorMessage;

    public sealed record OptionalGlobalSelectorMessage(Guid? SelectedCorrelationId, string Label);

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
