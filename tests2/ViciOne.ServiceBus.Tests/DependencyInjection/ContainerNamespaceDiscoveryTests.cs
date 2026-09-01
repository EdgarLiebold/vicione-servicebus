using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DependencyInjection
{
    public sealed class ContainerNamespaceDiscoveryTests
    {
        [Fact]
        [RequirementCoverage("REQ-VSB-CONTAINER-DISCOVERY", "consumer-saga-machine-and-activities-run-on-discovered-endpoints")]
        public async Task NamespaceDiscovery_ConfiguresAndExecutesEveryOwnedEndpointEndToEnd()
        {
            TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
                .GetValidatedOptions().OperationTimeout!.Value;
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            var endpoints = new DiscoveryEndpointObserver();
            await using ServiceProvider provider = new ServiceCollection()
                .AddViciOneServiceBusTestHarness(configuration =>
                {
                    configuration.SetTestTimeouts(timeout, timeout);
                    configuration.SetInMemorySagaRepositoryProvider();
                    configuration.AddConsumersFromNamespaceContaining<ContainerDiscovery.DiscoveryMarker>();
                    configuration.AddSagaStateMachinesFromNamespaceContaining<ContainerDiscovery.DiscoveryMarker>();
                    configuration.AddSagasFromNamespaceContaining<ContainerDiscovery.DiscoveryMarker>();
                    configuration.AddActivitiesFromNamespaceContaining<ContainerDiscovery.DiscoveryMarker>();
                    configuration.AddRequestClient<ContainerDiscovery.DiscoveryPing>(new Uri("queue:ping-queue"));
                    configuration.UsingInMemory((context, bus) =>
                    {
                        bus.ConnectEndpointConfigurationObserver(endpoints);
                        bus.ConfigureEndpoints(context, filter =>
                            filter.Exclude<ContainerDiscovery.DiscoveryExcludedConsumer>());
                    });
                })
                .BuildServiceProvider(validateScopes: true);
            ITestHarness harness = await provider.StartTestHarness().WaitAsync(timeout, cancellationToken);

            try
            {
                Guid routingCorrelationId = NewId.NextGuid();
                var builder = new RoutingSlipBuilder(routingCorrelationId);
                builder.AddActivity(
                    "Ping",
                    new Uri("queue:Ping_execute"),
                    new ContainerDiscovery.PingArguments(routingCorrelationId));
                builder.AddActivity(
                    "PingSecond",
                    new Uri("queue:PingSecond_execute"),
                    new ContainerDiscovery.PingArguments(routingCorrelationId));
                await harness.Bus.Execute(builder.Build(), cancellationToken);
                IPublishedMessage<RoutingSlipCompleted> slipCompleted = await harness.Published
                    .SelectAsync<RoutingSlipCompleted>(
                        message => message.Context.Message.TrackingNumber == routingCorrelationId,
                        cancellationToken)
                    .First()
                    .WaitAsync(timeout, cancellationToken);

                Guid messageId = NewId.NextGuid();
                IRequestClient<ContainerDiscovery.DiscoveryPing> client =
                    harness.GetRequestClient<ContainerDiscovery.DiscoveryPing>();
                Response<ContainerDiscovery.DiscoveryPong> response = await client
                    .GetResponse<ContainerDiscovery.DiscoveryPong>(
                        new ContainerDiscovery.DiscoveryPing(messageId),
                        cancellationToken);
                ISagaStateMachineTestHarness<ContainerDiscovery.DiscoveryPingStateMachine,
                    ContainerDiscovery.DiscoveryPingState> machineHarness = harness
                    .GetSagaStateMachineHarness<ContainerDiscovery.DiscoveryPingStateMachine,
                        ContainerDiscovery.DiscoveryPingState>();
                await machineHarness.Consumed.SelectAsync<ContainerDiscovery.PingReceived>(cancellationToken)
                    .First().WaitAsync(timeout, cancellationToken);
                await harness.Bus.Publish(new ContainerDiscovery.PingAcknowledged(messageId), cancellationToken);
                IPublishedMessage<ContainerDiscovery.PingCompleted> pingCompleted = await harness.Published
                    .SelectAsync<ContainerDiscovery.PingCompleted>(
                        message => message.Context.Message.CorrelationId == messageId,
                        cancellationToken)
                    .First()
                    .WaitAsync(timeout, cancellationToken);
                ISagaTestHarness<ContainerDiscovery.DiscoveryPingSaga> sagaHarness =
                    harness.GetSagaHarness<ContainerDiscovery.DiscoveryPingSaga>();

                Assert.Equal(routingCorrelationId, slipCompleted.Context.Message.TrackingNumber);
                Assert.Equal(messageId, response.Message.CorrelationId);
                Assert.Equal(messageId, pingCompleted.Context.Message.CorrelationId);
                Assert.Equal(messageId, sagaHarness.Sagas.Contains(messageId).CorrelationId);
                Assert.Equal(messageId, machineHarness.Sagas.Contains(messageId).CorrelationId);
                Assert.All(
                    new[]
                    {
                        "DiscoveryPing",
                        "Ping_compensate",
                        "Ping_execute",
                        "PingSecond_execute",
                        "discovery-ping-state",
                        "ping-queue",
                    },
                    expected => Assert.Contains(expected, endpoints.Names));
                Assert.DoesNotContain("DiscoveryExcluded", endpoints.Names);
            }
            finally
            {
                await harness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
            }
        }

        private sealed class DiscoveryEndpointObserver : IEndpointConfigurationObserver
        {
            private readonly ConcurrentDictionary<string, byte> _names = new(StringComparer.Ordinal);

            public string[] Names => _names.Keys.ToArray();

            public void EndpointConfigured<T>(T configurator)
                where T : IReceiveEndpointConfigurator =>
                _names.TryAdd(configurator.InputAddress.AbsolutePath.Trim('/'), 0);
        }
    }
}
