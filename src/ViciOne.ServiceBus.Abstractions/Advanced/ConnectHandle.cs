using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Controls a synchronous registration that can be disconnected explicitly or by disposal.</summary>
public interface ConnectHandle :
    IDisposable,
    IAsyncDisposable
{
    /// <summary>
    /// Disconnects the registration. Disposal has no further effect after a successful disconnect.
    /// </summary>
    void Disconnect();

    /// <summary>Disconnects the registration and awaits any asynchronous cleanup exposed by the handle.</summary>
    /// <returns>A value task that completes after the registration has released its owned resources.</returns>
    ValueTask IAsyncDisposable.DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
