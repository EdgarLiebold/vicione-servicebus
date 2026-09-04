using System.ComponentModel;

namespace ViciOne.ServiceBus;
/// <summary>Acceptance semantics reported by a durable-send transport adapter.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public readonly record struct DurableSendDispatchResult(DurableSendCompletionMode CompletionMode)
{
    public static DurableSendDispatchResult TransportAccepted { get; }
        = new(DurableSendCompletionMode.TransportAcceptance);

    public static DurableSendDispatchResult AwaitConsumerCompletion { get; }
        = new(DurableSendCompletionMode.ConsumerCompletion);
}
