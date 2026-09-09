namespace ViciOne.ServiceBus.Advanced;

/// <summary>Converts consume-context callbacks into pipeline stages.</summary>
public static class ConsumeContextExecuteExtensions
{
    /// <summary>Creates a pipe that invokes a synchronous typed consume callback.</summary>
    /// <typeparam name="TMessage">The consumed message type.</typeparam>
    /// <param name="callback">The callback invoked for each context.</param>
    /// <returns>A pipe that invokes the callback.</returns>
    public static IPipe<ConsumeContext<TMessage>> ToPipe<TMessage>(this Action<ConsumeContext<TMessage>> callback)
        where TMessage : class
    {
        return Middleware.Pipe.Execute(callback);
    }

    /// <summary>Creates a pipe that invokes an asynchronous typed consume callback.</summary>
    /// <typeparam name="TMessage">The consumed message type.</typeparam>
    /// <param name="callback">The callback invoked for each context.</param>
    /// <returns>A pipe that invokes the callback.</returns>
    public static IPipe<ConsumeContext<TMessage>> ToPipe<TMessage>(this Func<ConsumeContext<TMessage>, Task> callback)
        where TMessage : class
    {
        return Middleware.Pipe.ExecuteAwaited(callback);
    }

    /// <summary>Creates a pipe that invokes a synchronous consume callback.</summary>
    /// <param name="callback">The callback invoked for each context.</param>
    /// <returns>A pipe that invokes the callback.</returns>
    public static IPipe<ConsumeContext> ToPipe(this Action<ConsumeContext> callback)
    {
        return Middleware.Pipe.Execute(callback);
    }

    /// <summary>Creates a pipe that invokes an asynchronous consume callback.</summary>
    /// <param name="callback">The callback invoked for each context.</param>
    /// <returns>A pipe that invokes the callback.</returns>
    public static IPipe<ConsumeContext> ToPipe(this Func<ConsumeContext, Task> callback)
    {
        return Middleware.Pipe.ExecuteAwaited(callback);
    }
}
