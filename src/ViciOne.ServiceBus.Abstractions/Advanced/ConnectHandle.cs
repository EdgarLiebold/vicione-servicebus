using System;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Controls a synchronous registration that can be disconnected explicitly or by disposal.</summary>
public interface ConnectHandle :
    IDisposable
{
    /// <summary>
    /// Disconnects the registration. Disposal has no further effect after a successful disconnect.
    /// </summary>
    void Disconnect();
}
