namespace ViciOne.ServiceBus.Agents;

/// <summary>Represents a supervised agent whose pipe context becomes available asynchronously.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public interface IAsyncPipeContextAgent<TContext> :
    IAsyncPipeContextHandle<TContext>,
    IPipeContextAgent<TContext>
    where TContext : class, PipeContext
{
}
