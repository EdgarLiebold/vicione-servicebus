namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Associates a registered bus identity with its payload-admission runtime.</summary>
internal interface IPayloadAdmissionRuntimeRegistration
{
    string BusKey { get; }

    IPayloadAdmissionRuntime Runtime { get; }
}
