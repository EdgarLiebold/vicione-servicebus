#nullable enable

namespace ViciOne.ServiceBus.Serialization;

using System;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.MessageData;

/// <summary>
/// Non-generic bridge kept inside the runtime so the transport serialization owner does not lose the
/// typed-bus policy boundary.
/// </summary>
internal interface IPayloadAdmissionRuntime
{
    IPayloadSerializationBuffer CreateSerializedBodyBuffer();

    PayloadAdmissionResult EvaluateSerializedBody(ReadOnlyMemory<byte> serializedBody, bool messageDataOffloadObserved);

    IPayloadSerializationBuffer CreateTransportEnvelopeBuffer();

    void ValidateTransportEnvelope(ReadOnlyMemory<byte> serializedEnvelope);
}

internal sealed class PayloadAdmissionRuntime<TBus> : IPayloadAdmissionRuntime
    where TBus : class, IBus
{
    readonly IPayloadAdmissionEvaluator<TBus> _evaluator;

    public PayloadAdmissionRuntime(IPayloadAdmissionEvaluator<TBus> evaluator)
    {
        _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
    }

    public IPayloadSerializationBuffer CreateSerializedBodyBuffer() => _evaluator.CreateSerializedBodyBuffer();

    public PayloadAdmissionResult EvaluateSerializedBody(ReadOnlyMemory<byte> serializedBody, bool messageDataOffloadObserved)
        => _evaluator.EvaluateSerializedBody(serializedBody, messageDataOffloadObserved);

    public IPayloadSerializationBuffer CreateTransportEnvelopeBuffer() => _evaluator.CreateTransportEnvelopeBuffer();

    public void ValidateTransportEnvelope(ReadOnlyMemory<byte> serializedEnvelope)
        => _evaluator.ValidateTransportEnvelope(serializedEnvelope);
}

internal sealed class PayloadAdmissionSerializationContext
{
    public PayloadAdmissionSerializationContext(IPayloadAdmissionRuntime runtime, bool messageDataOffloadObserved)
    {
        Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        MessageDataOffloadObserved = messageDataOffloadObserved;
    }

    public IPayloadAdmissionRuntime Runtime { get; }

    public bool MessageDataOffloadObserved { get; }
}

internal interface IPayloadAdmissionHostConfiguration
{
    IPayloadAdmissionRuntime? PayloadAdmissionRuntime { get; }

    void SetPayloadAdmissionRuntime(IPayloadAdmissionRuntime runtime);
}

internal interface IPayloadAdmissionRuntimeRegistration
{
    string BusKey { get; }

    IPayloadAdmissionRuntime Runtime { get; }
}

internal sealed class PayloadAdmissionRuntimeRegistration<TBus> : IPayloadAdmissionRuntimeRegistration
    where TBus : class, IBus
{
    public PayloadAdmissionRuntimeRegistration(PayloadAdmissionRuntime<TBus> runtime)
    {
        Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
    }

    public string BusKey { get; } = BusRegistrationIdentity.GetKey(typeof(TBus));

    public IPayloadAdmissionRuntime Runtime { get; }
}

/// <summary>Common physical transport boundary used by both send and publish paths.</summary>
internal static class PayloadAdmissionTransportBoundary
{
    public static void Apply<T>(IHostConfiguration hostConfiguration, SendContext<T> context)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(hostConfiguration);
        ArgumentNullException.ThrowIfNull(context);

        if (hostConfiguration is not IPayloadAdmissionHostConfiguration { PayloadAdmissionRuntime: { } runtime })
            return;

        bool messageDataOffloadObserved = context.TryGetPayload(out MessageDataAdmissionEvidence? evidence)
            && evidence.HasStoredReference;

        context.GetOrAddPayload(() => new PayloadAdmissionSerializationContext(runtime, messageDataOffloadObserved));

        if (context is not TransportSendContext transportContext)
        {
            throw new ConfigurationException(
                "Payload admission requires a transport send context at the provider boundary.");
        }

        _ = transportContext.Body.GetBytes();
    }
}
