using System;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Provides an endpoint for scoped send.</summary>
public class ScopedSendEndpoint :
    SendEndpointProxy
{
    readonly IServiceProvider _scope;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="endpoint">The endpoint.</param>
    /// <param name="scope">The scope.</param>
    public ScopedSendEndpoint(ISendEndpoint endpoint, IServiceProvider scope)
        : base(endpoint)
    {
        _scope = scope;
    }

    /// <summary>Gets pipe proxy.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <returns>The pipe proxy.</returns>
    protected override IPipe<SendContext<T>> GetPipeProxy<T>(IPipe<SendContext<T>>? pipe = default)
    {
        return new ScopedSendPipeAdapter<T>(_scope, pipe);
    }
}
