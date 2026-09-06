namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>Represents the method that handles consumer partition key selector.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <typeparam name="TKey">The key used for lookup.</typeparam>
/// <param name="message">The message to process.</param>
/// <returns>The value produced by the operation.</returns>
public delegate TKey ConsumerPartitionKeySelector<in TMessage, out TKey>(TMessage message);
