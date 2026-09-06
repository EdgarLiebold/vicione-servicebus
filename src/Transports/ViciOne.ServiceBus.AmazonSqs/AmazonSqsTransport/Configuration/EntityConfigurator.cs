using System;

namespace ViciOne.ServiceBus.AmazonSqs.Configuration;

/// <summary>Provides common naming and lifetime settings for Amazon SQS and Amazon SNS entities.</summary>
public abstract class EntityConfigurator
{
    /// <summary>Initializes common entity settings.</summary>
    /// <param name="entityName">The provider entity name.</param>
    /// <param name="durable">Whether the entity is retained when its endpoint stops.</param>
    /// <param name="autoDelete">Whether the entity is deleted when its endpoint stops.</param>
    protected EntityConfigurator(string entityName, bool durable = true, bool autoDelete = false)
    {
        EntityName = entityName;
        Durable = durable;
        AutoDelete = autoDelete;
    }

    /// <summary>Gets or sets whether the entity is retained when its endpoint stops.</summary>
    public bool Durable { get; set; }
    /// <summary>Gets or sets whether the entity is deleted when its endpoint stops.</summary>
    public bool AutoDelete { get; set; }
    /// <summary>Gets or sets the provider entity name.</summary>
    public string EntityName { get; set; }

    /// <summary>Gets the provider entity kind encoded in endpoint addresses.</summary>
    protected abstract AmazonSqsEndpointAddress.AddressType AddressType { get; }

    /// <summary>Formats the provider endpoint address for the configured entity.</summary>
    /// <param name="hostAddress">The Amazon SQS host address.</param>
    /// <returns>The queue or topic endpoint address.</returns>
    public virtual AmazonSqsEndpointAddress GetEndpointAddress(Uri hostAddress)
    {
        return new AmazonSqsEndpointAddress(hostAddress, EntityName, Durable, AutoDelete, AddressType);
    }
}
