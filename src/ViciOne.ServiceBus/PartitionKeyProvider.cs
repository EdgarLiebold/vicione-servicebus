namespace ViciOne.ServiceBus;

public delegate byte[] PartitionKeyProvider<in TContext>(TContext context);
