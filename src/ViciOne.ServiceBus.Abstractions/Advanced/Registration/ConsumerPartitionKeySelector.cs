namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>Selects the key that assigns a consumed message to a concurrency partition.</summary>
/// <typeparam name="TMessage">The consumed message contract.</typeparam>
/// <typeparam name="TKey">The partition-key type.</typeparam>
/// <param name="message">The message whose partition is selected.</param>
/// <returns>The key used to serialize related message deliveries.</returns>
public delegate TKey ConsumerPartitionKeySelector<in TMessage, out TKey>(TMessage message);
