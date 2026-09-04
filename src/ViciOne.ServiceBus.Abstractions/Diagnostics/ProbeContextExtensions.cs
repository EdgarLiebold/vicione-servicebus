namespace ViciOne.ServiceBus.Operations;

/// <summary>
/// Provides extension methods for probe context.
/// </summary>
public static class ProbeContextExtensions
{
    /// <summary>
    /// Creates filter scope.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="filterType">The filter type value.</param>
    /// <returns>The result of the operation.</returns>
    public static ProbeContext CreateFilterScope(this ProbeContext context, string filterType)
    {
        var scope = context.CreateScope("filters");

        scope.Add("filterType", filterType);

        return scope;
    }

    /// <summary>
    /// Creates consumer factory scope.
    /// </summary>
    /// <typeparam name="TConsumer">The t consumer type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="source">The source value.</param>
    /// <returns>The result of the operation.</returns>
    public static ProbeContext CreateConsumerFactoryScope<TConsumer>(this ProbeContext context, string source)
    {
        var scope = context.CreateScope("consumerFactory");
        scope.Add("source", source);
        scope.Add("consumerType", TypeCache<TConsumer>.ShortName);

        return scope;
    }

    /// <summary>
    /// Creates message scope.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="messageType">The message type value.</param>
    /// <returns>The result of the operation.</returns>
    public static ProbeContext CreateMessageScope(this ProbeContext context, string messageType)
    {
        var scope = context.CreateScope(messageType);

        return scope;
    }
}
