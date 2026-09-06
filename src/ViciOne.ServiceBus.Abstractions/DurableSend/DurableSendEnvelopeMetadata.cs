

namespace ViciOne.ServiceBus.Providers.Persistence;
/// <summary>
/// Marks a canonical send context as a durable admission envelope. The retained store owns its admission timestamp,
/// so serializers must not inject a new wall-clock sent time into otherwise idempotent envelope bytes.
/// </summary>
public sealed class DurableSendEnvelopeMetadata
{
    private DurableSendEnvelopeMetadata()
    {
    }

    /// <summary>Gets the instance.</summary>
    public static DurableSendEnvelopeMetadata Instance { get; } = new();
}
