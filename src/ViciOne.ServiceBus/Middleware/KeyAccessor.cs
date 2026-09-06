namespace ViciOne.ServiceBus.Middleware;

/// <summary>Represents the method that handles key accessor.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
/// <typeparam name="TKey">The key used for lookup.</typeparam>
/// <param name="context">The context associated with the operation.</param>
/// <returns>The value produced by the operation.</returns>
public delegate TKey KeyAccessor<in TContext, out TKey>(TContext context);
