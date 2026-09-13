using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Context;

/// <summary>Carries state for default saga consume operations.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class DefaultSagaConsumeContext<TSaga, TMessage> :
    ConsumeContextScope<TMessage>,
    SagaConsumeContext<TSaga, TMessage>
    where TMessage : class
    where TSaga : class, ISaga
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="instance">The instance.</param>
    public DefaultSagaConsumeContext(ConsumeContext<TMessage> context, TSaga instance)
        : base(context)
    {
        Saga = instance;
    }

    /// <summary>Gets the correlation id.</summary>
    public override Guid? CorrelationId => Saga.CorrelationId;

    /// <summary>Gets the saga.</summary>
    public TSaga Saga { get; }
    /// <summary>Gets or sets a value indicating whether completed.</summary>
    public bool IsCompleted { get; private set; }

    /// <summary>Sets completed.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SetCompletedAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        IsCompleted = true;

        return Task.CompletedTask;
    }
}
