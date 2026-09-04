namespace ViciOne.ServiceBus.Sagas;

/// <summary>
/// Specifies the available concurrency mode values.
/// </summary>
public enum ConcurrencyMode
{
    /// <summary>
    /// Indicates optimistic.
    /// </summary>
    Optimistic = 0,
    /// <summary>
    /// Indicates pessimistic.
    /// </summary>
    Pessimistic = 1
}
