namespace ViciOne.ServiceBus.Util;

/// <summary>Represents a connection that owns no resources and requires no disconnection work.</summary>
public class EmptyConnectHandle :
    ConnectHandle
{
    /// <summary>Releases the resources owned by this instance.</summary>
    public void Dispose()
    {
    }

    /// <summary>Disconnects the current observer or endpoint.</summary>
    public void Disconnect()
    {
    }
}
