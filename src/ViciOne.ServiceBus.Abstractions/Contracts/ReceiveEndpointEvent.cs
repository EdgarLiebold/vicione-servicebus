using System;

namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>Provides the endpoint and address associated with a receive-endpoint lifecycle notification.</summary>
public interface ReceiveEndpointEvent
{
    /// <summary>Gets the receive endpoint's input address.</summary>
    Uri InputAddress { get; }

    /// <summary>Gets the receive endpoint associated with the notification.</summary>
    IReceiveEndpoint ReceiveEndpoint { get; }
}
