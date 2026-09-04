namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>
/// Represents the method that handles consumer partition key selector.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
/// <typeparam name="TKey">The t key type.</typeparam>
/// <param name="message">The message value.</param>
/// <returns>The result of the operation.</returns>
public delegate TKey ConsumerPartitionKeySelector<in TMessage, out TKey>(TMessage message);
