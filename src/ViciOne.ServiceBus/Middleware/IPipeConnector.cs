namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// The intent is to connect a pipe of a specific type to a pipe of a different type,
/// for which there is a provider that knows how to convert the input type to the output type.
/// </summary>
public interface IPipeConnector
{
    /// <summary>
    /// Connect a pipe of the specified type to the DispatchFilter
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="pipe"></param>
    /// <returns></returns>
    ConnectHandle ConnectPipe<T>(IPipe<T> pipe)
        where T : class, PipeContext;
}


/// <summary>
/// Connect a pipe of the same type as the target pipe
/// </summary>
/// <typeparam name="TContext"></typeparam>
public interface IPipeConnector<out TContext>
    where TContext : class, PipeContext
{
    /// <summary>
    /// Connects pipe.
    /// </summary>
    /// <param name="pipe">The pipe value.</param>
    /// <returns>The result of the operation.</returns>
    ConnectHandle ConnectPipe(IPipe<TContext> pipe);
}
