using System;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Associates one send operation with its payload-admission runtime and offload evidence.</summary>
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
