namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>
/// Specifies the available consumer concurrency mode values.
/// </summary>
public enum ConsumerConcurrencyMode
{
    /// <summary>
    /// Indicates parallel.
    /// </summary>
    Parallel = 0,
    /// <summary>
    /// Indicates serial.
    /// </summary>
    Serial = 1,
    /// <summary>
    /// Indicates partitioned.
    /// </summary>
    Partitioned = 2
}
