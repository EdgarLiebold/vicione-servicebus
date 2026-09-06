using System;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Saga;

/// <summary>Carries state for in memory saga consume operations.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class InMemorySagaConsumeContext<TSaga, TMessage> :
    DefaultSagaConsumeContext<TSaga, TMessage>,
    IDisposable
    where TMessage : class
    where TSaga : class, ISaga
{
    readonly SagaInstance<TSaga> _saga;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="saga">The saga.</param>
    public InMemorySagaConsumeContext(ConsumeContext<TMessage> context, SagaInstance<TSaga> saga)
        : base(context, saga.Instance)
    {
        _saga = saga;
    }

    /// <summary>Releases the resources owned by this instance.</summary>
    public void Dispose()
    {
        _saga.Release();
    }
}
