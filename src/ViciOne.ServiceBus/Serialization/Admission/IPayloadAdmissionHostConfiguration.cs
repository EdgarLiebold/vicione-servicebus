namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Exposes the payload-admission runtime attached to a transport host.</summary>
internal interface IPayloadAdmissionHostConfiguration
{
    IPayloadAdmissionRuntime? PayloadAdmissionRuntime { get; }

    void SetPayloadAdmissionRuntime(IPayloadAdmissionRuntime runtime);
}
