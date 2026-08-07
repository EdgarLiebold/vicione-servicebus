// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Agents
{
    /// <summary>
    /// An active use of a pipe context as an agent.
    /// </summary>
    /// <typeparam name="TContext"></typeparam>
    public interface IActivePipeContextAgent<TContext> :
        ActivePipeContextHandle<TContext>,
        IAgent
        where TContext : class, PipeContext
    {
    }
}
