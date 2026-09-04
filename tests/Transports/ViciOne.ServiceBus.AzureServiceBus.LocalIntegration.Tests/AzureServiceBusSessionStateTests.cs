using global::Azure.Messaging.ServiceBus;
using global::Azure.Messaging.ServiceBus.Administration;
using ViciOne.ServiceBus.AzureServiceBus;
using ViciOne.ServiceBus.AzureServiceBus.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.LocalIntegration.Tests;

public sealed class AzureServiceBusSessionStateTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-SESSION-FLOW", "session-identity-flows-through-receive-publish-and-send")]
    public async Task SessionIdentity_FlowsThroughReceivePublishAndSendExactlyOnceAsync()
    {
        AzureServiceBusLocalFixture fixture = AzureServiceBusLocalFixture.Create("session-flow");
        ServiceBusAdministrationClient admin = fixture.CreateAdministrationClient();
        await using ServiceBusClient client = fixture.CreateClient();
        string queue = fixture.Name("input");
        string publishedSubscription = $"vsb-{Guid.NewGuid():N}";
        string sessionId = NewId.NextGuid().ToString("D");
        var receivedStart = Observation<ConsumeContext<SessionStart>>();
        var receivedPublished = Observation<ConsumeContext<SessionPublished>>();
        var receivedSent = Observation<ConsumeContext<SessionSent>>();
        int startCount = 0;
        int publishedCount = 0;
        int sentCount = 0;
        IBusControl bus = Bus.Factory.CreateUsingAzureServiceBus(configuration =>
        {
            configuration.Host(new Uri("sb://localhost/"), client, admin);
            configuration.DefaultMessageTimeToLive = EmulatorEntityTimeToLive;
            configuration.OverrideDefaultBusEndpointQueueName(fixture.Name("bus"));
            configuration.Publish<SessionPublished>(topology => topology.DefaultMessageTimeToLive = EmulatorEntityTimeToLive);
            configuration.ReceiveEndpoint(queue, endpoint =>
            {
                endpoint.ConfigureConsumeTopology = false;
                endpoint.DefaultMessageTimeToLive = EmulatorEntityTimeToLive;
                endpoint.RequiresSession = true;
                endpoint.MaxConcurrentCallsPerSession = 1;
                endpoint.Subscribe<SessionPublished>(publishedSubscription, subscription =>
                    subscription.DefaultMessageTimeToLive = EmulatorEntityTimeToLive);
                endpoint.Handler<SessionStart>(async context =>
                {
                    Interlocked.Increment(ref startCount);
                    receivedStart.TrySetResult(context);
                    await context.Advanced().PublishAsync(new SessionPublished(context.Message.CorrelationId), context.CancellationToken);
                    await context.Advanced().SendAsync(new Uri($"queue:{queue}"), new SessionSent(context.Message.CorrelationId));
                });
                endpoint.Handler<SessionPublished>(context =>
                {
                    Interlocked.Increment(ref publishedCount);
                    receivedPublished.TrySetResult(context);
                    return Task.CompletedTask;
                });
                endpoint.Handler<SessionSent>(context =>
                {
                    Interlocked.Increment(ref sentCount);
                    receivedSent.TrySetResult(context);
                    return Task.CompletedTask;
                });
            });
        });
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            ISendEndpoint endpoint = await bus.GetSendEndpointAsync(new Uri($"queue:{queue}"), TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            Guid correlationId = NewId.NextGuid();
            await endpoint.SendAsync(
                    new SessionStart(correlationId),
                    context => ((ServiceBusSendContext<SessionStart>)context).SessionId = sessionId,
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            ConsumeContext<SessionStart> start = await receivedStart.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            ConsumeContext<SessionPublished> published = await receivedPublished.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            ConsumeContext<SessionSent> sent = await receivedSent.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal(sessionId, start.GetPayload<ServiceBusMessageContext>().SessionId);
            Assert.Equal(sessionId, published.GetPayload<ServiceBusMessageContext>().SessionId);
            Assert.Equal(sessionId, sent.GetPayload<ServiceBusMessageContext>().SessionId);
            Assert.False(start.Advanced().ReceiveContext.Redelivered);
            Assert.False(published.Advanced().ReceiveContext.Redelivered);
            Assert.False(sent.Advanced().ReceiveContext.Redelivered);
            Assert.Equal(correlationId, published.Message.CorrelationId);
            Assert.Equal(correlationId, sent.Message.CorrelationId);

            await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            started = false;
            Assert.Equal((1, 1, 1), (startCount, publishedCount, sentCount));
            QueueRuntimeProperties runtime = await admin.GetQueueRuntimePropertiesAsync(queue, cancellationToken);
            Assert.Equal(0, runtime.ActiveMessageCount);
            Assert.Equal(0, runtime.DeadLetterMessageCount);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            await fixture.CleanupAsync(admin);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-SESSION-SAGA", "session-state-repository-loads-the-same-saga-on-the-next-message")]
    public async Task MessageSessionSagaRepository_LoadsTheCommittedStateOnTheNextMessageAsync()
    {
        AzureServiceBusLocalFixture fixture = AzureServiceBusLocalFixture.Create("session-saga");
        ServiceBusAdministrationClient admin = fixture.CreateAdministrationClient();
        await using ServiceBusClient client = fixture.CreateClient();
        string sagaQueue = fixture.Name("saga");
        string resultQueue = fixture.Name("results");
        string resultSubscription = $"vsb-{Guid.NewGuid():N}";
        string sessionId = NewId.NextGuid().ToString("D");
        Guid correlationId = Guid.Parse(sessionId);
        var created = Observation<ConsumeContext<SagaCreated>>();
        var advanced = Observation<ConsumeContext<SagaAdvanced>>();
        ISagaRepository<SessionSaga> repository = MessageSessionSagaRepository.Create<SessionSaga>();
        IBusControl bus = Bus.Factory.CreateUsingAzureServiceBus(configuration =>
        {
            configuration.Host(new Uri("sb://localhost/"), client, admin);
            configuration.DefaultMessageTimeToLive = EmulatorEntityTimeToLive;
            configuration.OverrideDefaultBusEndpointQueueName(fixture.Name("bus"));
            configuration.Publish<SagaCreated>(topology => topology.DefaultMessageTimeToLive = EmulatorEntityTimeToLive);
            configuration.Publish<SagaAdvanced>(topology => topology.DefaultMessageTimeToLive = EmulatorEntityTimeToLive);
            configuration.ReceiveEndpoint(sagaQueue, endpoint =>
            {
                endpoint.ConfigureConsumeTopology = false;
                endpoint.DefaultMessageTimeToLive = EmulatorEntityTimeToLive;
                endpoint.RequiresSession = true;
                endpoint.MaxConcurrentCallsPerSession = 1;
                endpoint.UseInMemoryOutbox();
                endpoint.Saga(repository);
            });
            configuration.ReceiveEndpoint(resultQueue, endpoint =>
            {
                endpoint.ConfigureConsumeTopology = false;
                endpoint.DefaultMessageTimeToLive = EmulatorEntityTimeToLive;
                endpoint.Subscribe<SagaCreated>(resultSubscription, subscription =>
                    subscription.DefaultMessageTimeToLive = EmulatorEntityTimeToLive);
                endpoint.Subscribe<SagaAdvanced>(resultSubscription, subscription =>
                    subscription.DefaultMessageTimeToLive = EmulatorEntityTimeToLive);
                endpoint.Handler<SagaCreated>(context =>
                {
                    created.TrySetResult(context);
                    return Task.CompletedTask;
                });
                endpoint.Handler<SagaAdvanced>(context =>
                {
                    advanced.TrySetResult(context);
                    return Task.CompletedTask;
                });
            });
        });
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            ISendEndpoint endpoint = await bus.GetSendEndpointAsync(new Uri($"queue:{sagaQueue}"), TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);

            await endpoint.SendAsync(
                    new CreateSessionSaga(correlationId, "created-state"),
                    context => ((ServiceBusSendContext<CreateSessionSaga>)context).SessionId = sessionId,
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            SagaCreated first = (await created.Task.WaitAsync(fixture.OperationTimeout, cancellationToken)).Message;
            Assert.Equal(new SagaCreated(correlationId, "created-state", 1), first);

            await endpoint.SendAsync(
                    new AdvanceSessionSaga(correlationId, "next-state"),
                    context => ((ServiceBusSendContext<AdvanceSessionSaga>)context).SessionId = sessionId,
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            SagaAdvanced second = (await advanced.Task.WaitAsync(fixture.OperationTimeout, cancellationToken)).Message;
            Assert.Equal(new SagaAdvanced(correlationId, "created-state", "next-state", 2), second);

            await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            started = false;
            Assert.Equal(0, (await admin.GetQueueRuntimePropertiesAsync(sagaQueue, cancellationToken)).Value.ActiveMessageCount);
            Assert.Equal(0, (await admin.GetQueueRuntimePropertiesAsync(resultQueue, cancellationToken)).Value.ActiveMessageCount);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            await fixture.CleanupAsync(admin);
        }
    }

    static TaskCompletionSource<T> Observation<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    static readonly TimeSpan EmulatorEntityTimeToLive = TimeSpan.FromHours(1);

    public sealed record SessionStart(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record SessionPublished(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record SessionSent(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record CreateSessionSaga(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;

    public sealed record AdvanceSessionSaga(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;

    public sealed record SagaCreated(Guid CorrelationId, string OriginalValue, int Revision) : CorrelatedBy<Guid>;

    public sealed record SagaAdvanced(Guid CorrelationId, string OriginalValue, string NewValue, int Revision) : CorrelatedBy<Guid>;

    public sealed class SessionSaga :
        ISaga,
        InitiatedBy<CreateSessionSaga>,
        Orchestrates<AdvanceSessionSaga>
    {
        public Guid CorrelationId { get; set; }

        public string OriginalValue { get; set; } = "";

        public int Revision { get; set; }

        public Task ConsumeAsync(ConsumeContext<CreateSessionSaga> context)
        {
            OriginalValue = context.Message.Value;
            Revision = 1;
            return context.Advanced().PublishAsync(new SagaCreated(CorrelationId, OriginalValue, Revision), context.CancellationToken);
        }

        public Task ConsumeAsync(ConsumeContext<AdvanceSessionSaga> context)
        {
            if (OriginalValue.Length == 0 || Revision != 1)
                throw new InvalidOperationException("The session repository did not load the state committed by the first delivery.");

            Revision++;
            return context.Advanced().PublishAsync(
                new SagaAdvanced(CorrelationId, OriginalValue, context.Message.Value, Revision),
                context.CancellationToken);
        }
    }
}
