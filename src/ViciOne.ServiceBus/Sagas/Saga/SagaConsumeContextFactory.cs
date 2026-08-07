// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Saga
{
    using System.Threading.Tasks;
    using Context;


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
}
