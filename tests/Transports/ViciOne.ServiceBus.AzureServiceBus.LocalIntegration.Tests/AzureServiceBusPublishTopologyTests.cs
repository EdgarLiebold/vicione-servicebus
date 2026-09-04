using System.Collections.Concurrent;
using global::Azure.Messaging.ServiceBus;
using global::Azure.Messaging.ServiceBus.Administration;
using ViciOne.ServiceBus.AzureServiceBus;
using ViciOne.ServiceBus.AzureServiceBus.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.LocalIntegration.Tests;

public sealed class AzureServiceBusPublishTopologyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-PUBLISH-TOPOLOGY", "hierarchy-multiple-types-and-array-drain-exactly-once")]
    public async Task HierarchyMultipleTypesAndArray_DeliverEveryIdentityExactlyOnceAsync()
    {
        AzureServiceBusLocalFixture fixture = AzureServiceBusLocalFixture.Create("publish-matrix");
        ServiceBusAdministrationClient admin = fixture.CreateAdministrationClient();
        await using ServiceBusClient client = fixture.CreateClient();
        string queue = fixture.Name("input");
        Guid leftId = NewId.NextGuid();
        Guid rightId = NewId.NextGuid();
        Guid alphaId = NewId.NextGuid();
        Guid betaId = NewId.NextGuid();
        Guid arrayId = NewId.NextGuid();
        var deliveries = new ConcurrentDictionary<Guid, int>();
        var completed = Observation<bool>();
        int entries = 0;
        IBusControl bus = CreateBus(fixture, client, admin, configuration =>
        {
            ConfigurePublish<IHierarchyBase>(configuration);
            ConfigurePublish<IHierarchyLeft>(configuration);
            ConfigurePublish<IHierarchyRight>(configuration);
            ConfigurePublish<AlphaMessage>(configuration);
            ConfigurePublish<BetaMessage>(configuration);
            ConfigurePublish<ArrayMessage[]>(configuration);
            configuration.ReceiveEndpoint(queue, endpoint =>
            {
                endpoint.ConfigureConsumeTopology = false;
                endpoint.DefaultMessageTimeToLive = EmulatorEntityTimeToLive;
                ConfigureSubscription<IHierarchyBase>(endpoint, fixture.Name("base-sub"));
                ConfigureSubscription<AlphaMessage>(endpoint, fixture.Name("alpha-sub"));
                ConfigureSubscription<BetaMessage>(endpoint, fixture.Name("beta-sub"));
                ConfigureSubscription<ArrayMessage[]>(endpoint, fixture.Name("array-sub"));
                endpoint.Handler<IHierarchyBase>(context => RecordAsync(context.Message.Id));
                endpoint.Handler<AlphaMessage>(context => RecordAsync(context.Message.Id));
                endpoint.Handler<BetaMessage>(context => RecordAsync(context.Message.Id));
                endpoint.Handler<ArrayMessage[]>(context => RecordAsync(Assert.Single(context.Message).Id));
            });
        });
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            await bus.PublishAsync<IHierarchyLeft>(new { Id = leftId, Value = "left" }, cancellationToken);
            await bus.PublishAsync<IHierarchyRight>(new { Id = rightId, Value = "right" }, cancellationToken);
            await bus.PublishAsync(new AlphaMessage(alphaId), cancellationToken);
            await bus.PublishAsync(new BetaMessage(betaId), cancellationToken);
            await bus.PublishAsync(new[] { new ArrayMessage(arrayId) }, cancellationToken);
            var formatter = new ServiceBusMessageNameFormatter();
            string baseTopic = formatter.GetMessageName(typeof(IHierarchyBase)).ToString();
            string leftTopic = formatter.GetMessageName(typeof(IHierarchyLeft)).ToString();
            string rightTopic = formatter.GetMessageName(typeof(IHierarchyRight)).ToString();
            SubscriptionProperties leftBridge = Assert.Single(await GetSubscriptionsAsync(admin, leftTopic, cancellationToken));
            SubscriptionProperties rightBridge = Assert.Single(await GetSubscriptionsAsync(admin, rightTopic, cancellationToken));
            Assert.Equal(baseTopic, ForwardEntityPath(leftBridge.ForwardTo));
            Assert.Equal(baseTopic, ForwardEntityPath(rightBridge.ForwardTo));
            Assert.True(await completed.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));

            await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            started = false;
            Guid[] expected = [leftId, rightId, alphaId, betaId, arrayId];
            Assert.Equal(expected.Order(), deliveries.Keys.Order());
            Assert.All(deliveries.Values, count => Assert.Equal(1, count));
            Assert.Equal(expected.Length, entries);
            QueueRuntimeProperties terminal = await admin.GetQueueRuntimePropertiesAsync(queue, cancellationToken);
            Assert.Equal(0, terminal.ActiveMessageCount);
            Assert.Equal(0, terminal.DeadLetterMessageCount);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            await fixture.CleanupAsync(admin);
        }

        Task RecordAsync(Guid id)
        {
            deliveries.AddOrUpdate(id, 1, static (_, count) => count + 1);
            if (Interlocked.Increment(ref entries) == 5)
                completed.TrySetResult(true);
            return Task.CompletedTask;
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-PUBLISH-TOPOLOGY", "direct-topic-uses-exact-provider-address-and-drains")]
    public async Task DirectTopic_UsesExactProviderAddressAndDrainsAsync()
    {
        AzureServiceBusLocalFixture fixture = AzureServiceBusLocalFixture.Create("address-routes");
        ServiceBusAdministrationClient admin = fixture.CreateAdministrationClient();
        await using ServiceBusClient client = fixture.CreateClient();
        string topicQueue = fixture.Name("topic-input");
        string topic = fixture.Name("private-topic");
        string subscription = $"vsb-{Guid.NewGuid():N}";
        Guid topicId = NewId.NextGuid();
        var direct = Observation<ConsumeContext<DirectTopicMessage>>();
        IBusControl bus = CreateBus(fixture, client, admin, configuration =>
        {
            configuration.Message<DirectTopicMessage>(topology => topology.SetEntityName(topic));
            configuration.Publish<DirectTopicMessage>(topology =>
            {
                topology.DefaultMessageTimeToLive = EmulatorEntityTimeToLive;
            });
            configuration.ReceiveEndpoint(topicQueue, endpoint =>
            {
                endpoint.ConfigureConsumeTopology = false;
                endpoint.DefaultMessageTimeToLive = EmulatorEntityTimeToLive;
                endpoint.Subscribe<DirectTopicMessage>(subscription, settings =>
                    settings.DefaultMessageTimeToLive = EmulatorEntityTimeToLive);
                endpoint.Handler<DirectTopicMessage>(context =>
                {
                    direct.TrySetResult(context);
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
            SubscriptionProperties configured =
                (await admin.GetSubscriptionAsync(topic, subscription, cancellationToken)).Value;
            Assert.Equal(topicQueue, ForwardEntityPath(configured.ForwardTo));
            ISendEndpoint topicEndpoint = await bus.GetSendEndpointAsync(new Uri($"topic:{topic}"), TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            await topicEndpoint.SendAsync(new DirectTopicMessage(topicId, "exact-topic-value"), cancellationToken);

            ConsumeContext<DirectTopicMessage> topicContext =
                await direct.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.Equal(new DirectTopicMessage(topicId, "exact-topic-value"), topicContext.Message);
            Assert.Equal(new Uri($"sb://localhost/{topicQueue}"), topicContext.Advanced().ReceiveContext.InputAddress);

            await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            started = false;
            Assert.Equal(0, (await admin.GetQueueRuntimePropertiesAsync(topicQueue, cancellationToken)).Value.ActiveMessageCount);
            SubscriptionRuntimeProperties terminal =
                await admin.GetSubscriptionRuntimePropertiesAsync(topic, subscription, cancellationToken);
            Assert.Equal(0, terminal.ActiveMessageCount);
            Assert.Equal(0, terminal.DeadLetterMessageCount);
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
        ServiceBusAdministrationClient admin,
        Action<IServiceBusBusFactoryConfigurator> configure) =>
        Bus.Factory.CreateUsingAzureServiceBus(configuration =>
        {
            configuration.Host(new Uri("sb://localhost/"), client, admin);
            configuration.DefaultMessageTimeToLive = EmulatorEntityTimeToLive;
            configuration.OverrideDefaultBusEndpointQueueName(fixture.Name("bus"));
            configure(configuration);
        });

    static void ConfigurePublish<T>(IServiceBusBusFactoryConfigurator configuration)
        where T : class =>
        configuration.Publish<T>(topology => topology.DefaultMessageTimeToLive = EmulatorEntityTimeToLive);

    static void ConfigureSubscription<T>(IServiceBusReceiveEndpointConfigurator endpoint, string subscription)
        where T : class =>
        endpoint.Subscribe<T>(subscription, settings => settings.DefaultMessageTimeToLive = EmulatorEntityTimeToLive);

    static TaskCompletionSource<T> Observation<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    static async Task<SubscriptionProperties[]> GetSubscriptionsAsync(
        ServiceBusAdministrationClient admin,
        string topic,
        CancellationToken cancellationToken)
    {
        var subscriptions = new List<SubscriptionProperties>();
        await foreach (SubscriptionProperties value in admin.GetSubscriptionsAsync(topic, cancellationToken))
            subscriptions.Add(value);
        return subscriptions.ToArray();
    }

    static string ForwardEntityPath(string forwardTo) =>
        Uri.TryCreate(forwardTo, UriKind.Absolute, out Uri? address)
            ? Uri.UnescapeDataString(address.AbsolutePath.Trim('/'))
            : forwardTo;

    static readonly TimeSpan EmulatorEntityTimeToLive = TimeSpan.FromHours(1);

    public interface IHierarchyBase
    {
        Guid Id { get; }
        string Value { get; }
    }

    public interface IHierarchyLeft : IHierarchyBase;

    public interface IHierarchyRight : IHierarchyBase;

    public sealed record AlphaMessage(Guid Id);

    public sealed record BetaMessage(Guid Id);

    public sealed record ArrayMessage(Guid Id);

    public sealed record DirectTopicMessage(Guid Id, string Value);
}
