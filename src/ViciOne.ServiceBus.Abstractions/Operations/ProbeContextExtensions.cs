namespace ViciOne.ServiceBus.Operations;

/// <summary>Creates the standard diagnostic scopes emitted by message-pipeline components.</summary>
public static class ProbeContextExtensions
{
    /// <summary>Creates a pipeline-filter scope and records its logical filter type.</summary>
    /// <param name="context">The parent diagnostic scope.</param>
    /// <param name="filterType">The non-empty logical filter type.</param>
    /// <returns>The child scope for additional filter details.</returns>
    public static ProbeContext CreateFilterScope(this ProbeContext context, string filterType)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(filterType);

        var scope = context.CreateScope("filters");

        scope.Add("filterType", filterType);

        return scope;
    }

    /// <summary>Creates a consumer-factory scope and records its source and consumer type.</summary>
    /// <typeparam name="TConsumer">The consumer type created by the factory.</typeparam>
    /// <param name="context">The parent diagnostic scope.</param>
    /// <param name="source">The non-empty logical source of the factory.</param>
    /// <returns>The child scope for additional factory details.</returns>
    public static ProbeContext CreateConsumerFactoryScope<TConsumer>(this ProbeContext context, string source)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        var scope = context.CreateScope("consumerFactory");
        scope.Add("source", source);
        scope.Add("consumerType", TypeCache<TConsumer>.ShortName);

        return scope;
    }

    /// <summary>Creates a scope identified by a message-contract name.</summary>
    /// <param name="context">The parent diagnostic scope.</param>
    /// <param name="messageType">The non-empty message-contract name.</param>
    /// <returns>The message scope.</returns>
    public static ProbeContext CreateMessageScope(this ProbeContext context, string messageType)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageType);

        return context.CreateScope(messageType);
    }
}
