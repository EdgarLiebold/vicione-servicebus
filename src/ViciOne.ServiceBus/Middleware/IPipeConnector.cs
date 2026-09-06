namespace ViciOne.ServiceBus.Middleware;

/// <summary>Defines the operations required by pipe connector.</summary>
public interface IPipeConnector
{
    /// <summary>Connect a pipe of the specified type to the DispatchFilter.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectPipe<T>(IPipe<T> pipe)
        where T : class, PipeContext;
}


/// <summary>Connect a pipe of the same type as the target pipe.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public interface IPipeConnector<out TContext>
    where TContext : class, PipeContext
{
    /// <summary>Connects pipe.</summary>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectPipe(IPipe<TContext> pipe);
}
