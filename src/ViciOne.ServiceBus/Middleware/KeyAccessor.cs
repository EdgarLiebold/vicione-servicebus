namespace ViciOne.ServiceBus.Middleware;

public delegate TKey KeyAccessor<in TContext, out TKey>(TContext context);
