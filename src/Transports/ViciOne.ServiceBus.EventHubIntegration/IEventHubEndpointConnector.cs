using System;

namespace ViciOne.ServiceBus;

public interface IEventHubEndpointConnector
{
    HostReceiveEndpointHandle ConnectEventHubEndpoint(string eventHubName, string consumerGroup,
        Action<IRiderRegistrationContext, IEventHubReceiveEndpointConfigurator> configure);
}
