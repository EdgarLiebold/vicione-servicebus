using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Registers the payload-admission runtime under the stable identity of a typed bus.</summary>
/// <typeparam name="TBus">The registered bus type.</typeparam>
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
