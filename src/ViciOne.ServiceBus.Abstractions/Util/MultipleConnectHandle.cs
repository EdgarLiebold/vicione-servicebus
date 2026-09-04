using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Util;

/// <summary>
/// Provides a multiple connect handle implementation.
/// </summary>
public class MultipleConnectHandle :
    ConnectHandle
{
    readonly ConnectHandle[] _handles;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="handles">The handles value.</param>
    public MultipleConnectHandle(IEnumerable<ConnectHandle> handles)
    {
        _handles = handles.ToArray();
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="handles">The handles value.</param>
    public MultipleConnectHandle(params ConnectHandle[] handles)
    {
        _handles = handles;
    }

    /// <summary>
    /// Performs the disconnect operation.
    /// </summary>
    public void Disconnect()
    {
        for (var i = 0; i < _handles.Length; i++)
            _handles[i].Disconnect();
    }

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    public void Dispose()
    {
        Disconnect();
    }
}
