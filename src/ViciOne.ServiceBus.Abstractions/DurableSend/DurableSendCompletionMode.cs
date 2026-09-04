using System.ComponentModel;

namespace ViciOne.ServiceBus;
/// <summary>
/// Defines the acceptance boundary after which a durable sender may remove its local persisted intent.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public enum DurableSendCompletionMode
{
    /// <summary>
    /// The transport's successful send acknowledgment is itself a durable hand-off boundary (for example a broker
    /// publisher confirmation to a durable queue). The local producer intent may be removed immediately.
    /// </summary>
    TransportAcceptance = 0,

    /// <summary>
    /// The transport acknowledgment is volatile (for example an in-process memory queue). The local producer intent
    /// remains persisted until the consumer pipeline reports successful completion.
    /// </summary>
    ConsumerCompletion = 1,
}
