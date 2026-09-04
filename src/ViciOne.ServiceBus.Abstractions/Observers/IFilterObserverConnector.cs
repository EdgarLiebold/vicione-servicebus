namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>
/// Defines the contract for filter observer connector.
/// </summary>
public interface IFilterObserverConnector
{
    /// <summary>
    /// Connect an observer to the filter and/or pipe
    /// </summary>
    /// <param name="observer"></param>
    /// <returns></returns>
    ConnectHandle ConnectObserver<T>(IFilterObserver<T> observer)
        where T : class, PipeContext;

    /// <summary>
    /// Connect an observer to the filter and/or pipe
    /// </summary>
    /// <param name="observer"></param>
    /// <returns></returns>
    ConnectHandle ConnectObserver(IFilterObserver observer);
}


/// <summary>
/// Defines the contract for filter observer connector.
/// </summary>
/// <typeparam name="TContext">The t context type.</typeparam>
public interface IFilterObserverConnector<out TContext>
    where TContext : class, PipeContext
{
    /// <summary>
    /// Connect an observer to the filter and/or pipe
    /// </summary>
    /// <param name="observer"></param>
    /// <returns></returns>
    ConnectHandle ConnectObserver(IFilterObserver<TContext> observer);
}
