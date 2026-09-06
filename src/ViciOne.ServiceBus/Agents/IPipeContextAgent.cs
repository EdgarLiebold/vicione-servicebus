namespace ViciOne.ServiceBus.Agents;

/// <summary>Defines the operations required by pipe context agent.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public interface IPipeContextAgent<TContext> :
    PipeContextHandle<TContext>,
    IAgent
    where TContext : class, PipeContext
{
}
