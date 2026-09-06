namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>The serialized boundary at which payload admission failed.</summary>
public enum PayloadAdmissionStage
{
    /// <summary>Indicates serialized body.</summary>
    SerializedBody = 0,

    /// <summary>Indicates message data.</summary>
    MessageData = 1,

    /// <summary>Indicates transport envelope.</summary>
    TransportEnvelope = 2,
}
