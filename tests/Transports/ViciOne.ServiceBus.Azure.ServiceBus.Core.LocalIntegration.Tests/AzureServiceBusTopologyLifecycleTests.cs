using global::Azure.Messaging.ServiceBus;
using global::Azure.Messaging.ServiceBus.Administration;
using ViciOne.ServiceBus.Azure.ServiceBus.Core.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Azure.ServiceBus.Core.LocalIntegration.Tests.TopologyContracts;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Azure.ServiceBus.Core.LocalIntegration.Tests;

public sealed class AzureServiceBusTopologyLifecycleTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-TOPOLOGY-LIFECYCLE", "deploy-publish-topology-creates-only-declared-topics")]
    public async Task DeployPublishTopology_CreatesDeclaredTopicsWithoutAnUnusedBusQueue()
    {
        AzureServiceBusLocalFixture fixture = AzureServiceBusLocalFixture.Create("deploy-topology");
        ServiceBusAdministrationClient admin = fixture.CreateAdministrationClient();
        await using ServiceBusClient client = fixture.CreateClient();
        string explicitTopic = fixture.Name("explicit");
        string namespaceTopic = fixture.Name("namespace");
        string excludedTopic = fixture.Name("excluded");
        string unusedBusQueue = fixture.Name("bus");
        IBusControl bus = Bus.Factory.CreateUsingAzureServiceBus(configuration =>
        {
            configuration.Host(new Uri("sb://localhost/"), client, admin);
            configuration.DefaultMessageTimeToLive = EmulatorEntityTimeToLive;
            configuration.OverrideDefaultBusEndpointQueueName(unusedBusQueue);
            configuration.DeployPublishTopology = true;
            configuration.Message<ExplicitTopologyContract>(topology => topology.SetEntityName(explicitTopic));
            configuration.Message<NamespaceTopologyContract>(topology => topology.SetEntityName(namespaceTopic));
            configuration.Message<ExcludedTopologyContract>(topology => topology.SetEntityName(excludedTopic));
            configuration.Publish<ExplicitTopologyContract>(topology =>
                topology.DefaultMessageTimeToLive = EmulatorEntityTimeToLive);
            configuration.Publish<NamespaceTopologyContract>(topology =>
                topology.DefaultMessageTimeToLive = EmulatorEntityTimeToLive);
            configuration.AddPublishMessageTypesFromNamespaceContaining<NamespaceTopologyContract>();
        });
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;

            Assert.True(await admin.TopicExistsAsync(explicitTopic, cancellationToken));
            Assert.True(await admin.TopicExistsAsync(namespaceTopic, cancellationToken));
            Assert.False(await admin.TopicExistsAsync(excludedTopic, cancellationToken));
            Assert.False(await admin.QueueExistsAsync(unusedBusQueue, cancellationToken));

            await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            started = false;
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            await fixture.CleanupAsync(admin);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-TOPOLOGY-LIFECYCLE", "dynamic-endpoint-rejects-duplicate-removes-subscription-and-reconnects")]
    public async Task DynamicEndpoint_RejectsDuplicateRemovesSubscriptionAndReconnects()
    {
        AzureServiceBusLocalFixture fixture = AzureServiceBusLocalFixture.Create("dynamic-endpoint");
        ServiceBusAdministrationClient admin = fixture.CreateAdministrationClient();
        await using ServiceBusClient client = fixture.CreateClient();
        string queue = fixture.Name("input");
        string topic = fixture.Name("topic");
        string subscription = $"vsb-{Guid.NewGuid():N}";
        Guid firstId = NewId.NextGuid();
        Guid secondId = NewId.NextGuid();
        var first = Observation<ConsumeContext<DynamicEndpointMessage>>();
        var second = Observation<ConsumeContext<DynamicEndpointMessage>>();
        int entries = 0;
        IBusControl bus = Bus.Factory.CreateUsingAzureServiceBus(configuration =>
        {
            configuration.Host(new Uri("sb://localhost/"), client, admin);
            configuration.DefaultMessageTimeToLive = EmulatorEntityTimeToLive;
            configuration.OverrideDefaultBusEndpointQueueName(fixture.Name("bus"));
            configuration.Message<DynamicEndpointMessage>(topology => topology.SetEntityName(topic));
            configuration.Publish<DynamicEndpointMessage>(topology =>
                topology.DefaultMessageTimeToLive = EmulatorEntityTimeToLive);
        });
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            HostReceiveEndpointHandle initial = Connect(first);
            await initial.Ready.WaitAsync(fixture.OperationTimeout, cancellationToken);

            ConfigurationException conflict = Assert.Throws<ConfigurationException>(
                () => bus.ConnectReceiveEndpoint(queue, _ => { }));
            Assert.Contains(queue, conflict.Message, StringComparison.Ordinal);

            Assert.True(await admin.SubscriptionExistsAsync(topic, subscription, cancellationToken));
            await bus.Publish(new DynamicEndpointMessage(firstId), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.Equal(firstId, (await first.Task.WaitAsync(fixture.OperationTimeout, cancellationToken)).Message.Id);
            await initial.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.False(await admin.SubscriptionExistsAsync(topic, subscription, cancellationToken));

            HostReceiveEndpointHandle reconnected = Connect(second);
            await reconnected.Ready.WaitAsync(fixture.OperationTimeout, cancellationToken);
            await bus.Publish(new DynamicEndpointMessage(secondId), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.Equal(secondId, (await second.Task.WaitAsync(fixture.OperationTimeout, cancellationToken)).Message.Id);
            await reconnected.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);

            await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            started = false;
            Assert.Equal(2, entries);
            Assert.False(await admin.SubscriptionExistsAsync(topic, subscription, cancellationToken));
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            await fixture.CleanupAsync(admin);
        }

        HostReceiveEndpointHandle Connect(TaskCompletionSource<ConsumeContext<DynamicEndpointMessage>> observation) =>
            bus.ConnectReceiveEndpoint(queue, endpoint =>
            {
                var serviceBusEndpoint = (IServiceBusReceiveEndpointConfigurator)endpoint;
                endpoint.ConfigureConsumeTopology = false;
                serviceBusEndpoint.DefaultMessageTimeToLive = EmulatorEntityTimeToLive;
                serviceBusEndpoint.RemoveSubscriptions = true;
                serviceBusEndpoint.Subscribe<DynamicEndpointMessage>(subscription, configuration =>
                    configuration.DefaultMessageTimeToLive = EmulatorEntityTimeToLive);
                endpoint.Handler<DynamicEndpointMessage>(context =>
                {
                    Interlocked.Increment(ref entries);
                    observation.TrySetResult(context);
                    return Task.CompletedTask;
                });
            });
    }

    static TaskCompletionSource<T> Observation<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    static readonly TimeSpan EmulatorEntityTimeToLive = TimeSpan.FromHours(1);

    public sealed record DynamicEndpointMessage(Guid Id);

}
