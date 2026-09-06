namespace ViciOne.ServiceBus.Util;

/// <summary>A do-nothing connect handle, simply to satisfy.</summary>
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
