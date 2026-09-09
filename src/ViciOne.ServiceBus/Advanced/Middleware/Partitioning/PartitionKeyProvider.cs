namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>Returns the binary partition key for a pipeline context.</summary>
/// <typeparam name="TContext">The pipe-context type.</typeparam>
/// <param name="context">The context whose partition is selected.</param>
/// <returns>A non-null stable binary partition key.</returns>
public delegate byte[] PartitionKeyProvider<in TContext>(TContext context);
