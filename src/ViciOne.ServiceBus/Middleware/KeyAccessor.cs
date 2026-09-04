namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Represents the method that handles key accessor.
/// </summary>
/// <typeparam name="TContext">The t context type.</typeparam>
/// <typeparam name="TKey">The t key type.</typeparam>
/// <param name="context">The operation context.</param>
/// <returns>The result of the operation.</returns>
public delegate TKey KeyAccessor<in TContext, out TKey>(TContext context);
