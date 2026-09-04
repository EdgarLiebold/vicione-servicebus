namespace ViciOne.ServiceBus.Testing;

/// <summary>
/// Represents the method that handles filter delegate.
/// </summary>
/// <typeparam name="TContext">The t context type.</typeparam>
/// <param name="context">The operation context.</param>
/// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
public delegate bool FilterDelegate<in TContext>(TContext context)
    where TContext : class;
