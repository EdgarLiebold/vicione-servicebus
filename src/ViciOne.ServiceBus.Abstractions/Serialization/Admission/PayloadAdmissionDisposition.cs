namespace ViciOne.ServiceBus.Serialization;

/// <summary>The action selected for an admitted serialized application body.</summary>
public enum PayloadAdmissionDisposition
{
    /// <summary>Keep the serialized body in the transport envelope.</summary>
    Inline = 0,

    /// <summary>Use the bus-owned MessageData facility rather than an inline body.</summary>
    OffloadToMessageData = 1,
}
