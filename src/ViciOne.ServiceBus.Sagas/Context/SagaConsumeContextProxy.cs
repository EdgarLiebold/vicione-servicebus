using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Context;

/// <summary>A consumer instance merged with a message consume context.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class SagaConsumeContextProxy<TSaga, TMessage> :
    ConsumeContextProxy<TMessage>,
    SagaConsumeContext<TSaga, TMessage>
    where TMessage : class
    where TSaga : class, ISaga
{
    readonly SagaConsumeContext<TSaga, TMessage> _sagaContext;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="sagaContext">The saga context.</param>
    public SagaConsumeContextProxy(ConsumeContext<TMessage> context, SagaConsumeContext<TSaga, TMessage> sagaContext)
        : base(context)
    {
        _sagaContext = sagaContext ?? throw new ArgumentNullException(nameof(sagaContext));
    }

    /// <summary>Gets the correlation id.</summary>
    public override Guid? CorrelationId => Saga.CorrelationId;

    /// <summary>Gets the saga.</summary>
    public TSaga Saga => _sagaContext.Saga;

    /// <summary>Sets completed.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SetCompletedAsync(CancellationToken cancellationToken = default)
    {
        return _sagaContext.SetCompletedAsync(cancellationToken: cancellationToken);
    }

    /// <summary>Gets a value indicating whether completed.</summary>
    public bool IsCompleted => _sagaContext.IsCompleted;
}
