using Amazon;
using ViciOne.ServiceBus.AmazonSqs.Configuration;
using ViciOne.ServiceBus.AmazonSqs.Tests.TestDoubles;
using ViciOne.ServiceBus.AmazonSqs.Topology;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.Tests;

public sealed class AmazonSqsSendTransportSelectionTests
{
    [Theory]
    [InlineData("queue:orders", true, "orders")]
    [InlineData("topic:events", false, "events")]
    [InlineData("amazonsqs://eu-central-1/orders?type=topic", false, "orders")]
    [RequirementCoverage("REQ-VSB-AWS-SQS-TOPOLOGY", "send-transport-queue-topology-lookup-follows-address-kind")]
    public async Task CreateSendTransport_SelectsQueueTopologyAndRegistersOwnedAgents(
        string destination, bool isQueue, string expectedName)
    {
        var registrations = new List<IAgent>();
        var queueSettingsRequests = 0;
        IAmazonSqsSendTopologyConfigurator sendTopology = InterfaceProxy<IAmazonSqsSendTopologyConfigurator>.Create((method, args) =>
            method.Name switch
            {
                nameof(IAmazonSqsSendTopology.GetSendSettings) => ResolveQueueSettings(args),
                _ => throw new NotSupportedException(method.Name)
            });
        IAmazonSqsTopologyConfiguration topology = InterfaceProxy<IAmazonSqsTopologyConfiguration>.Create((method, _) =>
            method.Name switch
            {
                "get_Send" => sendTopology,
                _ => throw new NotSupportedException(method.Name)
            });
        (ConnectionContextSupervisor supervisor, SqsReceiveEndpointContext endpoint) = CreateSupervisor(topology);
        IClientContextSupervisor parent = InterfaceProxy<IClientContextSupervisor>.Create((method, args) => method.Name switch
        {
            nameof(IClientContextSupervisor.AddSendAgent) => Register(args),
            _ => throw new NotSupportedException(method.Name)
        });

        ISendTransport transport = await supervisor.CreateSendTransportAsync(
            endpoint, parent, new Uri(destination), TestContext.Current.CancellationToken);

        Assert.Collection(registrations,
            agent => Assert.IsType<ClientContextSupervisor>(agent),
            agent => Assert.Same(transport, agent));
        Assert.Equal(isQueue ? 1 : 0, queueSettingsRequests);

        SendSettings ResolveQueueSettings(object?[]? args)
        {
            var address = Assert.IsType<AmazonSqsEndpointAddress>(Assert.Single(args!));
            Assert.Equal(AmazonSqsEndpointAddress.AddressType.Queue, address.Type);
            Assert.Equal(expectedName, address.Name);
            queueSettingsRequests++;
            return new QueueSendSettings(address);
        }

        object? Register(object?[]? args)
        {
            registrations.Add(Assert.IsAssignableFrom<IAgent>(Assert.Single(args!)));
            return null;
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-CANCELLATION", "canceled-transport-creation-does-not-register-agents")]
    public async Task CreateSendTransport_CanceledRequestDoesNotResolveAddressOrRegisterAgents()
    {
        var registrations = 0;
        (ConnectionContextSupervisor supervisor, SqsReceiveEndpointContext endpoint) = CreateSupervisor();
        IClientContextSupervisor parent = InterfaceProxy<IClientContextSupervisor>.Create((method, _) => method.Name switch
        {
            nameof(IClientContextSupervisor.AddSendAgent) => CountRegistration(),
            _ => throw new NotSupportedException(method.Name)
        });
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => supervisor.CreateSendTransportAsync(
            endpoint, parent, new Uri("https://invalid-for-sqs.example"), cancellation.Token));

        Assert.Equal(0, registrations);

        object? CountRegistration()
        {
            registrations++;
            return null;
        }
    }

    private static (ConnectionContextSupervisor Supervisor, SqsReceiveEndpointContext Endpoint) CreateSupervisor(
        IAmazonSqsTopologyConfiguration? sendTopology = null)
    {
        var topology = new AmazonSqsTopologyConfiguration(AmazonSqsBusFactory.CreateMessageTopology());
        var bus = new AmazonSqsBusConfiguration(topology);
        bus.HostConfiguration.Settings = new AmazonSqsHostSettings(
            RegionEndpoint.EUCentral1,
            null,
            false,
            new Uri("amazonsqs://eu-central-1/"),
            new AmazonSqsClientContextCacheOptions(),
            () => throw new InvalidOperationException("A transport must not open a provider connection during creation."),
            null);
        SqsReceiveEndpointContext endpoint = InterfaceProxy<SqsReceiveEndpointContext>.Create((method, _) => method.Name switch
        {
            "get_Serialization" => bus.Serialization.CreateSerializerCollection(),
            _ => throw new NotSupportedException(method.Name)
        });
        return (new ConnectionContextSupervisor(bus.HostConfiguration, sendTopology ?? topology), endpoint);
    }
}
