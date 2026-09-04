using System;
using ViciOne.ServiceBus.SqlTransport;
using ViciOne.ServiceBus.SqlTransport.Topology;

namespace ViciOne.ServiceBus;

public interface ISqlMessagePublishTopology<TMessage> :
    IMessagePublishTopology<TMessage>,
    ISqlMessagePublishTopology
    where TMessage : class
{
    Topic Topic { get; }

    SendSettings GetSendSettings(Uri hostAddress);

    BrokerTopology GetBrokerTopology();
}


public interface ISqlMessagePublishTopology
{
    /// <summary>
    /// Apply the message topology to the builder, including any implemented types
    /// </summary>
    /// <param name="builder">The topology builder</param>
    void Apply(IPublishEndpointBrokerTopologyBuilder builder);
}
