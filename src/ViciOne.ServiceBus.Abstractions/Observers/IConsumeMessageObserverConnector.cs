namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>Supports connection of a message observer to the pipeline.</summary>
public interface IConsumeMessageObserverConnector
{
    /// <summary>Connects consume message observer.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectConsumeMessageObserver<T>(IConsumeMessageObserver<T> observer)
        where T : class;
}


/// <summary>Supports connection of a message observer to the pipeline.</summary>
/// <typeparam name="T">The value type.</typeparam>
public interface IConsumeMessageObserverConnector<out T>
    where T : class
{
    /// <summary>Connects consume message observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectConsumeMessageObserver(IConsumeMessageObserver<T> observer);
}
