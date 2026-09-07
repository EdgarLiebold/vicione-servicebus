using System;

namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>Provides the input address associated with a receive-transport lifecycle notification.</summary>
public interface ReceiveTransportEvent
{
    /// <summary>Gets the receive transport's input address.</summary>
    Uri InputAddress { get; }
}
