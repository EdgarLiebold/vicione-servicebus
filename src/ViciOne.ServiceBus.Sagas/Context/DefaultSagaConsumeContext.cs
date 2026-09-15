using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Context;

/// <summary>Combines a consumed message with its referenced saga state and completion flag.</summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
/// <typeparam name="TMessage">The consumed message type.</typeparam>
public class DefaultSagaConsumeContext<TSaga, TMessage> :
    ConsumeContextScope<TMessage>,
    SagaConsumeContext<TSaga, TMessage>
    where TMessage : class
    where TSaga : class, ISaga
{
    /// <summary>Wraps the required message context and saga state without copying the state.</summary>
    /// <param name="context">The required consumed-message context.</param>
    /// <param name="instance">The required saga state.</param>
    public DefaultSagaConsumeContext(ConsumeContext<TMessage> context, TSaga instance)
        : base(context)
    {
        Saga = instance ?? throw new ArgumentNullException(nameof(instance));
    }

    /// <summary>Gets the correlation identifier from the current saga state.</summary>
    public override Guid? CorrelationId => Saga.CorrelationId;

    /// <summary>Gets the exact saga state supplied at construction.</summary>
    public TSaga Saga { get; }

    /// <summary>Gets whether this context has marked its saga as completed.</summary>
    public bool IsCompleted { get; private set; }

    /// <summary>Marks the saga as completed unless the explicitly supplied token is cancelled.</summary>
    /// <param name="cancellationToken">The token checked before changing the completion flag.</param>
    /// <returns>A completed task, or a task cancelled with the supplied token without changing the flag.</returns>
    public Task SetCompletedAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        IsCompleted = true;

        return Task.CompletedTask;
    }
}
