namespace ViciOne.ServiceBus.Azure.Storage.MessageData;

/// <summary>Generates unique blob names for Azure-backed message payloads.</summary>
public interface IBlobNameGenerator
{
    /// <summary>Generates the name for the next message-data blob.</summary>
    /// <returns>A non-empty blob name that is unique within the repository container.</returns>
    string GenerateBlobName();
}
