// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Agents
{
    public interface IPipeContextAgent<TContext> :
        PipeContextHandle<TContext>,
        IAgent
        where TContext : class, PipeContext
    {
    }
}
