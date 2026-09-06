namespace ViciOne.ServiceBus.Middleware;

/// <summary>Connects pipes selected by a dispatch key.</summary>
public interface IKeyPipeConnector<in TKey>
{
    /// <summary>Connects a context pipe for the specified key.</summary>
    ConnectHandle ConnectPipe<T>(TKey key, IPipe<T> pipe)
        where T : class, PipeContext;
}

/// <summary>Connects message pipes selected by a dispatch key.</summary>
public interface IKeyPipeConnector<out TMessage, in TKey>
    where TMessage : class
{
    /// <summary>Connects a message pipe for the specified key.</summary>
    ConnectHandle ConnectPipe(TKey key, IPipe<ConsumeContext<TMessage>> pipe);
}
