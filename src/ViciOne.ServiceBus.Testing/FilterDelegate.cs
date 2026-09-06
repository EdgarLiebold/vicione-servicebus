namespace ViciOne.ServiceBus.Testing;

/// <summary>Represents the method that handles filter delegate.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
/// <param name="context">The context associated with the operation.</param>
/// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
public delegate bool FilterDelegate<in TContext>(TContext context)
    where TContext : class;
