// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Agents
{
    public interface IAsyncPipeContextAgent<TContext> :
        IAsyncPipeContextHandle<TContext>,
        IPipeContextAgent<TContext>
        where TContext : class, PipeContext
    {
    }
}
