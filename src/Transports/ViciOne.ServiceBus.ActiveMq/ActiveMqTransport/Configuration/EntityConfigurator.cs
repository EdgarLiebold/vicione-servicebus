using System;

namespace ViciOne.ServiceBus.ActiveMq.Configuration;

/// <summary>
/// Provides an entity configurator implementation.
/// </summary>
public abstract class EntityConfigurator
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="entityName">The entity name value.</param>
    /// <param name="durable">The durable value.</param>
    /// <param name="autoDelete">The auto delete value.</param>
    protected EntityConfigurator(string entityName, bool durable = true, bool autoDelete = false)
    {
        EntityName = entityName;
        Durable = durable;
        AutoDelete = autoDelete;
    }

    /// <summary>
    /// Gets or sets the durable value.
    /// </summary>
    public bool Durable { get; set; }
    /// <summary>
    /// Gets or sets the auto delete value.
    /// </summary>
    public bool AutoDelete { get; set; }
    /// <summary>
    /// Gets the entity name value.
    /// </summary>
    public string EntityName { get; }

    /// <summary>
    /// Gets the address type value.
    /// </summary>
    protected abstract ActiveMqEndpointAddress.AddressType AddressType { get; }

    /// <summary>
    /// Gets endpoint address.
    /// </summary>
    /// <param name="hostAddress">The host address value.</param>
    /// <returns>The result of the operation.</returns>
    public ActiveMqEndpointAddress GetEndpointAddress(Uri hostAddress)
    {
        return new ActiveMqEndpointAddress(hostAddress, EntityName, Durable, AutoDelete, AddressType);
    }
}
