using System.Reflection;
using ViciOne.ServiceBus.Clients.Contexts;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Clients;

public sealed class ClientFactoryExtensionsBoundaryTests
{
    private static readonly Uri DestinationAddress = new("loopback://localhost/client-factory-boundary");

    [Fact]
    [RequirementCoverage("REQ-VSB-CLIENT-FACTORY-BOUNDARY", "every-advanced-entry-validates-required-inputs")]
    public void EveryAdvancedClientFactoryEntry_RejectsItsMissingRequiredInputBeforeUse()
    {
        IBus bus = DispatchProxy.Create<IBus, UnexpectedInvocationProxy>();
        ConsumeContext consumeContext = DispatchProxy.Create<ConsumeContext, UnexpectedInvocationProxy>();

        AssertParameter("bus", () => ClientFactoryExtensions.CreateRequestClient<BoundaryRequest>(null!, DestinationAddress));
        AssertParameter("destinationAddress", () => bus.CreateRequestClient<BoundaryRequest>(null!));
        AssertParameter("bus", () => ClientFactoryExtensions.CreateRequestClient<BoundaryRequest>(null!));

        AssertParameter("consumeContext", () =>
            ClientFactoryExtensions.CreateRequestClient<BoundaryRequest>(null!, bus, DestinationAddress));
        AssertParameter("bus", () => consumeContext.CreateRequestClient<BoundaryRequest>(null!, DestinationAddress));
        AssertParameter("destinationAddress", () => consumeContext.CreateRequestClient<BoundaryRequest>(bus, null!));
        AssertParameter("consumeContext", () =>
            ClientFactoryExtensions.CreateRequestClient<BoundaryRequest>(null!, bus));
        AssertParameter("bus", () => consumeContext.CreateRequestClient<BoundaryRequest>(null!));

        AssertParameter("bus", () => ClientFactoryExtensions.CreateClientFactory((IBus)null!));
        AssertParameter("receiveEndpointHandle", () =>
            ClientFactoryExtensions.CreateClientFactory((IHostReceiveEndpointHandle)null!));
        AssertParameter("connector", () => ClientFactoryExtensions.CreateClientFactory((IReceiveConnector)null!));
        AssertParameter("connector", () => ClientFactoryExtensions.ConnectClientFactory(null!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CLIENT-FACTORY", "advanced-factories-preserve-context-timeout-endpoint-kind-and-ownership")]
    public async Task AdvancedClientFactories_PreserveTheirContextTimeoutEndpointKindAndOwnershipAsync()
    {
        var timeout = new RequestTimeout(TimeSpan.FromSeconds(41));
        IBus bus = DispatchProxy.Create<IBus, UnexpectedInvocationProxy>();

        await using (IClientFactory busFactory = bus.CreateClientFactory(timeout))
        {
            var context = Assert.IsType<BusClientFactoryContext>(busFactory.Context);
            Assert.Equal(timeout, context.DefaultTimeout);
        }

        var directHandle = new RecordingHostReceiveEndpointHandle(
            CreateReceiveEndpoint(new Uri("loopback://localhost/direct-client-factory")));
        IClientFactory directFactory = directHandle.CreateClientFactory(timeout);
        var directContext = Assert.IsType<HostReceiveEndpointClientFactoryContext>(directFactory.Context);

        Assert.Equal(timeout, directContext.DefaultTimeout);
        Assert.Equal(directHandle.ReceiveEndpoint.InputAddress, directContext.ResponseAddress);
        await directFactory.DisposeAsync();
        await directFactory.DisposeAsync();
        Assert.Equal(1, directHandle.StopCount);

        var responseHandle = new RecordingHostReceiveEndpointHandle(
            CreateReceiveEndpoint(new Uri("loopback://localhost/response-client-factory")));
        IReceiveConnector responseConnector = CreateConnector(responseHandle, out RecordingConnectorProxy responseProxy);
        await using (IClientFactory responseFactory = responseConnector.CreateClientFactory(timeout))
        {
            var responseContext = Assert.IsType<HostReceiveEndpointClientFactoryContext>(responseFactory.Context);
            Assert.IsType<ResponseEndpointDefinition>(responseProxy.Definition);
            Assert.Null(responseProxy.EndpointNameFormatter);
            Assert.Null(responseProxy.ConfigureEndpoint);
            Assert.Equal(timeout, responseContext.DefaultTimeout);
            Assert.Equal(responseHandle.ReceiveEndpoint.InputAddress, responseContext.ResponseAddress);
        }
        Assert.Equal(1, responseHandle.StopCount);

        var temporaryHandle = new RecordingHostReceiveEndpointHandle(
            CreateReceiveEndpoint(new Uri("loopback://localhost/temporary-client-factory")));
        IReceiveConnector temporaryConnector = CreateConnector(temporaryHandle, out RecordingConnectorProxy temporaryProxy);
        await using (IClientFactory temporaryFactory = temporaryConnector.ConnectClientFactory(timeout))
        {
            var temporaryContext = Assert.IsType<HostReceiveEndpointClientFactoryContext>(temporaryFactory.Context);
            Assert.IsType<TemporaryEndpointDefinition>(temporaryProxy.Definition);
            Assert.Same(KebabCaseEndpointNameFormatter.Instance, temporaryProxy.EndpointNameFormatter);
            Assert.Null(temporaryProxy.ConfigureEndpoint);
            Assert.Equal(timeout, temporaryContext.DefaultTimeout);
            Assert.Equal(temporaryHandle.ReceiveEndpoint.InputAddress, temporaryContext.ResponseAddress);
        }
        Assert.Equal(1, temporaryHandle.StopCount);
    }

    private static void AssertParameter(string expectedParameterName, Action operation)
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(operation);
        Assert.Equal(expectedParameterName, exception.ParamName);
    }

    private static IReceiveConnector CreateConnector(
        IHostReceiveEndpointHandle handle,
        out RecordingConnectorProxy proxy)
    {
        IReceiveConnector connector = DispatchProxy.Create<IReceiveConnector, RecordingConnectorProxy>();
        proxy = (RecordingConnectorProxy)(object)connector;
        proxy.Handle = handle;
        return connector;
    }

    private static IReceiveEndpoint CreateReceiveEndpoint(Uri inputAddress)
    {
        IReceiveEndpoint endpoint = DispatchProxy.Create<IReceiveEndpoint, RecordingReceiveEndpointProxy>();
        ((RecordingReceiveEndpointProxy)(object)endpoint).InputAddress = inputAddress;
        return endpoint;
    }

    private sealed record BoundaryRequest;

    private class UnexpectedInvocationProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"The client-factory boundary invoked {targetMethod?.Name}.");
    }

