namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>
/// Supports connection of a message observer to the pipeline
/// </summary>
public interface IConsumeMessageObserverConnector
{
    /// <summary>
    /// Connects consume message observer.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    ConnectHandle ConnectConsumeMessageObserver<T>(IConsumeMessageObserver<T> observer)
        where T : class;
}


/// <summary>
/// Supports connection of a message observer to the pipeline
/// </summary>
public interface IConsumeMessageObserverConnector<out T>
    where T : class
{
    /// <summary>
    /// Connects consume message observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    ConnectHandle ConnectConsumeMessageObserver(IConsumeMessageObserver<T> observer);
}
