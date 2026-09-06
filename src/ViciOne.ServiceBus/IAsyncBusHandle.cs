using System;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Controls the lifetime of async bus.</summary>
public interface IAsyncBusHandle :
    IAsyncDisposable
{
}
