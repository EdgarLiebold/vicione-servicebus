using global::Azure.Messaging.ServiceBus;
using global::Azure.Messaging.ServiceBus.Administration;
using ViciOne.ServiceBus.Azure.ServiceBus.Core.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Azure.ServiceBus.Core.LocalIntegration.Tests;

public sealed class AzureServiceBusDeadLetterTransportTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-NATIVE-DEAD-LETTER", "skipped-message-uses-native-dead-letter-subqueue")]
    public async Task SkippedMessage_UsesNativeDeadLetterQueueWithoutCreatingSkippedQueueAsync()
    {
        AzureServiceBusLocalFixture fixture = AzureServiceBusLocalFixture.Create("skipped-dlq");
        ServiceBusAdministrationClient admin = fixture.CreateAdministrationClient();
        await using ServiceBusClient client = fixture.CreateClient();
        string queue = fixture.Name("input");
        Guid messageId = NewId.NextGuid();
        IBusControl bus = CreateBus(fixture, client, admin, queue, endpoint =>
            endpoint.ConfigureDeadLetterQueueDeadLetterTransport());

        DeadLetterResult result = await SendAndReceiveDeadLetterAsync(
            fixture,
            client,
            admin,
            bus,
            queue,
            $"{queue}_skipped",
            new SkippedMessage("skipped-body"),
            messageId,
            TestContext.Current.CancellationToken);

        ServiceBusReceivedMessage deadLettered = result.Message;
        Assert.Equal(messageId.ToString("N"), deadLettered.MessageId);
        Assert.Equal("dead-letter", deadLettered.ApplicationProperties[MessageHeaders.Reason]);
        Assert.False(result.AlternativeQueueExisted);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-NATIVE-DEAD-LETTER", "faulted-message-uses-native-dead-letter-subqueue-with-fault-headers")]
    public async Task FaultedMessage_UsesNativeDeadLetterQueueWithExactFaultHeadersAsync()
    {
        const string failureMessage = "native-dead-letter-consumer-failure";
        AzureServiceBusLocalFixture fixture = AzureServiceBusLocalFixture.Create("fault-dlq");
        ServiceBusAdministrationClient admin = fixture.CreateAdministrationClient();
        await using ServiceBusClient client = fixture.CreateClient();
        string queue = fixture.Name("input");
        Guid messageId = NewId.NextGuid();
        int attempts = 0;
        IBusControl bus = CreateBus(fixture, client, admin, queue, endpoint =>
        {
            endpoint.ConfigureDeadLetterQueueErrorTransport();
            endpoint.Handler<FaultedMessage>(_ =>
            {
                Interlocked.Increment(ref attempts);
                throw new NativeDeadLetterException(failureMessage);
            });
        });

        DeadLetterResult result = await SendAndReceiveDeadLetterAsync(
            fixture,
            client,
            admin,
            bus,
            queue,
            $"{queue}_error",
            new FaultedMessage("faulted-body"),
            messageId,
            TestContext.Current.CancellationToken);

        ServiceBusReceivedMessage deadLettered = result.Message;
        Assert.Equal(messageId.ToString("N"), deadLettered.MessageId);
        Assert.Equal("fault", deadLettered.ApplicationProperties[MessageHeaders.Reason]);
        Assert.Equal(failureMessage, deadLettered.ApplicationProperties[MessageHeaders.FaultMessage]);
        Assert.Equal(1, attempts);
        Assert.False(result.AlternativeQueueExisted);
    }

    static IBusControl CreateBus(
        AzureServiceBusLocalFixture fixture,
        ServiceBusClient client,
        ServiceBusAdministrationClient admin,
        string queue,
        Action<IServiceBusReceiveEndpointConfigurator> configureEndpoint) =>
        Bus.Factory.CreateUsingAzureServiceBus(configuration =>
        {
            configuration.Host(new Uri("sb://localhost/"), client, admin);
            configuration.DefaultMessageTimeToLive = EmulatorEntityTimeToLive;
            configuration.OverrideDefaultBusEndpointQueueName(fixture.Name("bus"));
            configuration.Publish<Fault>(topology => topology.DefaultMessageTimeToLive = EmulatorEntityTimeToLive);
            configuration.Publish<Fault<FaultedMessage>>(topology => topology.DefaultMessageTimeToLive = EmulatorEntityTimeToLive);
            configuration.ReceiveEndpoint(queue, endpoint =>
            {
                endpoint.ConfigureConsumeTopology = false;
                endpoint.DefaultMessageTimeToLive = EmulatorEntityTimeToLive;
                configureEndpoint(endpoint);
            });
        });

    static async Task<DeadLetterResult> SendAndReceiveDeadLetterAsync<T>(
        AzureServiceBusLocalFixture fixture,
        ServiceBusClient client,
        ServiceBusAdministrationClient admin,
        IBusControl bus,
        string queue,
        string alternativeQueue,
        T message,
        Guid messageId,
        CancellationToken cancellationToken)
        where T : class
    {
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            await using ServiceBusReceiver deadLetter = client.CreateReceiver(queue, new ServiceBusReceiverOptions
            {
                SubQueue = SubQueue.DeadLetter,
            });
            Task<ServiceBusReceivedMessage?> received = deadLetter.ReceiveMessageAsync(fixture.OperationTimeout, cancellationToken);
            ISendEndpoint endpoint = await bus.GetSendEndpointAsync(new Uri($"queue:{queue}"), cancellationToken: cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await endpoint.SendAsync(
                    message,
                    context => context.MessageId = messageId,
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            ServiceBusReceivedMessage actual = await received
                ?? throw new InvalidOperationException("The native dead-letter subqueue received no message.");
            await deadLetter.CompleteMessageAsync(actual, cancellationToken);
            await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            started = false;
            QueueRuntimeProperties terminal = await admin.GetQueueRuntimePropertiesAsync(queue, cancellationToken);
            Assert.Equal(0, terminal.ActiveMessageCount);
            Assert.Equal(0, terminal.DeadLetterMessageCount);

            return new DeadLetterResult(actual, await admin.QueueExistsAsync(alternativeQueue, cancellationToken));
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            await fixture.CleanupAsync(admin);
        }
    }

    static readonly TimeSpan EmulatorEntityTimeToLive = TimeSpan.FromHours(1);

    public sealed record SkippedMessage(string Value);

    public sealed record FaultedMessage(string Value);

    public sealed class NativeDeadLetterException(string message) : Exception(message);

    readonly record struct DeadLetterResult(ServiceBusReceivedMessage Message, bool AlternativeQueueExisted);
}
