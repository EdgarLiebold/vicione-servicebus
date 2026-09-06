namespace ViciOne.ServiceBus.Operations;

/// <summary>Provides extension methods for probe context.</summary>
public static class ProbeContextExtensions
{
    /// <summary>Creates filter scope.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="filterType">The runtime filter type used by the operation.</param>
    /// <returns>The created filter scope.</returns>
    public static ProbeContext CreateFilterScope(this ProbeContext context, string filterType)
    {
        var scope = context.CreateScope("filters");

        scope.Add("filterType", filterType);

        return scope;
    }

    /// <summary>Creates consumer factory scope.</summary>
    /// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="source">The source value.</param>
    /// <returns>The created consumer factory scope.</returns>
    public static ProbeContext CreateConsumerFactoryScope<TConsumer>(this ProbeContext context, string source)
    {
        var scope = context.CreateScope("consumerFactory");
        scope.Add("source", source);
        scope.Add("consumerType", TypeCache<TConsumer>.ShortName);

        return scope;
    }

    /// <summary>Creates message scope.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <returns>The created message scope.</returns>
    public static ProbeContext CreateMessageScope(this ProbeContext context, string messageType)
    {
        var scope = context.CreateScope(messageType);

        return scope;
    }
}
