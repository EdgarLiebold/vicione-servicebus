namespace ViciOne.ServiceBus.Providers.Persistence;

/// <summary>Acceptance semantics reported by a durable-send transport adapter.</summary>
public readonly record struct DurableSendDispatchResult
{
    /// <summary>Creates a result for a defined durable-delivery completion boundary.</summary>
    /// <param name="completionMode">The completion boundary reached by the dispatcher.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="completionMode" /> is undefined.</exception>
    public DurableSendDispatchResult(DurableSendCompletionMode completionMode)
    {
        if (completionMode is not (DurableSendCompletionMode.TransportAcceptance or DurableSendCompletionMode.ConsumerCompletion))
            throw new ArgumentOutOfRangeException(nameof(completionMode), completionMode, "A completed dispatch requires a defined completion boundary.");

        CompletionMode = completionMode;
    }

    /// <summary>Gets the completion boundary reached by the dispatcher.</summary>
    public DurableSendCompletionMode CompletionMode { get; }

    /// <summary>Gets the result for a durable transport acceptance.</summary>
    public static DurableSendDispatchResult TransportAccepted { get; }
        = new(DurableSendCompletionMode.TransportAcceptance);

    /// <summary>Gets the result for a volatile delivery awaiting logical consumer completion.</summary>
    public static DurableSendDispatchResult AwaitConsumerCompletion { get; }
        = new(DurableSendCompletionMode.ConsumerCompletion);
}
