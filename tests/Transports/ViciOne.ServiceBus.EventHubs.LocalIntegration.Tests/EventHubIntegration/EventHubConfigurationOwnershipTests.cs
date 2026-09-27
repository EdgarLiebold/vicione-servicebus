using Azure.Messaging.EventHubs.Producer;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.EventHubs.Configuration;
using ViciOne.ServiceBus.EventHubs.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EventHubs.LocalIntegration.Tests.EventHubIntegration;

public sealed class EventHubConfigurationOwnershipTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-EVENTHUB-CONFIGURATION-OWNERSHIP", "consumer-group-identity-distinguishes-valid-and-duplicate-endpoints")]
    public async Task ConsumerGroups_AreIndependentAndDuplicatePairsPreventBuildAsync()
    {
        await using EventHubLocalFixture fixture = EventHubLocalFixture.Create("endpoint-identity");
        int duplicateEndpointBuilds = 0;
        await using ServiceProvider duplicate = CreateEndpointProvider(fixture, ["cg1", "cg1"],
            () => Interlocked.Increment(ref duplicateEndpointBuilds));
        ConfigurationException failure = Assert.Throws<ConfigurationException>(() => duplicate.GetRequiredService<IBusControl>());
        Assert.Contains("config-eh/cg1 was added more than once", failure.ToString(), StringComparison.Ordinal);
        Assert.Equal(0, duplicateEndpointBuilds);

        int distinctEndpointBuilds = 0;
        await using ServiceProvider distinct = CreateEndpointProvider(fixture, ["cg1", "cg2"],
            () => Interlocked.Increment(ref distinctEndpointBuilds));
        IBusControl bus = distinct.GetRequiredService<IBusControl>();
        Assert.NotNull(bus);
        Assert.Equal(2, distinctEndpointBuilds);
    }

    private static ServiceProvider CreateEndpointProvider(EventHubLocalFixture fixture, string[] groups, Action built) =>
        new ServiceCollection().AddViciOneServiceBus(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.UsingInMemory();
            configuration.AddRider(rider => rider.UsingEventHub((_, eventHubs) =>
            {
                fixture.Configure(eventHubs);
                foreach (string group in groups)
                    eventHubs.ReceiveEndpoint("config-eh", group, endpoint =>
                    {
                        endpoint.ContainerName = fixture.ContainerName(group);
                        built();
                    });
            }));
        }).BuildServiceProvider(true);

    [Fact]
    [RequirementCoverage("REQ-VSB-EVENTHUB-CONFIGURATION-OWNERSHIP", "rejected-replacements-preserve-effective-clients-and-delivery")]
    public Task RejectedReplacement_PreservesEffectiveClientsAndDeliveryAsync() =>
        AssertConfiguredDeliveryAsync(rejectReplacements: true);

    [Fact]
    [RequirementCoverage("REQ-VSB-EVENTHUB-CONFIGURATION-OWNERSHIP", "invalid-input-does-not-consume-configuration-before-valid-delivery")]
    public Task InvalidConfiguration_CanBeCompletedBeforeFirstBuildAsync() =>
        AssertConfiguredDeliveryAsync(rejectReplacements: false);

    private static async Task AssertConfiguredDeliveryAsync(bool rejectReplacements)
    {
        const string eventHubName = "config-eh";
        Guid marker = Guid.NewGuid();
        var received = new TaskCompletionSource<ConfigurationMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var state = new DeliveryState(marker, received);
        await using EventHubLocalFixture fixture = EventHubLocalFixture.Create("config-owner");
        string container = fixture.ContainerName("original");
        string identifier = $"original-{marker:N}";
        int discardedCallbacks = 0;
        EventHubFactoryConfigurator? factory = null;
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(state)
            .AddViciOneServiceBus(configuration =>
            {
                configuration.Limits(MessageLimits.Conservative);
                configuration.UsingInMemory();
                configuration.AddRider(rider =>
                {
                    rider.AddConsumer<ConfigurationConsumer>();
                    rider.UsingEventHub((context, eventHubs) =>
                    {
                        factory = Assert.IsType<EventHubFactoryConfigurator>(eventHubs);
                        eventHubs.ReceiveEndpoint(eventHubName, EventHubLocalFixture.ConsumerGroup, endpoint =>
                        {
                            endpoint.ContainerName = container;
                            endpoint.CheckpointMessageCount = 1;
                            endpoint.ConfigureConsumer<ConfigurationConsumer>(context);
                        });
                        if (!rejectReplacements)
                        {
                            string[] failures = factory.Validate().Select(result => result.Key).Order().ToArray();
                            Assert.Equal(new[] { "HostSettings", "HostSettings", "StorageSettings" }, failures);
                            Assert.Throws<ArgumentException>(() => eventHubs.Host(" "));
                            Assert.Throws<ArgumentException>(() => eventHubs.Storage(" "));
                            Assert.Throws<ArgumentNullException>(() => eventHubs.ConfigureProducerOptions(null!));
                            Assert.Equal(failures, factory.Validate().Select(result => result.Key).Order().ToArray());
                        }

                        fixture.Configure(eventHubs);
                        eventHubs.ConfigureProducerOptions(options => options.Identifier = identifier);
                        Assert.Empty(factory.Validate());
                        if (rejectReplacements)
                        {
                            ConfigurationException host = Assert.Throws<ConfigurationException>(() =>
                                eventHubs.Host("Endpoint=sb://discarded.invalid/;SharedAccessKeyName=unused;SharedAccessKey=unused"));
                            ConfigurationException storage = Assert.Throws<ConfigurationException>(() =>
                                eventHubs.Storage(new Uri("http://discarded.invalid/checkpoints"),
                                    _ => Interlocked.Increment(ref discardedCallbacks)));
                            ConfigurationException options = Assert.Throws<ConfigurationException>(() =>
                                eventHubs.ConfigureProducerOptions(_ => Interlocked.Increment(ref discardedCallbacks)));
                            Assert.Contains("Host settings may not be specified more than once", host.Message, StringComparison.Ordinal);
                            Assert.Contains("Storage settings may not be specified more than once", storage.Message, StringComparison.Ordinal);
                            Assert.Contains("ProducerOptions configurator may not be specified more than once", options.Message, StringComparison.Ordinal);
                            Assert.Empty(factory.Validate());
                        }
                    });
                });
            }).BuildServiceProvider(true);
        IBusControl bus = provider.GetRequiredService<IBusControl>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;
        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            Assert.NotNull(factory);
            await factory.ConnectionContextSupervisor.SendAsync(Pipe.ExecuteAwaited<ConnectionContext>(async context =>
            {
                await using EventHubProducerClient client = context.CreateEventHubClient(eventHubName);
                Assert.Equal(identifier, client.Identifier);
                Assert.Equal(eventHubName, client.EventHubName);
                await using EventHubProducerClient expectedClient = fixture.CreateRawProducer(eventHubName);
                Assert.Equal(expectedClient.FullyQualifiedNamespace, client.FullyQualifiedNamespace);
                Assert.NotEmpty(await client.GetPartitionIdsAsync(cancellationToken));
            }), cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);

            await using AsyncServiceScope scope = provider.CreateAsyncScope();
            IEventHubProducer producer = await scope.ServiceProvider.GetRequiredService<IEventHubProducerProvider>()
                .GetProducerAsync(eventHubName, cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            var expected = new ConfigurationMessage(marker, "original configuration still delivers");
            await producer.ProduceAsync(expected, cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.Equal(expected, await received.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));
            await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = false;

            var blobs = new List<string>();
            await foreach (BlobItem blob in fixture.GetContainer(container).GetBlobsAsync(cancellationToken: cancellationToken))
                blobs.Add(blob.Name);
            Assert.Contains(blobs, name => name.Contains($"/{eventHubName}/{EventHubLocalFixture.ConsumerGroup}/checkpoint/", StringComparison.Ordinal));
            Assert.Equal(0, Volatile.Read(ref discardedCallbacks));
            Assert.Equal(1, state.Deliveries);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    private sealed record ConfigurationMessage(Guid Marker, string Text);

    private sealed class DeliveryState(Guid marker, TaskCompletionSource<ConfigurationMessage> received)
    {
        private int _deliveries;
        public int Deliveries => Volatile.Read(ref _deliveries);

        public void Observe(ConfigurationMessage message)
        {
            if (message.Marker != marker)
                return;
            Interlocked.Increment(ref _deliveries);
            received.TrySetResult(message);
        }
    }

    private sealed class ConfigurationConsumer(DeliveryState state) : IConsumer<ConfigurationMessage>
    {
        public Task ConsumeAsync(ConsumeContext<ConfigurationMessage> context)
        {
            state.Observe(context.Message);
            return Task.CompletedTask;
        }
    }
}
