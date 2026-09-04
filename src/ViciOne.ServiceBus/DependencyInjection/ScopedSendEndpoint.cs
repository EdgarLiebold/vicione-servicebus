using System;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>
/// Provides a scoped send endpoint implementation.
/// </summary>
public class ScopedSendEndpoint :
    SendEndpointProxy
{
    readonly IServiceProvider _scope;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="endpoint">The endpoint value.</param>
    /// <param name="scope">The scope value.</param>
    public ScopedSendEndpoint(ISendEndpoint endpoint, IServiceProvider scope)
        : base(endpoint)
    {
        _scope = scope;
    }

    /// <summary>
    /// Gets pipe proxy.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="pipe">The pipe value.</param>
    /// <returns>The result of the operation.</returns>
    protected override IPipe<SendContext<T>> GetPipeProxy<T>(IPipe<SendContext<T>>? pipe = default)
    {
        return new ScopedSendPipeAdapter<T>(_scope, pipe);
    }
}
