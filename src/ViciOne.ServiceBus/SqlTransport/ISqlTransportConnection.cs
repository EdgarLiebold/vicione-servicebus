using System;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>
/// Defines the contract for sql transport connection.
/// </summary>
public interface ISqlTransportConnection :
    IAsyncDisposable
{
}
