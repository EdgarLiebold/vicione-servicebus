namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Identifies a serializer-owned body admitted for the current send context.</summary>
internal interface IPayloadAdmittedMessageBody
{
    PayloadAdmissionSerializationContext? AdmissionContext { get; }
}
