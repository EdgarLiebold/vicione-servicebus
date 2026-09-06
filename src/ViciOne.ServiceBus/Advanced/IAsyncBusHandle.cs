using System;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Represents asynchronous ownership of a bus lifetime.</summary>
public interface IAsyncBusHandle :
    IAsyncDisposable
{
}
