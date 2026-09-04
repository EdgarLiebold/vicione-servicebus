using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Logging;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Creates a saga instance using the default factory method
/// </summary>
/// <typeparam name="TSaga"></typeparam>
/// <typeparam name="TMessage"></typeparam>
public class DefaultSagaFactory<TSaga, TMessage> :
    ISagaFactory<TSaga, TMessage>
    where TSaga : class, ISaga
    where TMessage : class
{
    public TSaga Create(ConsumeContext<TMessage> context)
    {
        if (!context.CorrelationId.HasValue)
            throw new SagaException("The correlationId was not present and the saga could not be created", typeof(TSaga), typeof(TMessage));

        return SagaMetadataCache<TSaga>.FactoryMethod(context.CorrelationId.Value);
    }

    public Task SendAsync(ConsumeContext<TMessage> context, IPipe<SagaConsumeContext<TSaga, TMessage>> next)
    {
        if (!context.CorrelationId.HasValue)
            throw new SagaException("The correlationId was not present and the saga could not be created", typeof(TSaga), typeof(TMessage));

        var instance = SagaMetadataCache<TSaga>.FactoryMethod(context.CorrelationId.Value);

        var proxy = new DefaultSagaConsumeContext<TSaga, TMessage>(context, instance);

        proxy.LogCreated();

        return next.SendAsync(proxy);
    }
}
