using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Util;

/// <summary>Controls the lifetime of multiple connect.</summary>
public class MultipleConnectHandle :
    ConnectHandle
{
    readonly ConnectHandle[] _handles;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="handles">The handles.</param>
    public MultipleConnectHandle(IEnumerable<ConnectHandle> handles)
    {
        _handles = handles.ToArray();
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="handles">The handles.</param>
    public MultipleConnectHandle(params ConnectHandle[] handles)
    {
        _handles = handles;
    }

    /// <summary>Disconnects the current observer or endpoint.</summary>
    public void Disconnect()
    {
        for (var i = 0; i < _handles.Length; i++)
            _handles[i].Disconnect();
    }

    /// <summary>Releases the resources owned by this instance.</summary>
    public void Dispose()
    {
        Disconnect();
    }
}
