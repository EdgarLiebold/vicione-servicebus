namespace ViciOne.ServiceBus.Agents;

/// <summary>Defines the operations required by async pipe context agent.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public interface IAsyncPipeContextAgent<TContext> :
    IAsyncPipeContextHandle<TContext>,
    IPipeContextAgent<TContext>
    where TContext : class, PipeContext
{
}
