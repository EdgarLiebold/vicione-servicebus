using System;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Saga;

/// <summary>
/// Provides an in memory saga consume context implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class InMemorySagaConsumeContext<TSaga, TMessage> :
    DefaultSagaConsumeContext<TSaga, TMessage>,
    IDisposable
    where TMessage : class
    where TSaga : class, ISaga
{
    readonly SagaInstance<TSaga> _saga;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="saga">The saga value.</param>
    public InMemorySagaConsumeContext(ConsumeContext<TMessage> context, SagaInstance<TSaga> saga)
        : base(context, saga.Instance)
    {
        _saga = saga;
    }

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    public void Dispose()
    {
        _saga.Release();
    }
}
