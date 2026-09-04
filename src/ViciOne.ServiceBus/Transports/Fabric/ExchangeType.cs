namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>
/// Specifies the available exchange type values.
/// </summary>
public enum ExchangeType
{
    /// <summary>
    /// Indicates fan out.
    /// </summary>
    FanOut = 0,
    /// <summary>
    /// Indicates direct.
    /// </summary>
    Direct = 1,
    /// <summary>
    /// Indicates topic.
    /// </summary>
    Topic = 2,
}
