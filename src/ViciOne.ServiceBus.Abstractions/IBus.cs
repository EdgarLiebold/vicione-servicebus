using System;

namespace ViciOne.ServiceBus;

/// <summary>Provides message publishing, endpoint resolution, dynamic receiving, observation, and topology for one bus instance.</summary>
public interface IBus :
    IPublishEndpoint,
    IPublishEndpointProvider,
    ISendEndpointProvider,
    IConsumePipeConnector,
    IRequestPipeConnector,
    IConsumeMessageObserverConnector,
    IConsumeObserverConnector,
    IReceiveObserverConnector,
    IReceiveEndpointObserverConnector,
    IReceiveConnector,
    IProbeSite
{
    /// <summary>Gets the input address of the bus endpoint.</summary>
    Uri Address { get; }

    /// <summary>Gets the transport topology used by the bus.</summary>
    IBusTopology Topology { get; }
}
