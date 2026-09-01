namespace ViciOne.ServiceBus.AzureServiceBusTransport.LocalIntegration.Tests;

using System.Collections.Concurrent;
using global::Azure.Messaging.ServiceBus;
using global::Azure.Messaging.ServiceBus.Administration;
using Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class AzureServiceBusSessionAndSchedulingTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-SESSIONS", "multiple-sessions-preserve-identity-order-and-terminal-count")]
    public async Task MultipleSessions_PreserveIdentityOrderAndTerminalCount()
    {
        const int sessionCount = 3;
        const int messagesPerSession = 4;
        AzureServiceBusLocalFixture fixture = AzureServiceBusLocalFixture.Create("sessions");
        ServiceBusAdministrationClient admin = fixture.CreateAdministrationClient();
        await using ServiceBusClient client = fixture.CreateClient();
        string queue = fixture.Name("input");
        string[] sessionIds = Enumerable.Range(0, sessionCount).Select(index => $"session-{index}").ToArray();
        var received = new ConcurrentDictionary<string, ConcurrentQueue<int>>();
        var completed = Observation<bool>();
        int total = 0;
        IBusControl bus = Bus.Factory.CreateUsingAzureServiceBus(configuration =>
        {
            configuration.Host(new Uri("sb://localhost/"), client, admin);
            configuration.DefaultMessageTimeToLive = EmulatorEntityTimeToLive;
            configuration.OverrideDefaultBusEndpointQueueName(fixture.Name("bus"));
            configuration.ReceiveEndpoint(queue, endpoint =>
            {
                endpoint.ConfigureConsumeTopology = false;
                endpoint.DefaultMessageTimeToLive = EmulatorEntityTimeToLive;
                endpoint.RequiresSession = true;
                endpoint.MaxConcurrentSessions = sessionCount;
                endpoint.MaxConcurrentCallsPerSession = 1;
                endpoint.Handler<SessionMessage>(context =>
                {
                    string sessionId = context.GetPayload<ServiceBusMessageContext>().SessionId;
                    received.GetOrAdd(sessionId, _ => new ConcurrentQueue<int>()).Enqueue(context.Message.Sequence);
                    if (Interlocked.Increment(ref total) == sessionCount * messagesPerSession)
                        completed.TrySetResult(true);
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
            ISendEndpoint endpoint = await bus.GetSendEndpoint(new Uri($"queue:{queue}"))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await Task.WhenAll(sessionIds.Select(sessionId => SendSession(
                    endpoint,
                    sessionId,
                    messagesPerSession,
                    fixture.OperationTimeout,
                    cancellationToken)))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.True(await completed.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));

            await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            started = false;
            Assert.Equal(sessionCount * messagesPerSession, total);
            Assert.Equal(sessionIds.Order(), received.Keys.Order());
            foreach (string sessionId in sessionIds)
                Assert.Equal(Enumerable.Range(0, messagesPerSession), received[sessionId]);
            Assert.Equal(0, (await admin.GetQueueRuntimePropertiesAsync(queue, cancellationToken)).Value.ActiveMessageCount);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            await fixture.CleanupAsync(admin);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-SCHEDULING", "provider-owned-schedule-delivery-and-cancel-acceptance")]
    public async Task ProviderScheduling_ProvesAcceptedStateDeliveryAndCancelAcceptance()
    {
        AzureServiceBusLocalFixture fixture = AzureServiceBusLocalFixture.Create("scheduling");
        ServiceBusAdministrationClient admin = fixture.CreateAdministrationClient();
        await using ServiceBusClient client = fixture.CreateClient();
        string queue = fixture.Name("input");
        Guid deliveredId = NewId.NextGuid();
        Guid cancelledId = NewId.NextGuid();
        var delivered = Observation<ConsumeContext<ScheduledDelivery>>();
        int entries = 0;
        IBusControl bus = Bus.Factory.CreateUsingAzureServiceBus(configuration =>
        {
            configuration.Host(new Uri("sb://localhost/"), client, admin);
            configuration.DefaultMessageTimeToLive = EmulatorEntityTimeToLive;
            configuration.OverrideDefaultBusEndpointQueueName(fixture.Name("bus"));
            configuration.UseServiceBusMessageScheduler();
            configuration.ReceiveEndpoint(queue, endpoint =>
            {
                endpoint.ConfigureConsumeTopology = false;
                endpoint.DefaultMessageTimeToLive = EmulatorEntityTimeToLive;
                endpoint.Handler<ScheduledDelivery>(context =>
                {
                    Interlocked.Increment(ref entries);
                    delivered.TrySetResult(context);
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
            IMessageScheduler scheduler = bus.CreateServiceBusMessageScheduler();
            Uri destination = new($"queue:{queue}");
            DateTime cancellationDueTime = TimeProvider.System.GetUtcNow().UtcDateTime.AddHours(1);
            ScheduledMessage<ScheduledDelivery> cancelled = await scheduler.ScheduleSend(
                    destination,
                    cancellationDueTime,
                    new ScheduledDelivery(cancelledId),
                    Pipe.Execute<SendContext<ScheduledDelivery>>(context => context.MessageId = cancelledId),
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await using ServiceBusReceiver providerReceiver = client.CreateReceiver(queue);
            IReadOnlyList<ServiceBusReceivedMessage> scheduled = await providerReceiver.PeekMessagesAsync(
                10,
                cancellationToken: cancellationToken);
            ServiceBusReceivedMessage providerScheduled = Assert.Single(
                scheduled,
                message => message.MessageId == cancelledId.ToString("N"));
            Assert.Equal(ServiceBusMessageState.Scheduled, providerScheduled.State);
            Assert.Equal(
                providerScheduled.SequenceNumber,
                BitConverter.ToInt64(cancelled.TokenId.ToByteArray(), 0));
            Assert.Equal(
                BitConverter.ToUInt64(new Guid("E25FC12B-FF28-4476-A6E1-DE45E154A675").ToByteArray(), 8),
                BitConverter.ToUInt64(cancelled.TokenId.ToByteArray(), 8));
            await scheduler.CancelScheduledSend(destination, cancelled.TokenId, cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.Empty(await providerReceiver.PeekMessagesAsync(10, cancellationToken: cancellationToken));

            DateTime scheduledTime = TimeProvider.System.GetUtcNow().UtcDateTime.AddSeconds(5);
            ScheduledMessage<ScheduledDelivery> accepted = await scheduler.ScheduleSend(
                    destination,
                    scheduledTime,
                    new ScheduledDelivery(deliveredId),
                    Pipe.Execute<SendContext<ScheduledDelivery>>(context =>
                        ((ServiceBusSendContext<ScheduledDelivery>)context).PartitionKey = "2112"),
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.Equal(scheduledTime, accepted.ScheduledTime);
            ConsumeContext<ScheduledDelivery> actual = await delivered.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.Equal(deliveredId, actual.Message.Id);
            Assert.Equal("2112", actual.GetPayload<ServiceBusMessageContext>().PartitionKey);

            await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            started = false;
            QueueRuntimeProperties terminal = await admin.GetQueueRuntimePropertiesAsync(queue, cancellationToken);
            Assert.Equal(1, entries);
            Assert.Equal(0, terminal.ActiveMessageCount);
            Assert.Equal(0, terminal.ScheduledMessageCount);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            await fixture.CleanupAsync(admin);
        }
    }

    static async Task SendSession(
        ISendEndpoint endpoint,
        string sessionId,
        int count,
        TimeSpan operationTimeout,
        CancellationToken cancellationToken)
    {
        for (int sequence = 0; sequence < count; sequence++)
        {
            await endpoint.Send(
                    new SessionMessage(sessionId, sequence),
                    context => ((ServiceBusSendContext<SessionMessage>)context).SessionId = sessionId,
                    cancellationToken)
                .WaitAsync(operationTimeout, cancellationToken);
        }
    }

    static TaskCompletionSource<T> Observation<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    static readonly TimeSpan EmulatorEntityTimeToLive = TimeSpan.FromHours(1);

    public sealed record SessionMessage(string SessionId, int Sequence);

    public sealed record ScheduledDelivery(Guid Id);
}
