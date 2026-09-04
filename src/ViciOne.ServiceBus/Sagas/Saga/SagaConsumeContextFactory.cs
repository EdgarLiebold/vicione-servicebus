using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Saga;

public class SagaConsumeContextFactory<TContext, TSaga> :
    ISagaConsumeContextFactory<TContext, TSaga>
    where TSaga : class, ISaga
    where TContext : class
{
    public Task<SagaConsumeContext<TSaga, T>> CreateSagaConsumeContext<T>(TContext context, ConsumeContext<T> consumeContext, TSaga instance,
        SagaConsumeContextMode mode)
        where T : class
    {
        return Task.FromResult<SagaConsumeContext<TSaga, T>>(new DefaultSagaConsumeContext<TSaga, T>(consumeContext, instance));
    }
}
