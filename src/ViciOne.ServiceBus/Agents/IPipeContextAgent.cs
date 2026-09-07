namespace ViciOne.ServiceBus.Agents;

/// <summary>Represents a supervised owner and source of a pipe context.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public interface IPipeContextAgent<TContext> :
    IPipeContextHandle<TContext>,
    IAgent
    where TContext : class, PipeContext
{
}
