using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.AmazonSqs.Configuration;
using ViciOne.ServiceBus.AmazonSqs.Tests.TestDoubles;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.Tests;

public sealed class AmazonSqsConsumeTopologyConnectionTests
{
    [Theory]
    [InlineData(false, ConnectPipeOptions.ConfigureConsumeTopology, true, false, false)]
    [InlineData(true, 0, true, false, false)]
    [InlineData(true, ConnectPipeOptions.ConfigureConsumeTopology, false, false, false)]
    [InlineData(true, ConnectPipeOptions.ConfigureConsumeTopology, true, true, false)]
    [InlineData(true, ConnectPipeOptions.ConfigureConsumeTopology, true, true, true)]
    [RequirementCoverage("REQ-VSB-AWS-SQS-CONSUME-TOPOLOGY", "subscription-requires-endpoint-option-and-message-consent")]
    public void ConnectConsumePipe_SubscribesOnlyWhenAllTopologyFlagsAllowIt(
        bool endpointEnabled, ConnectPipeOptions options, bool messageEnabled, bool shouldSubscribe, bool useDefaultOverload)
    {
        int topologyLookups = 0;
        int subscriptions = 0;
        int pipeConnections = 0;
        var handle = new EmptyConnectHandle();
        IPipe<ConsumeContext<Contract>> consumerPipe = Pipe.Empty<ConsumeContext<Contract>>();
        IAmazonSqsMessageConsumeTopologyConfigurator<Contract> messageTopology =
            InterfaceProxy<IAmazonSqsMessageConsumeTopologyConfigurator<Contract>>.Create((method, _) => method.Name switch
            {
                "get_ConfigureConsumeTopology" => messageEnabled,
                nameof(IAmazonSqsMessageConsumeTopologyConfigurator<Contract>.Subscribe) => RecordSubscription(),
                _ => throw new NotSupportedException(method.Name)
            });
        IAmazonSqsConsumeTopologyConfigurator consumeTopology =
            InterfaceProxy<IAmazonSqsConsumeTopologyConfigurator>.Create((method, _) => method.Name switch
            {
                nameof(IAmazonSqsConsumeTopologyConfigurator.GetMessageTopology) => ResolveMessageTopology(method.GetGenericArguments()[0]),
                _ => throw new NotSupportedException(method.Name)
            });
        IAmazonSqsTopologyConfiguration topology = InterfaceProxy<IAmazonSqsTopologyConfiguration>.Create((method, _) => method.Name switch
        {
            "get_Consume" => consumeTopology,
            _ => throw new NotSupportedException(method.Name)
        });
        IConsumePipe consumePipe = InterfaceProxy<IConsumePipe>.Create((method, args) => method.Name switch
        {
            nameof(IConsumePipeConnector.ConnectConsumePipe) => ConnectPipe(method.GetGenericArguments()[0], args),
            _ => throw new NotSupportedException(method.Name)
        });
        IAmazonSqsReceiveEndpointConfiguration configuration =
            InterfaceProxy<IAmazonSqsReceiveEndpointConfiguration>.Create((method, _) => method.Name switch
            {
                "get_ConfigureConsumeTopology" => endpointEnabled,
                "get_Topology" => topology,
                "get_ConsumePipe" => consumePipe,
                _ => throw new NotSupportedException(method.Name)
            });
        IAmazonSqsHostConfiguration host = InterfaceProxy<IAmazonSqsHostConfiguration>.Create((method, _) =>
            throw new NotSupportedException(method.Name));
        var builder = new AmazonSqsReceiveEndpointBuilder(host, configuration);

        ConnectHandle connected = useDefaultOverload
            ? builder.ConnectConsumePipe(consumerPipe)
            : builder.ConnectConsumePipe(consumerPipe, options);

        Assert.Same(handle, connected);
        Assert.Equal(1, pipeConnections);
        Assert.Equal(endpointEnabled && options.HasFlag(ConnectPipeOptions.ConfigureConsumeTopology) ? 1 : 0, topologyLookups);
        Assert.Equal(shouldSubscribe ? 1 : 0, subscriptions);

        object? RecordSubscription()
        {
            subscriptions++;
            return null;
        }

        object ResolveMessageTopology(Type messageType)
        {
            Assert.Equal(typeof(Contract), messageType);
            topologyLookups++;
            return messageTopology;
        }

        ConnectHandle ConnectPipe(Type messageType, object?[]? arguments)
        {
            Assert.Equal(typeof(Contract), messageType);
            Assert.Same(consumerPipe, Assert.Single(arguments!));
            pipeConnections++;
            return handle;
        }
    }

    private sealed class Contract { }
}
