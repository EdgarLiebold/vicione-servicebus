namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>Defines the operations required by filter observer connector.</summary>
public interface IFilterObserverConnector
{
    /// <summary>Connect an observer to the filter and/or pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectObserver<T>(IFilterObserver<T> observer)
        where T : class, PipeContext;

    /// <summary>Connect an observer to the filter and/or pipe.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectObserver(IFilterObserver observer);
}


/// <summary>Defines the operations required by filter observer connector.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public interface IFilterObserverConnector<out TContext>
    where TContext : class, PipeContext
{
    /// <summary>Connect an observer to the filter and/or pipe.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectObserver(IFilterObserver<TContext> observer);
}
