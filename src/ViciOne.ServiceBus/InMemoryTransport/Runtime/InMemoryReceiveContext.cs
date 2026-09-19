using System.Collections.Generic;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.InMemoryTransport.Runtime;

/// <summary>Represents a received message on the in-memory transport.</summary>
internal sealed class InMemoryReceiveContext :
    BaseReceiveContext,
    RoutingKeyConsumeContext,
    TransportReceiveContext
{
    readonly Lazy<MessageBody> _body;
    readonly InMemoryTransportMessage _message;
    readonly IInMemoryReceiveEndpointContext _receiveEndpointContext;

    /// <summary>Creates a receive context for a transport message and its owning endpoint.</summary>
    /// <param name="message">The received transport message.</param>
    /// <param name="receiveEndpointContext">The endpoint that received the message.</param>
    public InMemoryReceiveContext(InMemoryTransportMessage message, IInMemoryReceiveEndpointContext receiveEndpointContext)
        : base(
            (message ?? throw new ArgumentNullException(nameof(message))).DeliveryCount > 0,
            receiveEndpointContext ?? throw new ArgumentNullException(nameof(receiveEndpointContext)),
            GetPayloads(message))
    {
        _message = message;
        _receiveEndpointContext = receiveEndpointContext;

        _body = new Lazy<MessageBody>(CreateBody);
    }

    /// <summary>Gets the provider for the message's transport headers.</summary>
    protected override IHeaderProvider HeaderProvider => new DictionarySendHeaderProvider(_message.Headers);

    /// <summary>Gets the message body after applying the configured size limits.</summary>
    public override MessageBody Body => EnforceMessageLimits(_body.Value);

    /// <summary>Gets the routing key carried by the transport message.</summary>
    public string? RoutingKey => _message.RoutingKey;

    /// <summary>Gets the OpenTelemetry messaging-system identifier.</summary>
    public string ActivitySystem => "in-memory";

    /// <summary>Captures the routing key for message scheduling and replay.</summary>
    /// <returns>The transport-property bag, or <see langword="null"/> when no routing key is present.</returns>
    public IDictionary<string, object>? GetTransportProperties()
    {
        return string.IsNullOrWhiteSpace(RoutingKey)
            ? null
            : new Dictionary<string, object>
            {
                [InMemoryTransportPropertyNames.RoutingKey] = RoutingKey,
            };
    }

    MessageBody CreateBody()
    {
        InMemoryPayloadAdmissionProof? proof = _message.PayloadAdmissionProof;
        IPayloadAdmissionRuntime? runtime = _receiveEndpointContext.PayloadAdmissionRuntime;
        if (runtime is null)
        {
            if (proof is not null)
                throw new InvalidOperationException("The in-memory receive endpoint has no runtime for the admitted transport envelope.");

            return new BinaryMessageBody(_message.Body);
        }

        if (proof is not null && !ReferenceEquals(proof.OwnerRuntime, runtime))
            throw new InvalidOperationException("The in-memory transport envelope was admitted by a different bus.");

        var admission = new PayloadAdmissionSerializationContext(
            runtime,
            proof?.Proof.MessageDataOffloadObserved ?? false);
        return AdmittedCopyMessageBody.Create(
            _message.Body.AsMemory(),
            ContentType,
            _receiveEndpointContext.Serialization,
            admission,
            proof?.Proof);
    }

    static object[] GetPayloads(InMemoryTransportMessage message)
        => message.DurableSendContext is { } durableSendContext ? [durableSendContext] : [];
}
