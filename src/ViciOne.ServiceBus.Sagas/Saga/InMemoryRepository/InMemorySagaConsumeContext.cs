using System;
using System.Threading;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Saga;

/// <summary>Wraps a consumed message and owns one acquired in-memory saga lease.</summary>
/// <typeparam name="TSaga">The referenced saga state type.</typeparam>
/// <typeparam name="TMessage">The consumed message type.</typeparam>
public class InMemorySagaConsumeContext<TSaga, TMessage> :
    DefaultSagaConsumeContext<TSaga, TMessage>,
    IDisposable
    where TMessage : class
    where TSaga : class, ISaga
{
    SagaInstance<TSaga>? _saga;

    /// <summary>Takes ownership of a saga lease already acquired by the caller.</summary>
    /// <param name="context">The required consumed-message context.</param>
    /// <param name="saga">The required saga wrapper whose lease is transferred to this context.</param>
    public InMemorySagaConsumeContext(ConsumeContext<TMessage> context, SagaInstance<TSaga> saga)
        : base(context ?? throw new ArgumentNullException(nameof(context)),
            (saga ?? throw new ArgumentNullException(nameof(saga))).Instance)
    {
        _saga = saga;
    }

    /// <summary>Releases the transferred saga lease exactly once, including concurrent disposal.</summary>
    public void Dispose()
    {
        Interlocked.Exchange(ref _saga, null)?.Release();
    }
}
