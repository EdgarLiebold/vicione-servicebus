namespace ViciOne.ServiceBus;

public delegate TKey ConsumerPartitionKeySelector<in TMessage, out TKey>(TMessage message);
