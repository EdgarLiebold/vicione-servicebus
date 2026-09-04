namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>The serialized boundary at which payload admission failed.</summary>
public enum PayloadAdmissionStage
{
    /// <summary>The application-body serialization boundary.</summary>
    SerializedBody = 0,

    /// <summary>The existing MessageData ownership boundary.</summary>
    MessageData = 1,

    /// <summary>The final transport-envelope materialization boundary.</summary>
    TransportEnvelope = 2,
}
