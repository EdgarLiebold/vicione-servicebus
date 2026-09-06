using System;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Adapts scoped send pipe between component contracts.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class ScopedSendPipeAdapter<TMessage> :
    SendContextPipeAdapter<TMessage>
    where TMessage : class
{
    readonly IServiceProvider _provider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    public ScopedSendPipeAdapter(IServiceProvider provider, IPipe<SendContext<TMessage>>? pipe)
        : base(pipe)
    {
        _provider = provider;
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    protected override void Send<T>(SendContext<T> context)
    {
        context.GetOrAddPayload(() => _provider);
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="context">The context associated with the operation.</param>
    protected override void Send(SendContext<TMessage> context)
    {
    }
}
