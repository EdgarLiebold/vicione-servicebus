namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Defines configuration options for amazon sqs transport.
/// </summary>
public class AmazonSqsTransportOptions
{
    /// <summary>
    /// Gets or sets the region value.
    /// </summary>
    public string? Region { get; set; }
    /// <summary>
    /// Gets or sets the scope value.
    /// </summary>
    public string? Scope { get; set; }
}
