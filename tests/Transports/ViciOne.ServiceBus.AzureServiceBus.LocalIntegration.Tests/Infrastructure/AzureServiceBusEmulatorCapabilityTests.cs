using System.Reflection;
using global::Azure.Messaging.ServiceBus;
using global::Azure.Messaging.ServiceBus.Administration;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.AzureServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.LocalIntegration.Tests.Infrastructure;

public sealed class AzureServiceBusEmulatorCapabilityTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-HOST-CONFIGURATION", "factory-retains-caller-messaging-client-and-creates-administration-client")]
    public async Task FactoryWithCallerMessagingClient_CreatesWorkingAdministrationClientAndPreservesMessagingClientAsync()
    {
        AzureServiceBusLocalFixture fixture = AzureServiceBusLocalFixture.Create("factory-mixed-clients");
        ServiceBusAdministrationClient cleanup = fixture.CreateAdministrationClient();
        await using ServiceBusClient messagingClient = fixture.CreateClient();
        string queue = fixture.Name("queue");
        string messageId = Guid.NewGuid().ToString("N");
        using CancellationTokenSource timeout = fixture.OperationCancellation();
        Uri address = ServiceBusConnectionStringProperties.Parse(fixture.ManagementConnectionString).Endpoint;
        IServiceBusHostConfiguration configuration = DispatchProxy.Create<IServiceBusHostConfiguration, HostConfigurationProxy>();
        var proxy = (HostConfigurationProxy)(object)configuration;
        proxy.Address = address;
        proxy.Settings = new HostSettings
        {
            ServiceUri = address,
            ConnectionString = fixture.ManagementConnectionString,
            ServiceBusClient = messagingClient,
            RetryLimit = 0,
        };
        IPipeContextFactory<ConnectionContext> factory = new ConnectionContextFactory(configuration);

        try
        {
            IPipeContextAgent<ConnectionContext> agent = factory.CreateContext(new Supervisor());
            await using ServiceBusConnectionContext connection = Assert.IsType<ServiceBusConnectionContext>(await agent.Context);
            QueueProperties created = await connection.CreateQueueAsync(new CreateQueueOptions(queue), timeout.Token);
            await using ServiceBusSender sender = connection.CreateMessageSender(queue);
            await using ServiceBusReceiver receiver = messagingClient.CreateReceiver(queue);
            await sender.SendMessageAsync(new ServiceBusMessage("factory-payload") { MessageId = messageId }, timeout.Token);
            ServiceBusReceivedMessage received = await receiver.ReceiveMessageAsync(fixture.OperationTimeout, timeout.Token)
                ?? throw new InvalidOperationException("The factory's messaging client delivered no message.");

            Assert.Equal(queue, created.Name);
            Assert.Equal(messageId, received.MessageId);
            Assert.Equal("factory-payload", received.Body.ToString());
            await receiver.CompleteMessageAsync(received, timeout.Token);
        }
        finally
        {
            if (await cleanup.QueueExistsAsync(queue, timeout.Token))
                await cleanup.DeleteQueueAsync(queue, timeout.Token);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-HOST-CONFIGURATION", "factory-retains-caller-administration-client-and-creates-messaging-client")]
    public async Task FactoryWithCallerAdministrationClient_CreatesWorkingMessagingClientAndPreservesAdministrationClientAsync()
    {
        AzureServiceBusLocalFixture fixture = AzureServiceBusLocalFixture.Create("factory-mixed-clients");
        ServiceBusAdministrationClient administrationClient = fixture.CreateAdministrationClient();
        await using ServiceBusClient receiverClient = fixture.CreateClient();
        string queue = fixture.Name("queue");
        string messageId = Guid.NewGuid().ToString("N");
        using CancellationTokenSource timeout = fixture.OperationCancellation();
        Uri address = ServiceBusConnectionStringProperties.Parse(fixture.DataConnectionString).Endpoint;
        IServiceBusHostConfiguration configuration = DispatchProxy.Create<IServiceBusHostConfiguration, HostConfigurationProxy>();
        var proxy = (HostConfigurationProxy)(object)configuration;
        proxy.Address = address;
        proxy.Settings = new HostSettings
        {
            ServiceUri = address,
            ConnectionString = fixture.DataConnectionString,
            ServiceBusAdministrationClient = administrationClient,
            RetryLimit = 0,
        };
        IPipeContextFactory<ConnectionContext> factory = new ConnectionContextFactory(configuration);

        try
        {
            IPipeContextAgent<ConnectionContext> agent = factory.CreateContext(new Supervisor());
            await using ServiceBusConnectionContext connection = Assert.IsType<ServiceBusConnectionContext>(await agent.Context);
            QueueProperties created = await connection.CreateQueueAsync(new CreateQueueOptions(queue), timeout.Token);
            await using ServiceBusSender sender = connection.CreateMessageSender(queue);
            await using ServiceBusReceiver receiver = receiverClient.CreateReceiver(queue);
            await sender.SendMessageAsync(new ServiceBusMessage("factory-payload") { MessageId = messageId }, timeout.Token);
            ServiceBusReceivedMessage received = await receiver.ReceiveMessageAsync(fixture.OperationTimeout, timeout.Token)
                ?? throw new InvalidOperationException("The factory's messaging client delivered no message.");

            Assert.Equal(queue, created.Name);
            Assert.Equal(messageId, received.MessageId);
            Assert.Equal("factory-payload", received.Body.ToString());
            await receiver.CompleteMessageAsync(received, timeout.Token);
        }
        finally
        {
            if (await administrationClient.QueueExistsAsync(queue, timeout.Token))
                await administrationClient.DeleteQueueAsync(queue, timeout.Token);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-EMULATOR-CAPABILITY", "administration-queues-topics-subscriptions-rules-and-update")]
    public async Task AdministrationClient_ManagesQueuesTopicsSubscriptionsAndRulesAsync()
    {
        AzureServiceBusLocalFixture fixture = AzureServiceBusLocalFixture.Create();
        ServiceBusAdministrationClient admin = fixture.CreateAdministrationClient();
        string queue = AzureServiceBusLocalFixture.EntityName("admin-queue");
        string topic = AzureServiceBusLocalFixture.EntityName("admin-topic");
        string subscription = "subscription";
        string rule = "exact-subject";
        using CancellationTokenSource timeout = fixture.OperationCancellation();

        try
        {
            QueueProperties createdQueue = await admin.CreateQueueAsync(
                new CreateQueueOptions(queue) { MaxDeliveryCount = 4 }, timeout.Token);
            TopicProperties createdTopic = await admin.CreateTopicAsync(topic, timeout.Token);
            SubscriptionProperties createdSubscription = await admin.CreateSubscriptionAsync(
                new CreateSubscriptionOptions(topic, subscription) { MaxDeliveryCount = 5 },
                new CreateRuleOptions(rule, new CorrelationRuleFilter { Subject = "selected" }),
                timeout.Token);

            createdQueue.MaxDeliveryCount = 7;
            QueueProperties updatedQueue = await admin.UpdateQueueAsync(createdQueue, timeout.Token);
            RuleProperties readRule = await admin.GetRuleAsync(topic, subscription, rule, timeout.Token);

            Assert.Equal(queue, createdQueue.Name);
            Assert.Equal(7, updatedQueue.MaxDeliveryCount);
            Assert.Equal(topic, createdTopic.Name);
            Assert.Equal(subscription, createdSubscription.SubscriptionName);
            CorrelationRuleFilter filter = Assert.IsType<CorrelationRuleFilter>(readRule.Filter);
            Assert.Equal("selected", filter.Subject);
        }
        finally
        {
            if (await admin.TopicExistsAsync(topic, timeout.Token))
                await admin.DeleteTopicAsync(topic, timeout.Token);
            if (await admin.QueueExistsAsync(queue, timeout.Token))
                await admin.DeleteQueueAsync(queue, timeout.Token);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-EMULATOR-CAPABILITY", "amqp-send-receive-complete")]
    public async Task AmqpClient_SendsReceivesAndCompletesAnExactMessageAsync()
    {
        AzureServiceBusLocalFixture fixture = AzureServiceBusLocalFixture.Create();
        ServiceBusAdministrationClient admin = fixture.CreateAdministrationClient();
        await using ServiceBusClient client = fixture.CreateClient();
        string queue = AzureServiceBusLocalFixture.EntityName("amqp");
        string messageId = Guid.NewGuid().ToString("N");
        using CancellationTokenSource timeout = fixture.OperationCancellation();

        try
        {
            await admin.CreateQueueAsync(queue, timeout.Token);
            await using ServiceBusSender sender = client.CreateSender(queue);
            await using ServiceBusReceiver receiver = client.CreateReceiver(queue);
            await sender.SendMessageAsync(
                new ServiceBusMessage(BinaryData.FromString("payload")) { MessageId = messageId },
                timeout.Token);

            ServiceBusReceivedMessage received = await receiver.ReceiveMessageAsync(fixture.OperationTimeout, timeout.Token)
                ?? throw new InvalidOperationException("The emulator accepted the send but delivered no message.");

            Assert.Equal(messageId, received.MessageId);
            Assert.Equal("payload", received.Body.ToString());
            await receiver.CompleteMessageAsync(received, timeout.Token);
        }
        finally
        {
            if (await admin.QueueExistsAsync(queue, timeout.Token))
                await admin.DeleteQueueAsync(queue, timeout.Token);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-EMULATOR-CAPABILITY", "sessions-and-session-state")]
    public async Task Sessions_CarryIdentityAndMutableProviderStateAsync()
    {
        AzureServiceBusLocalFixture fixture = AzureServiceBusLocalFixture.Create();
        ServiceBusAdministrationClient admin = fixture.CreateAdministrationClient();
        await using ServiceBusClient client = fixture.CreateClient();
        string queue = AzureServiceBusLocalFixture.EntityName("session");
        string sessionId = Guid.NewGuid().ToString("N");
        using CancellationTokenSource timeout = fixture.OperationCancellation();

        try
        {
            await admin.CreateQueueAsync(new CreateQueueOptions(queue) { RequiresSession = true }, timeout.Token);
            await using ServiceBusSender sender = client.CreateSender(queue);
            await sender.SendMessageAsync(new ServiceBusMessage("session-payload") { SessionId = sessionId }, timeout.Token);
            await using ServiceBusSessionReceiver receiver = await client.AcceptSessionAsync(
                queue, sessionId, cancellationToken: timeout.Token);

            await receiver.SetSessionStateAsync(BinaryData.FromString("state-42"), timeout.Token);
            BinaryData? state = await receiver.GetSessionStateAsync(timeout.Token);
            ServiceBusReceivedMessage received = await receiver.ReceiveMessageAsync(fixture.OperationTimeout, timeout.Token)
                ?? throw new InvalidOperationException("The accepted session delivered no message.");

            Assert.Equal(sessionId, receiver.SessionId);
            Assert.Equal("state-42", state?.ToString());
            Assert.Equal("session-payload", received.Body.ToString());
            await receiver.CompleteMessageAsync(received, timeout.Token);
            await receiver.SetSessionStateAsync(null, timeout.Token);
            Assert.Null(await receiver.GetSessionStateAsync(timeout.Token));
        }
        finally
        {
            if (await admin.QueueExistsAsync(queue, timeout.Token))
                await admin.DeleteQueueAsync(queue, timeout.Token);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-EMULATOR-CAPABILITY", "provider-schedule-sequence-and-cancellation-acceptance")]
    public async Task Scheduling_ReturnsAProviderSequenceAndAcceptsCancellationAsync()
    {
        AzureServiceBusLocalFixture fixture = AzureServiceBusLocalFixture.Create();
        ServiceBusAdministrationClient admin = fixture.CreateAdministrationClient();
        await using ServiceBusClient client = fixture.CreateClient();
        string queue = AzureServiceBusLocalFixture.EntityName("schedule");
        using CancellationTokenSource timeout = fixture.OperationCancellation();

        try
        {
            await admin.CreateQueueAsync(queue, timeout.Token);
            await using ServiceBusSender sender = client.CreateSender(queue);
            long sequence = await sender.ScheduleMessageAsync(
                new ServiceBusMessage("scheduled") { MessageId = Guid.NewGuid().ToString("N") },
                TimeProvider.System.GetUtcNow().AddHours(1),
                timeout.Token);

            Assert.True(sequence > 0);
            await sender.CancelScheduledMessageAsync(sequence, timeout.Token);
        }
        finally
        {
            if (await admin.QueueExistsAsync(queue, timeout.Token))
                await admin.DeleteQueueAsync(queue, timeout.Token);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-EMULATOR-CAPABILITY", "duplicate-detection")]
    public async Task DuplicateDetection_KeepsOneProviderMessageForOneMessageIdAsync()
    {
        AzureServiceBusLocalFixture fixture = AzureServiceBusLocalFixture.Create();
        ServiceBusAdministrationClient admin = fixture.CreateAdministrationClient();
        await using ServiceBusClient client = fixture.CreateClient();
        string queue = AzureServiceBusLocalFixture.EntityName("dedupe");
        string messageId = Guid.NewGuid().ToString("N");
        using CancellationTokenSource timeout = fixture.OperationCancellation();

        try
        {
            await admin.CreateQueueAsync(new CreateQueueOptions(queue)
            {
                RequiresDuplicateDetection = true,
                DuplicateDetectionHistoryTimeWindow = TimeSpan.FromMinutes(1),
            }, timeout.Token);
            await using ServiceBusSender sender = client.CreateSender(queue);
            await using ServiceBusReceiver receiver = client.CreateReceiver(queue);
            await sender.SendMessagesAsync(
                [new ServiceBusMessage("first") { MessageId = messageId }, new ServiceBusMessage("duplicate") { MessageId = messageId }],
                timeout.Token);

            IReadOnlyList<ServiceBusReceivedMessage> messages = await receiver.PeekMessagesAsync(10, cancellationToken: timeout.Token);

            ServiceBusReceivedMessage only = Assert.Single(messages);
            Assert.Equal(messageId, only.MessageId);
            Assert.Equal("first", only.Body.ToString());
        }
        finally
        {
            if (await admin.QueueExistsAsync(queue, timeout.Token))
                await admin.DeleteQueueAsync(queue, timeout.Token);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-EMULATOR-CAPABILITY", "dead-letter-reason-and-description")]
    public async Task DeadLettering_PreservesTheExactReasonAndMessageAsync()
    {
        AzureServiceBusLocalFixture fixture = AzureServiceBusLocalFixture.Create();
        ServiceBusAdministrationClient admin = fixture.CreateAdministrationClient();
        await using ServiceBusClient client = fixture.CreateClient();
        string queue = AzureServiceBusLocalFixture.EntityName("deadletter");
        string messageId = Guid.NewGuid().ToString("N");
        using CancellationTokenSource timeout = fixture.OperationCancellation();

        try
        {
            await admin.CreateQueueAsync(queue, timeout.Token);
            await using ServiceBusSender sender = client.CreateSender(queue);
            await using ServiceBusReceiver receiver = client.CreateReceiver(queue);
            await using ServiceBusReceiver deadLetter = client.CreateReceiver(queue, new ServiceBusReceiverOptions
            {
                SubQueue = SubQueue.DeadLetter,
            });
            await sender.SendMessageAsync(new ServiceBusMessage("failed") { MessageId = messageId }, timeout.Token);
            ServiceBusReceivedMessage received = await receiver.ReceiveMessageAsync(fixture.OperationTimeout, timeout.Token)
                ?? throw new InvalidOperationException("The message to dead-letter was not delivered.");
            await receiver.DeadLetterMessageAsync(received, "capability-reason", "exact-description", timeout.Token);

            ServiceBusReceivedMessage failed = await deadLetter.ReceiveMessageAsync(fixture.OperationTimeout, timeout.Token)
                ?? throw new InvalidOperationException("The dead-letter subqueue received no message.");

            Assert.Equal(messageId, failed.MessageId);
            Assert.Equal("failed", failed.Body.ToString());
            Assert.Equal("capability-reason", failed.DeadLetterReason);
            Assert.Equal("exact-description", failed.DeadLetterErrorDescription);
            await deadLetter.CompleteMessageAsync(failed, timeout.Token);
        }
        finally
        {
            if (await admin.QueueExistsAsync(queue, timeout.Token))
                await admin.DeleteQueueAsync(queue, timeout.Token);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-EMULATOR-CAPABILITY", "forwarding")]
    public async Task Forwarding_DeliversTheExactMessageToTheConfiguredDestinationAsync()
    {
        AzureServiceBusLocalFixture fixture = AzureServiceBusLocalFixture.Create();
        ServiceBusAdministrationClient admin = fixture.CreateAdministrationClient();
        await using ServiceBusClient client = fixture.CreateClient();
        string source = AzureServiceBusLocalFixture.EntityName("forward-source");
        string destination = AzureServiceBusLocalFixture.EntityName("forward-destination");
        string messageId = Guid.NewGuid().ToString("N");
        using CancellationTokenSource timeout = fixture.OperationCancellation();

        try
        {
            await admin.CreateQueueAsync(destination, timeout.Token);
            await admin.CreateQueueAsync(new CreateQueueOptions(source) { ForwardTo = destination }, timeout.Token);
            await using ServiceBusSender sender = client.CreateSender(source);
            await using ServiceBusReceiver receiver = client.CreateReceiver(destination);
            await sender.SendMessageAsync(new ServiceBusMessage("forwarded") { MessageId = messageId }, timeout.Token);

            ServiceBusReceivedMessage forwarded = await receiver.ReceiveMessageAsync(fixture.OperationTimeout, timeout.Token)
                ?? throw new InvalidOperationException("The configured destination received no forwarded message.");

            Assert.Equal(messageId, forwarded.MessageId);
            Assert.Equal("forwarded", forwarded.Body.ToString());
            await receiver.CompleteMessageAsync(forwarded, timeout.Token);
        }
        finally
        {
            if (await admin.QueueExistsAsync(source, timeout.Token))
                await admin.DeleteQueueAsync(source, timeout.Token);
            if (await admin.QueueExistsAsync(destination, timeout.Token))
                await admin.DeleteQueueAsync(destination, timeout.Token);
        }
    }

    public class HostConfigurationProxy : DispatchProxy
    {
        public Uri Address { get; set; } = null!;
        public ServiceBusHostSettings Settings { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "get_HostAddress" => Address,
            "get_Settings" => Settings,
            _ => throw new NotSupportedException(targetMethod?.Name),
        };
    }
}