    private sealed class RecordingHostReceiveEndpointHandle(IReceiveEndpoint receiveEndpoint) : IHostReceiveEndpointHandle
    {
        public IReceiveEndpoint ReceiveEndpoint { get; } = receiveEndpoint;

        public Task<ReceiveEndpointReady> Ready { get; } = Task.FromResult<ReceiveEndpointReady>(null!);

        public int StopCount { get; private set; }

        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            StopCount++;
            return Task.CompletedTask;
        }
    }

    private class RecordingConnectorProxy : DispatchProxy
    {
        public IHostReceiveEndpointHandle? Handle { get; set; }

        public IEndpointDefinition? Definition { get; private set; }

        public IEndpointNameFormatter? EndpointNameFormatter { get; private set; }

        public Action<IReceiveEndpointConfigurator>? ConfigureEndpoint { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name == nameof(IReceiveConnector.ConnectReceiveEndpoint)
                && args is { Length: 3 }
                && args[0] is IEndpointDefinition definition)
            {
                Definition = definition;
                EndpointNameFormatter = args[1] as IEndpointNameFormatter;
                ConfigureEndpoint = args[2] as Action<IReceiveEndpointConfigurator>;
                return Handle ?? throw new InvalidOperationException("The connector handle was not configured.");
            }

            throw new InvalidOperationException($"The connector invoked unexpected member {targetMethod.Name}.");
        }
    }

    private class RecordingReceiveEndpointProxy : DispatchProxy
    {
        public Uri? InputAddress { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            return targetMethod.Name == "get_InputAddress"
                ? InputAddress
                : throw new InvalidOperationException($"The receive endpoint invoked unexpected member {targetMethod.Name}.");
        }
    }
}
