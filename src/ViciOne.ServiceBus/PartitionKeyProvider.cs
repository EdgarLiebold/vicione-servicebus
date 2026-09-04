namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Represents the method that handles partition key provider.
/// </summary>
/// <typeparam name="TContext">The t context type.</typeparam>
/// <param name="context">The operation context.</param>
/// <returns>The result of the operation.</returns>
public delegate byte[] PartitionKeyProvider<in TContext>(TContext context);
