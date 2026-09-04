using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Context;

public class DefaultSagaConsumeContext<TSaga, TMessage> :
    ConsumeContextScope<TMessage>,
    SagaConsumeContext<TSaga, TMessage>
    where TMessage : class
    where TSaga : class, ISaga
{
    public DefaultSagaConsumeContext(ConsumeContext<TMessage> context, TSaga instance)
        : base(context)
    {
        Saga = instance;
    }

    public override Guid? CorrelationId => Saga.CorrelationId;

    public TSaga Saga { get; }
    public bool IsCompleted { get; private set; }

    public Task SetCompletedAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); IsCompleted = true;

        return Task.CompletedTask;
    }
}
