namespace ViciOne.ServiceBus.Middleware;

/// <summary>Connects pipes selected by a dispatch key.</summary>
/// <typeparam name="TKey">The key used for lookup.</typeparam>
public interface IKeyPipeConnector<in TKey>
{
    /// <summary>Connects a context pipe for the specified key.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectPipe<T>(TKey key, IPipe<T> pipe)
        where T : class, PipeContext;
}

/// <summary>Connects message pipes selected by a dispatch key.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <typeparam name="TKey">The key used for lookup.</typeparam>
public interface IKeyPipeConnector<out TMessage, in TKey>
    where TMessage : class
{
    /// <summary>Connects a message pipe for the specified key.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectPipe(TKey key, IPipe<ConsumeContext<TMessage>> pipe);
}
