namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes requirements for redelivery pipe.</summary>
public interface IRedeliveryPipeSpecification
{
    /// <summary>Gets or sets the options.</summary>
    RedeliveryOptions Options { get; set; }
}
