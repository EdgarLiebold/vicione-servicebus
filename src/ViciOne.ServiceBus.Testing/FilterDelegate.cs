namespace ViciOne.ServiceBus.Testing;

public delegate bool FilterDelegate<in TContext>(TContext context)
    where TContext : class;
