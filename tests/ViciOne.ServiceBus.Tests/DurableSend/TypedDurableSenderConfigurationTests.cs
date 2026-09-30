using System.Net.Mime;
using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Serialization;
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-DURABLE-TYPED-API", "serializer-cannot-change-durable-message-identity")]
    public async Task SerializerChangingMessageId_RejectsBeforeDurableAdmissionAsync(bool clearId)
    {
        var destination = new Uri("loopback://durable-identity/input");
        var message = new ConfigurationMessage("unchanged intent");
        var options = new DurableSendOptions { IdempotencyKey = new DurableSendId(Guid.NewGuid()) };
        Guid replacementId = clearId ? Guid.Empty : Guid.NewGuid();
        var context = new MessageSendContext<ConfigurationMessage>(message)
        {
            DestinationAddress = destination,
            Serializer = new MessageIdChangingSerializer(replacementId),
        };
        ITransportSendEndpoint endpoint = DispatchProxy.Create<ITransportSendEndpoint, ContextEndpointProxy>();
        ((ContextEndpointProxy)(object)endpoint).Context = context;
        IBus bus = DispatchProxy.Create<IBus, EndpointBusProxy>();
        ((EndpointBusProxy)(object)bus).Endpoint = endpoint;
        IMessageContractCatalog catalog = new MessageContractCatalogBuilder()
            .Register<ConfigurationMessage>("vicione.tests.durable-serializer-identity")
            .Build();
        var admission = new RecordingAdmission();
        IDurableSender<IBus> sender = DurableSenderTestFactory.CreateTypedSender(bus, catalog, admission);

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sender.SendAsync(destination, message, options, TestContext.Current.CancellationToken));

        Assert.Contains("MessageId changed during serialization", failure.Message, StringComparison.Ordinal);
        Assert.Equal(replacementId, context.MessageId);
        Assert.Equal(0, admission.AdmissionCalls);
        Assert.Equal(options.IdempotencyKey.Value, context.ConversationId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-DURABLE-TYPED-API", "serializer-cannot-change-durable-content-type")]
    public async Task SerializerChangingContentType_RejectsBeforeDurableAdmissionAsync(bool mutateDuringBodyRead)
    {
        var destination = new Uri("loopback://durable-content-type/input");
        var message = new ConfigurationMessage("stable content type");
        var options = new DurableSendOptions { IdempotencyKey = new DurableSendId(Guid.NewGuid()) };
        var context = new MessageSendContext<ConfigurationMessage>(message)
        {
            DestinationAddress = destination,
            Serializer = new ContentTypeChangingSerializer(mutateDuringBodyRead),
        };
        ITransportSendEndpoint endpoint = DispatchProxy.Create<ITransportSendEndpoint, ContextEndpointProxy>();
        ((ContextEndpointProxy)(object)endpoint).Context = context;
        IBus bus = DispatchProxy.Create<IBus, EndpointBusProxy>();
        ((EndpointBusProxy)(object)bus).Endpoint = endpoint;
        IMessageContractCatalog catalog = new MessageContractCatalogBuilder()
            .Register<ConfigurationMessage>("vicione.tests.durable-serializer-content-type")
            .Build();
        var admission = new RecordingAdmission();
        IDurableSender<IBus> sender = DurableSenderTestFactory.CreateTypedSender(bus, catalog, admission);

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sender.SendAsync(destination, message, options, TestContext.Current.CancellationToken));

        Assert.Contains("ContentType changed during serialization", failure.Message, StringComparison.Ordinal);
        Assert.Equal("application/json", context.ContentType?.ToString());
        Assert.Equal(0, admission.AdmissionCalls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-DURABLE-TYPED-API", "throwing-serializer-restores-durable-content-type")]
    public async Task ThrowingSerializerChangingContentType_PreservesFaultAndRestoresContextAsync(bool throwDuringBodyRead)
    {
        var destination = new Uri("loopback://durable-throwing-content-type/input");
        var message = new ConfigurationMessage("original fault");
        var options = new DurableSendOptions { IdempotencyKey = new DurableSendId(Guid.NewGuid()) };
        var expectedFailure = new ExpectedSerializationException();
        var context = new MessageSendContext<ConfigurationMessage>(message)
        {
            DestinationAddress = destination,
            Serializer = new ThrowingContentTypeChangingSerializer(throwDuringBodyRead, expectedFailure),
        };
        ITransportSendEndpoint endpoint = DispatchProxy.Create<ITransportSendEndpoint, ContextEndpointProxy>();
        ((ContextEndpointProxy)(object)endpoint).Context = context;
        IBus bus = DispatchProxy.Create<IBus, EndpointBusProxy>();
        ((EndpointBusProxy)(object)bus).Endpoint = endpoint;
        IMessageContractCatalog catalog = new MessageContractCatalogBuilder()
            .Register<ConfigurationMessage>("vicione.tests.durable-throwing-content-type")
            .Build();
        var admission = new RecordingAdmission();
        IDurableSender<IBus> sender = DurableSenderTestFactory.CreateTypedSender(bus, catalog, admission);

        ExpectedSerializationException actual = await Assert.ThrowsAsync<ExpectedSerializationException>(() =>
            sender.SendAsync(destination, message, options, TestContext.Current.CancellationToken));

        Assert.Same(expectedFailure, actual);
        Assert.Equal("application/json", context.ContentType?.ToString());
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

    private sealed class MessageIdChangingSerializer(Guid replacementId) : IMessageSerializer
    {
        public ContentType ContentType { get; } = new("application/json");

        public MessageBody GetMessageBody<T>(SendContext<T> context) where T : class
        {
            context.MessageId = replacementId;
            return new StringMessageBody("{}");
        }
    }

    private sealed class ContentTypeChangingSerializer(bool mutateDuringBodyRead) : IMessageSerializer
    {
        public ContentType ContentType { get; } = new("application/json");

        public MessageBody GetMessageBody<T>(SendContext<T> context) where T : class
        {
            if (!mutateDuringBodyRead)
                context.ContentType = new ContentType("application/octet-stream");
            return mutateDuringBodyRead
                ? new DeferredContentTypeChangingBody(() => context.ContentType = new ContentType("application/octet-stream"))
                : new StringMessageBody("{}");
        }
    }

    private sealed class ThrowingContentTypeChangingSerializer(bool throwDuringBodyRead, Exception failure) : IMessageSerializer
    {
        public ContentType ContentType { get; } = new("application/json");

        public MessageBody GetMessageBody<T>(SendContext<T> context) where T : class
        {
            if (!throwDuringBodyRead)
            {
                context.ContentType = new ContentType("application/octet-stream");
                throw failure;
            }

            return new DeferredContentTypeChangingBody(() =>
            {
                context.ContentType = new ContentType("application/octet-stream");
                throw failure;
            });
        }
    }

    private sealed class ExpectedSerializationException : Exception;

    private sealed class DeferredContentTypeChangingBody(Action mutate) : MessageBody
    {
        public long Length => 2;

        public byte[] ToArray()
        {
            mutate();
            return "{}"u8.ToArray();
        }

        public Stream OpenReadStream() => new MemoryStream(ToArray(), writable: false);

        public bool TryGetTransportText([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? text)
        {
            text = "{}";
            return true;
        }
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
