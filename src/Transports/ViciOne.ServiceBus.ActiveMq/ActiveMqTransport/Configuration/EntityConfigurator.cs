using System;

namespace ViciOne.ServiceBus.ActiveMq.Configuration;

/// <summary>Defines the common name and lifecycle settings of an ActiveMQ entity.</summary>
public abstract class EntityConfigurator
{
    /// <summary>Creates entity settings.</summary>
    /// <param name="entityName">The broker entity name.</param>
    /// <param name="durable">Whether the entity persists across broker restarts.</param>
    /// <param name="autoDelete">Whether the broker removes the entity when it is no longer used.</param>
    protected EntityConfigurator(string entityName, bool durable = true, bool autoDelete = false)
    {
        EntityName = entityName;
        Durable = durable;
        AutoDelete = autoDelete;
    }

    /// <summary>Gets or sets whether the entity persists across broker restarts.</summary>
    public bool Durable { get; set; }
    /// <summary>Gets or sets whether the broker removes the entity when it is no longer used.</summary>
    public bool AutoDelete { get; set; }
    /// <summary>Gets the broker entity name.</summary>
    public string EntityName { get; }

    /// <summary>Gets whether this configuration addresses a queue or topic.</summary>
    protected abstract ActiveMqEndpointAddress.AddressType AddressType { get; }

    /// <summary>Builds a destination address from the entity and host settings.</summary>
    /// <param name="hostAddress">The configured broker host address.</param>
    /// <returns>The ActiveMQ endpoint address.</returns>
    public ActiveMqEndpointAddress GetEndpointAddress(Uri hostAddress)
    {
        return new ActiveMqEndpointAddress(hostAddress, EntityName, Durable, AutoDelete, AddressType);
    }
}
