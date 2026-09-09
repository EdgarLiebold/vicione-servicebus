namespace ViciOne.ServiceBus.Providers.Persistence;

/// <summary>
/// One claimed durable-send dispatch attempt. <see cref="ConsumerCompletion"/> is an unforgeable process-local capability
/// that a volatile in-process transport may propagate as pipeline context. It must never be serialized onto a wire.
/// </summary>
public readonly record struct DurableSendDispatchContext
{
    /// <summary>Creates a fenced dispatch attempt for one retained durable intent.</summary>
    /// <param name="message">The validated serialized intent.</param>
    /// <param name="durableSendId">The identity used to correlate dispatch telemetry and completion.</param>
    /// <param name="attempt">The one-based delivery attempt number.</param>
    /// <param name="consumerCompletion">The process-local consumer-completion capability.</param>
    /// <exception cref="ArgumentNullException">Either reference argument is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">The identities do not all identify <paramref name="message" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="attempt" /> is less than one.</exception>
    public DurableSendDispatchContext(
        SerializedDurableSend message,
        DurableSendId durableSendId,
        int attempt,
        IDurableSendConsumerCompletion consumerCompletion)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(consumerCompletion);
        _ = message.Validate();
        if (durableSendId.Value == Guid.Empty || durableSendId != message.Id)
            throw new ArgumentException("The dispatch identity must match the serialized durable intent.", nameof(durableSendId));
        if (consumerCompletion.DurableSendId != durableSendId)
            throw new ArgumentException("The completion capability must match the dispatch identity.", nameof(consumerCompletion));

        ArgumentOutOfRangeException.ThrowIfLessThan(attempt, 1);

        Message = message;
        DurableSendId = durableSendId;
        Attempt = attempt;
        ConsumerCompletion = consumerCompletion;
    }

    /// <summary>Gets the serialized durable intent.</summary>
    public SerializedDurableSend Message { get; }

    /// <summary>Gets the durable-send identity.</summary>
    public DurableSendId DurableSendId { get; }

    /// <summary>Gets the one-based delivery attempt number.</summary>
    public int Attempt { get; }

    /// <summary>Gets the process-local consumer-completion capability.</summary>
    public IDurableSendConsumerCompletion ConsumerCompletion { get; }
}
