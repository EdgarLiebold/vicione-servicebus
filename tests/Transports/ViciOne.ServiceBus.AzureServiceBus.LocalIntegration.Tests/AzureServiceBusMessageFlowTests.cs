using System.Collections.Concurrent;
using System.Runtime.Serialization;
using global::Azure.Messaging.ServiceBus;
using global::Azure.Messaging.ServiceBus.Administration;
using ViciOne.ServiceBus.AzureServiceBus.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.LocalIntegration.Tests;

public sealed class AzureServiceBusMessageFlowTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-MESSAGE-FLOW", "send-preserves-envelope-provider-properties-and-terminal-count")]
    public async Task Send_PreservesTheCompleteEnvelopeAndProviderPropertiesExactlyOnceAsync()
    {
        AzureServiceBusLocalFixture fixture = AzureServiceBusLocalFixture.Create("flow");
        ServiceBusAdministrationClient admin = fixture.CreateAdministrationClient();
        await using ServiceBusClient client = fixture.CreateClient();
        string queue = fixture.Name("input");
        Guid messageId = NewId.NextGuid();
        Guid correlationId = NewId.NextGuid();
        Guid conversationId = NewId.NextGuid();
        Guid requestId = NewId.NextGuid();
        DateTime sentAtUtc = new(2042, 3, 14, 15, 9, 26, DateTimeKind.Utc);
        Uri sourceAddress = new("sb://localhost/source");
        Uri responseAddress = new("sb://localhost/response");
        Uri faultAddress = new("sb://localhost/fault");
        var observed = Observation<ConsumeContext<FlowMessage>>();
        int entries = 0;
        IBusControl bus = CreateBus(fixture, client, admin, busConfiguration =>
        {
            busConfiguration.ReceiveEndpoint(queue, endpoint =>
            {
                endpoint.ConfigureConsumeTopology = false;
                endpoint.DefaultMessageTimeToLive = EmulatorEntityTimeToLive;
                endpoint.Handler<FlowMessage>(context =>
                {
                    Interlocked.Increment(ref entries);
                    observed.TrySetResult(context);
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
            await endpoint.SendAsync(
                    new FlowMessage("exact-payload", sentAtUtc),
                    context =>
                    {
                        context.MessageId = messageId;
                        context.CorrelationId = correlationId;
                        context.ConversationId = conversationId;
                        context.RequestId = requestId;
                        context.SourceAddress = sourceAddress;
                        context.ResponseAddress = responseAddress;
                        context.FaultAddress = faultAddress;
                        context.TimeToLive = TimeSpan.FromMinutes(4);
                        context.Headers.Set("native-marker", "marker-value");
                        var provider = (ServiceBusSendContext<FlowMessage>)context;
                        provider.ReplyTo = "reply-queue";
                        provider.ReplyToSessionId = "reply-session";
                        provider.Label = "exact-subject";
                    },
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            ConsumeContext<FlowMessage> actual = await observed.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            ServiceBusMessageContext provider = actual.GetPayload<ServiceBusMessageContext>();

            Assert.Equal("exact-payload", actual.Message.Value);
            Assert.Equal(sentAtUtc, actual.Message.SentAtUtc);
            Assert.Equal(DateTimeKind.Utc, actual.Message.SentAtUtc.Kind);
            Assert.Equal(messageId, actual.MessageId);
            Assert.Equal(correlationId, actual.CorrelationId);
            Assert.Equal(conversationId, actual.ConversationId);
            Assert.Equal(requestId, actual.RequestId);
            Assert.Equal(sourceAddress, actual.SourceAddress);
            Assert.Equal(responseAddress, actual.ResponseAddress);
            Assert.Equal(faultAddress, actual.FaultAddress);
            Assert.Equal("marker-value", actual.Headers.Get<string>("native-marker"));
            Assert.False(actual.Advanced().ReceiveContext.Redelivered);
            Assert.Equal("exact-subject", provider.Label);
            Assert.Equal("reply-queue", provider.ReplyTo);
            Assert.Equal("reply-session", provider.ReplyToSessionId);
            Assert.Equal(TimeSpan.FromMinutes(4), provider.TimeToLive);
            Assert.Equal(TimeSpan.Zero, provider.EnqueuedTime.Offset);
            Assert.Equal(1, provider.DeliveryCount);

            await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            started = false;
            QueueRuntimeProperties runtime = await admin.GetQueueRuntimePropertiesAsync(queue, cancellationToken);
            Assert.Equal(1, entries);
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
    [RequirementCoverage("REQ-VSB-ASB-REQUEST-RESPONSE", "queue-request-response-preserves-request-correlation-and-terminal-count")]
    public async Task RequestResponse_PreservesTheRequestCorrelationExactlyOnceAsync()
    {
        AzureServiceBusLocalFixture fixture = AzureServiceBusLocalFixture.Create("request");
        ServiceBusAdministrationClient admin = fixture.CreateAdministrationClient();
        await using ServiceBusClient client = fixture.CreateClient();
        string queue = fixture.Name("service");
        Guid correlationId = NewId.NextGuid();
        var consumed = Observation<ConsumeContext<RequestMessage>>();
        int entries = 0;
        IBusControl bus = CreateBus(fixture, client, admin, busConfiguration =>
        {
            busConfiguration.ReceiveEndpoint(queue, endpoint =>
            {
                endpoint.ConfigureConsumeTopology = false;
                endpoint.DefaultMessageTimeToLive = EmulatorEntityTimeToLive;
                endpoint.Handler<RequestMessage>(async context =>
                {
                    Interlocked.Increment(ref entries);
                    consumed.TrySetResult(context);
                    await context.RespondAsync(new ResponseMessage(context.Message.CorrelationId, "exact-response"));
                });
            });
        });
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            IRequestClient<RequestMessage> requestClient = bus.CreateRequestClient<RequestMessage>(
                new Uri($"queue:{queue}"),
                new RequestTimeout(fixture.OperationTimeout));

            Response<ResponseMessage> response = await requestClient.GetResponseAsync<ResponseMessage>(
                    new RequestMessage(correlationId),
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            ConsumeContext<RequestMessage> request = await consumed.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal(correlationId, request.Message.CorrelationId);
            Assert.NotNull(request.RequestId);
            Assert.Equal(correlationId, request.CorrelationId);
            Assert.Equal(request.RequestId, response.RequestId);
            Assert.Equal(correlationId, response.CorrelationId);
            Assert.Equal(correlationId, response.Message.CorrelationId);
            Assert.Equal("exact-response", response.Message.Value);

            await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            started = false;
            QueueRuntimeProperties runtime = await admin.GetQueueRuntimePropertiesAsync(queue, cancellationToken);
            Assert.Equal(1, entries);
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
    [RequirementCoverage("REQ-VSB-ASB-ERROR-TRANSPORT", "fault-moves-one-complete-envelope-and-publishes-one-correlated-fault")]
    public async Task Fault_MovesOneCompleteEnvelopeAndPublishesOneCorrelatedFaultAsync()
    {
        const string failureMessage = "intentional Azure Service Bus consumer failure";
        AzureServiceBusLocalFixture fixture = AzureServiceBusLocalFixture.Create("error");
        ServiceBusAdministrationClient admin = fixture.CreateAdministrationClient();
        await using ServiceBusClient client = fixture.CreateClient();
        string queue = fixture.Name("input");
        string errorQueue = $"{queue}_error";
        string faultQueue = fixture.Name("faults");
        Guid correlationId = NewId.NextGuid();
        Guid conversationId = NewId.NextGuid();
        Uri sourceAddress = new("sb://localhost/source");
        Uri responseAddress = new("sb://localhost/response");
        Uri faultAddress = new($"sb://localhost/{faultQueue}");
        var moved = Observation<ConsumeContext<ErrorMessage>>();
        var faulted = Observation<ConsumeContext<Fault<ErrorMessage>>>();
        int sourceEntries = 0;
        int movedEntries = 0;
        int faultEntries = 0;
        IBusControl bus = CreateBus(fixture, client, admin, busConfiguration =>
        {
            busConfiguration.ReceiveEndpoint(queue, endpoint =>
            {
                endpoint.ConfigureConsumeTopology = false;
                endpoint.DefaultMessageTimeToLive = EmulatorEntityTimeToLive;
                endpoint.Handler<ErrorMessage>(_ =>
                {
                    Interlocked.Increment(ref sourceEntries);
                    throw new SerializationException(failureMessage);
                });
            });
            busConfiguration.ReceiveEndpoint(errorQueue, endpoint =>
            {
                endpoint.ConfigureConsumeTopology = false;
                endpoint.DefaultMessageTimeToLive = EmulatorEntityTimeToLive;
                endpoint.Handler<ErrorMessage>(context =>
                {
                    Interlocked.Increment(ref movedEntries);
                    moved.TrySetResult(context);
                    return Task.CompletedTask;
                });
            });
            busConfiguration.ReceiveEndpoint(faultQueue, endpoint =>
            {
                endpoint.ConfigureConsumeTopology = false;
                endpoint.DefaultMessageTimeToLive = EmulatorEntityTimeToLive;
                endpoint.Handler<Fault<ErrorMessage>>(context =>
                {
                    Interlocked.Increment(ref faultEntries);
                    faulted.TrySetResult(context);
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
            await endpoint.SendAsync(
                    new ErrorMessage("must-move"),
                    context =>
                    {
                        context.CorrelationId = correlationId;
                        context.ConversationId = conversationId;
                        context.SourceAddress = sourceAddress;
                        context.ResponseAddress = responseAddress;
                        context.FaultAddress = faultAddress;
                    },
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            ConsumeContext<ErrorMessage> movedContext = await moved.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            ConsumeContext<Fault<ErrorMessage>> faultContext = await faulted.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal("must-move", movedContext.Message.Value);
            Assert.Equal(correlationId, movedContext.CorrelationId);
            Assert.Equal(conversationId, movedContext.ConversationId);
            Assert.Equal(sourceAddress, movedContext.SourceAddress);
            Assert.Equal(responseAddress, movedContext.ResponseAddress);
            Assert.Equal(faultAddress, movedContext.FaultAddress);
            Assert.Equal(new Uri($"sb://localhost/{queue}"), movedContext.DestinationAddress);
            Assert.Equal(failureMessage,
                movedContext.Advanced().ReceiveContext.TransportHeaders.Get(MessageHeaders.FaultMessage, default(string)));
            Assert.Equal("fault",
                movedContext.Advanced().ReceiveContext.TransportHeaders.Get(MessageHeaders.Reason, default(string)));
            Assert.Equal(conversationId, faultContext.ConversationId);
            Assert.Equal(correlationId, faultContext.CorrelationId);
            Assert.Equal(failureMessage, Assert.Single(faultContext.Message.Exceptions).Message);

            await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            started = false;
            Assert.Equal((1, 1, 1), (sourceEntries, movedEntries, faultEntries));
            Assert.Equal(0, (await admin.GetQueueRuntimePropertiesAsync(queue, cancellationToken)).Value.ActiveMessageCount);
            Assert.Equal(0, (await admin.GetQueueRuntimePropertiesAsync(errorQueue, cancellationToken)).Value.ActiveMessageCount);
            Assert.Equal(0, (await admin.GetQueueRuntimePropertiesAsync(faultQueue, cancellationToken)).Value.ActiveMessageCount);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            await fixture.CleanupAsync(admin);
        }
    }

    static IBusControl CreateBus(
        AzureServiceBusLocalFixture fixture,
        ServiceBusClient client,
        ServiceBusAdministrationClient administrationClient,
        Action<IServiceBusBusFactoryConfigurator> configure)
    {
        return Bus.Factory.CreateUsingAzureServiceBus(bus =>
        {
            bus.Host(new Uri("sb://localhost/"), client, administrationClient);
            bus.DefaultMessageTimeToLive = EmulatorEntityTimeToLive;
            bus.OverrideDefaultBusEndpointQueueName(fixture.Name("bus"));
            configure(bus);
        });
    }

    static TaskCompletionSource<T> Observation<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    static readonly TimeSpan EmulatorEntityTimeToLive = TimeSpan.FromHours(1);

    public sealed record FlowMessage(string Value, DateTime SentAtUtc);

    public sealed record RequestMessage(Guid CorrelationId);

    public sealed record ResponseMessage(Guid CorrelationId, string Value);

    public sealed record ErrorMessage(string Value);
}
