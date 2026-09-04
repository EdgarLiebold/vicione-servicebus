using System;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>
/// Provides a scoped send pipe adapter implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class ScopedSendPipeAdapter<TMessage> :
    SendContextPipeAdapter<TMessage>
    where TMessage : class
{
    readonly IServiceProvider _provider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    /// <param name="pipe">The pipe value.</param>
    public ScopedSendPipeAdapter(IServiceProvider provider, IPipe<SendContext<TMessage>>? pipe)
        : base(pipe)
    {
        _provider = provider;
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    protected override void Send<T>(SendContext<T> context)
    {
        context.GetOrAddPayload(() => _provider);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    protected override void Send(SendContext<TMessage> context)
    {
    }
}
