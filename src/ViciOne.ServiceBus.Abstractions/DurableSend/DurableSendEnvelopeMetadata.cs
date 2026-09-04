#nullable enable

namespace ViciOne.ServiceBus.ProviderAbstractions;

using System.ComponentModel;

/// <summary>
/// Marks a canonical send context as a durable admission envelope. The retained store owns its admission timestamp,
/// so serializers must not inject a new wall-clock sent time into otherwise idempotent envelope bytes.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class DurableSendEnvelopeMetadata
{
    private DurableSendEnvelopeMetadata()
    {
    }

    public static DurableSendEnvelopeMetadata Instance { get; } = new();
}
