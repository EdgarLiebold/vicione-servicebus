using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Context;

/// <summary>
/// Provides a default saga consume context implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class DefaultSagaConsumeContext<TSaga, TMessage> :
    ConsumeContextScope<TMessage>,
    SagaConsumeContext<TSaga, TMessage>
    where TMessage : class
    where TSaga : class, ISaga
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="instance">The instance value.</param>
    public DefaultSagaConsumeContext(ConsumeContext<TMessage> context, TSaga instance)
        : base(context)
    {
        Saga = instance;
    }

    /// <summary>
    /// Gets the correlation id value.
    /// </summary>
    public override Guid? CorrelationId => Saga.CorrelationId;

    /// <summary>
    /// Gets the saga value.
    /// </summary>
    public TSaga Saga { get; }
    /// <summary>
    /// Gets or sets the is completed value.
    /// </summary>
    public bool IsCompleted { get; private set; }

    /// <summary>
    /// Sets completed.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SetCompletedAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); IsCompleted = true;

        return Task.CompletedTask;
    }
}
