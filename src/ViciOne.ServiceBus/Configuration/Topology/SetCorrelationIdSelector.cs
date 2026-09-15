namespace ViciOne.ServiceBus.Configuration;

/// <summary>Returns one explicitly configured message correlation resolver.</summary>
/// <typeparam name="T">The message contract.</typeparam>
sealed class SetCorrelationIdSelector<T> :
    ICorrelationIdSelector<T>
    where T : class
{
    readonly IMessageCorrelationId<T> _messageCorrelationId;

    /// <summary>Creates a selector for the specified correlation resolver.</summary>
    /// <param name="messageCorrelationId">The resolver returned by this selector.</param>
    public SetCorrelationIdSelector(IMessageCorrelationId<T> messageCorrelationId)
    {
        _messageCorrelationId = messageCorrelationId ?? throw new ArgumentNullException(nameof(messageCorrelationId));
    }

    /// <summary>Returns the configured resolver.</summary>
    /// <param name="messageCorrelationId">The configured correlation resolver.</param>
    /// <returns>Always <see langword="true" />.</returns>
    public bool TryGetCorrelationIdResolver([NotNullWhen(true)] out IMessageCorrelationId<T>? messageCorrelationId)
    {
        messageCorrelationId = _messageCorrelationId;
        return true;
    }
}
