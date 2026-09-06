namespace ViciOne.ServiceBus.Advanced;

/// <summary>Represents the method that handles partition key provider.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
/// <param name="context">The context associated with the operation.</param>
/// <returns>The value produced by the operation.</returns>
public delegate byte[] PartitionKeyProvider<in TContext>(TContext context);
