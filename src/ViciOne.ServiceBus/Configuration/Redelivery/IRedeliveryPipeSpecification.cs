namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for redelivery pipe specification.
/// </summary>
public interface IRedeliveryPipeSpecification
{
    /// <summary>
    /// Gets or sets the options value.
    /// </summary>
    RedeliveryOptions Options { get; set; }
}
