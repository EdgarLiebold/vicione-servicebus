using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.ActiveMq.Configuration;

namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>Defines the address, lifecycle, and topology of an ActiveMQ queue send destination.</summary>
public class ActiveMqQueueSendSettings :
    ActiveMqQueueConfigurator,
    SendSettings
{
    /// <summary>Creates queue send settings from a parsed ActiveMQ endpoint address.</summary>
    /// <param name="address">The queue endpoint address.</param>
    public ActiveMqQueueSendSettings(ActiveMqEndpointAddress address)
        : base(address.Name, address.Durable, address.AutoDelete)
    {
    }

    /// <summary>Builds the absolute queue send address.</summary>
    /// <param name="hostAddress">The configured broker address.</param>
    /// <returns>The absolute queue address.</returns>
    public Uri GetSendAddress(Uri hostAddress)
    {
        return GetEndpointAddress(hostAddress);
    }

    /// <summary>Builds topology containing the send queue.</summary>
    /// <returns>The queue-only broker topology.</returns>
    public BrokerTopology GetBrokerTopology()
    {
        var builder = new PublishEndpointBrokerTopologyBuilder();

        builder.CreateQueue(EntityName, Durable, AutoDelete);

        return builder.BuildBrokerTopology();
    }

    IEnumerable<string> GetSettingStrings()
    {
        if (Durable)
            yield return "durable";

        if (AutoDelete)
            yield return "auto-delete";
    }

    /// <summary>Returns the enabled queue lifecycle flags.</summary>
    /// <returns>A comma-separated list containing <c>durable</c> and/or <c>auto-delete</c>.</returns>
    public override string ToString()
    {
        return string.Join(", ", GetSettingStrings());
    }
}
