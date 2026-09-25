using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Providers.Persistence;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.DurableSend;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DurableSend;

public sealed class TypedDurableSenderConfigurationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-TYPED-API", "non-transport-endpoint-rejected-before-admission")]
    public async Task EndpointWithoutTransportCapability_FailsBeforeDurableAdmissionAsync()
    {
        ISendEndpoint endpoint = DispatchProxy.Create<ISendEndpoint, UnexpectedEndpointProxy>();
        IBus bus = DispatchProxy.Create<IBus, EndpointBusProxy>();
        var busProxy = (EndpointBusProxy)(object)bus;
        busProxy.Endpoint = endpoint;
        IMessageContractCatalog catalog = new MessageContractCatalogBuilder()
            .Register<ConfigurationMessage>("vicione.tests.durable-transport-capability")
            .Build();
        var admission = new RecordingAdmission();
        IDurableSender<IBus> sender = DurableSenderTestFactory.CreateTypedSender(bus, catalog, admission);
        var destination = new Uri("loopback://durable-transport-capability/input");
        var options = new DurableSendOptions { IdempotencyKey = new DurableSendId(Guid.NewGuid()) };
        using var cancellation = new CancellationTokenSource();

        ConfigurationException failure = await Assert.ThrowsAsync<ConfigurationException>(() =>
            sender.SendAsync(destination, new ConfigurationMessage("not persisted"), options, cancellation.Token));

        Assert.Contains("canonical transport send-context", failure.Message, StringComparison.Ordinal);
        Assert.Equal(destination, busProxy.ResolvedAddress);
        Assert.Equal(cancellation.Token, busProxy.ResolutionToken);
        Assert.Equal(1, busProxy.ResolutionCalls);
        Assert.Equal(0, admission.AdmissionCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-TYPED-API", "noncanonical-send-context-rejected-before-admission")]
    public async Task EndpointReturningNoncanonicalContext_FailsBeforeDurableAdmissionAsync()
    {
        SendContext<ConfigurationMessage> context =
            DispatchProxy.Create<SendContext<ConfigurationMessage>, UnexpectedContextProxy>();
        ITransportSendEndpoint endpoint = DispatchProxy.Create<ITransportSendEndpoint, ContextEndpointProxy>();
        var endpointProxy = (ContextEndpointProxy)(object)endpoint;
        endpointProxy.Context = context;
        IBus bus = DispatchProxy.Create<IBus, EndpointBusProxy>();
        ((EndpointBusProxy)(object)bus).Endpoint = endpoint;
        IMessageContractCatalog catalog = new MessageContractCatalogBuilder()
            .Register<ConfigurationMessage>("vicione.tests.durable-context-capability")
            .Build();
        var admission = new RecordingAdmission();
        IDurableSender<IBus> sender = DurableSenderTestFactory.CreateTypedSender(bus, catalog, admission);
        var message = new ConfigurationMessage("not persisted");
        var options = new DurableSendOptions { IdempotencyKey = new DurableSendId(Guid.NewGuid()) };
        using var cancellation = new CancellationTokenSource();

        ConfigurationException failure = await Assert.ThrowsAsync<ConfigurationException>(() =>
            sender.SendAsync(new Uri("loopback://durable-context-capability/input"), message, options,
                cancellation.Token));

        Assert.Contains("cannot fix deterministic durable-admission metadata", failure.Message, StringComparison.Ordinal);
        Assert.Same(message, endpointProxy.CreatedMessage);
        Assert.Equal(cancellation.Token, endpointProxy.CreationToken);
        Assert.Equal(1, endpointProxy.CreationCalls);
        Assert.Equal(0, admission.AdmissionCalls);
    }

    public sealed record ConfigurationMessage(string Value);

    public class EndpointBusProxy : DispatchProxy
    {
        public ISendEndpoint Endpoint { get; set; } = null!;
        public Uri? ResolvedAddress { get; private set; }
        public CancellationToken ResolutionToken { get; private set; }
        public int ResolutionCalls { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name != nameof(ISendEndpointProvider.GetSendEndpointAsync) || args is not { Length: 2 })
                throw new InvalidOperationException($"Unexpected bus operation '{targetMethod?.Name}'.");

            ResolutionCalls++;
            ResolvedAddress = Assert.IsType<Uri>(args[0]);
            ResolutionToken = Assert.IsType<CancellationToken>(args[1]);
            return Task.FromResult(Endpoint);
        }
    }

    public class UnexpectedEndpointProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
            => throw new InvalidOperationException($"Unexpected endpoint operation '{targetMethod?.Name}'.");
    }

    public class ContextEndpointProxy : DispatchProxy
    {
        public SendContext<ConfigurationMessage> Context { get; set; } = null!;
        public ConfigurationMessage? CreatedMessage { get; private set; }
        public CancellationToken CreationToken { get; private set; }
        public int CreationCalls { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name != nameof(ITransportSendEndpoint.CreateSendContextAsync)
                || args is not { Length: 3 }
                || targetMethod.GetGenericArguments()[0] != typeof(ConfigurationMessage))
                throw new InvalidOperationException($"Unexpected transport endpoint operation '{targetMethod?.Name}'.");

            CreationCalls++;
            CreatedMessage = Assert.IsType<ConfigurationMessage>(args[0]);
            CreationToken = Assert.IsType<CancellationToken>(args[2]);
            return Task.FromResult(Context);
        }
    }

    public class UnexpectedContextProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
            => throw new InvalidOperationException($"Unexpected send context operation '{targetMethod?.Name}'.");
    }

    private sealed class RecordingAdmission : IDurableSendAdmission<IBus>
    {
        public int AdmissionCalls { get; private set; }

        public Task<DurableSendAdmissionResult> AdmitAsync(
            SerializedDurableSend message,
            CancellationToken cancellationToken = default)
        {
            AdmissionCalls++;
            throw new InvalidOperationException("An endpoint without transport capability must not reach durable admission.");
        }
    }
}
