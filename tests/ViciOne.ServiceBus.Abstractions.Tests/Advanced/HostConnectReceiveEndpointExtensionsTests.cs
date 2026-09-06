using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Advanced;

public sealed class HostConnectReceiveEndpointExtensionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-HOST-ENDPOINT-CONNECTION", "response-endpoint-forwarding")]
    public void ConnectResponseEndpoint_ForwardsTheDefinitionFormatterAndCallback()
    {
        (IReceiveConnector connector, ReceiveConnectorProxy proxy) = CreateConnector();
        IEndpointNameFormatter formatter = DispatchProxy.Create<IEndpointNameFormatter, EndpointNameFormatterProxy>();
        Action<IReceiveEndpointConfigurator> configure = _ => { };

        HostReceiveEndpointHandle result = connector.ConnectResponseEndpoint(formatter, configure);

        Assert.Same(proxy.Handle, result);
        Assert.IsType<ResponseEndpointDefinition>(proxy.Definition);
        Assert.Same(formatter, proxy.EndpointNameFormatter);
        Assert.Same(configure, proxy.ConfigureEndpoint);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-HOST-ENDPOINT-CONNECTION", "temporary-endpoint-forwarding")]
    public void ConnectReceiveEndpoint_ForwardsATemporaryDefinitionAndCallback()
    {
        (IReceiveConnector connector, ReceiveConnectorProxy proxy) = CreateConnector();
        Action<IReceiveEndpointConfigurator> configure = _ => { };

        HostReceiveEndpointHandle result = connector.ConnectReceiveEndpoint(configure);

        Assert.Same(proxy.Handle, result);
        Assert.IsType<TemporaryEndpointDefinition>(proxy.Definition);
        Assert.Null(proxy.EndpointNameFormatter);
        Assert.Same(configure, proxy.ConfigureEndpoint);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-HOST-ENDPOINT-CONNECTION", "required-connector")]
    public void ConvenienceEndpoints_RejectAMissingConnector()
    {
        IReceiveConnector? connector = null;

        Assert.Equal(
            "connector",
            Assert.Throws<ArgumentNullException>(() => connector!.ConnectResponseEndpoint()).ParamName);
        Assert.Equal(
            "connector",
            Assert.Throws<ArgumentNullException>(() => connector!.ConnectReceiveEndpoint()).ParamName);
    }

    private static (IReceiveConnector Connector, ReceiveConnectorProxy Proxy) CreateConnector()
    {
        IReceiveConnector connector = DispatchProxy.Create<IReceiveConnector, ReceiveConnectorProxy>();
        return (connector, (ReceiveConnectorProxy)(object)connector);
    }

    private class ReceiveConnectorProxy : DispatchProxy
    {
        public IEndpointDefinition? Definition { get; private set; }

        public IEndpointNameFormatter? EndpointNameFormatter { get; private set; }

        public Action<IReceiveEndpointConfigurator>? ConfigureEndpoint { get; private set; }

        public HostReceiveEndpointHandle Handle { get; } =
            DispatchProxy.Create<HostReceiveEndpointHandle, HostReceiveEndpointHandleProxy>();

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(IReceiveConnector.ConnectReceiveEndpoint)
                && args is { Length: 3 }
                && args[0] is IEndpointDefinition definition)
            {
                Definition = definition;
                EndpointNameFormatter = (IEndpointNameFormatter?)args[1];
                ConfigureEndpoint = (Action<IReceiveEndpointConfigurator>?)args[2];
                return Handle;
            }

            throw new NotSupportedException($"Unexpected receive connector member: {targetMethod?.Name}");
        }
    }

    private class EndpointNameFormatterProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException($"Unexpected endpoint name formatter member: {targetMethod?.Name}");
    }

    private class HostReceiveEndpointHandleProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException($"Unexpected endpoint handle member: {targetMethod?.Name}");
    }
}
