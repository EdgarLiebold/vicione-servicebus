
namespace ViciOne.ServiceBus.Providers.Persistence;
/// <summary>Acceptance semantics reported by a durable-send transport adapter.</summary>
/// <param name="CompletionMode">The completion mode.</param>
public readonly record struct DurableSendDispatchResult(DurableSendCompletionMode CompletionMode)
{
    /// <summary>Gets the transport accepted.</summary>
    public static DurableSendDispatchResult TransportAccepted { get; }
        = new(DurableSendCompletionMode.TransportAcceptance);

    /// <summary>Gets the await consumer completion.</summary>
    public static DurableSendDispatchResult AwaitConsumerCompletion { get; }
        = new(DurableSendCompletionMode.ConsumerCompletion);
}
