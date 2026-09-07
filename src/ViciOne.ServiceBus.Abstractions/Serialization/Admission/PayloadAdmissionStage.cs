namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>The serialized boundary at which payload admission failed.</summary>
public enum PayloadAdmissionStage
{
    /// <summary>The serialized application body.</summary>
    SerializedBody = 0,

    /// <summary>The externally stored MessageData object.</summary>
    MessageData = 1,

    /// <summary>The final serialized transport envelope.</summary>
    TransportEnvelope = 2,
}
