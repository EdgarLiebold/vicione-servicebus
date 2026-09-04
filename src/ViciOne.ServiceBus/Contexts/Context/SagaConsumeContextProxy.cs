using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Context;

/// <summary>
/// A consumer instance merged with a message consume context
/// </summary>
/// <typeparam name="TSaga"></typeparam>
/// <typeparam name="TMessage"></typeparam>
public class SagaConsumeContextProxy<TSaga, TMessage> :
    ConsumeContextProxy<TMessage>,
    SagaConsumeContext<TSaga, TMessage>
    where TMessage : class
    where TSaga : class, ISaga
{
    readonly SagaConsumeContext<TSaga, TMessage> _sagaContext;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="sagaContext">The saga context value.</param>
    public SagaConsumeContextProxy(ConsumeContext<TMessage> context, SagaConsumeContext<TSaga, TMessage> sagaContext)
        : base(context)
    {
        _sagaContext = sagaContext;
    }

    /// <summary>
    /// Gets the correlation id value.
    /// </summary>
    public override Guid? CorrelationId => Saga.CorrelationId;

    /// <summary>
    /// Gets the saga value.
    /// </summary>
    public TSaga Saga => _sagaContext.Saga;

    /// <summary>
    /// Sets completed.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SetCompletedAsync(CancellationToken cancellationToken = default)
    {
        return _sagaContext.SetCompletedAsync(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Gets the is completed value.
    /// </summary>
    public bool IsCompleted => _sagaContext.IsCompleted;
}
