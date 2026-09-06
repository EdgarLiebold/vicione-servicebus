namespace ViciOne.ServiceBus.Advanced;

/// <summary>Creates transport-safe endpoint names from application type names and tags.</summary>
public interface IEndpointNameFormatter
{
    /// <summary>Gets the separator placed between words in a formatted endpoint name.</summary>
    string Separator { get; }

    /// <summary>Creates a unique temporary endpoint name containing a tag.</summary>
    /// <param name="tag">The tag included in the temporary name.</param>
    /// <returns>The formatted temporary endpoint name.</returns>
    string TemporaryEndpoint(string tag);

    /// <summary>Formats the endpoint name for the selected consumer type.</summary>
    /// <typeparam name="T">The consumer implementation type.</typeparam>
    /// <returns>The formatted consumer endpoint name.</returns>
    string Consumer<T>()
        where T : class, IConsumer;

    /// <summary>Formats an endpoint name for a message contract.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <returns>The formatted message endpoint name.</returns>
    string Message<T>()
        where T : class;

    /// <summary>Formats an endpoint name for a saga.</summary>
    /// <typeparam name="T">The saga type.</typeparam>
    /// <returns>The formatted saga endpoint name.</returns>
    string Saga<T>()
        where T : class;

    /// <summary>Formats the execute endpoint name for a routing-slip activity.</summary>
    /// <typeparam name="T">The activity implementation type.</typeparam>
    /// <typeparam name="TArguments">The activity arguments type.</typeparam>
    /// <returns>The formatted execute endpoint name.</returns>
    string ExecuteActivity<T, TArguments>()
        where T : class
        where TArguments : class;

    /// <summary>Formats the compensation endpoint name for a routing-slip activity.</summary>
    /// <typeparam name="T">The activity implementation type.</typeparam>
    /// <typeparam name="TLog">The activity compensation-log type.</typeparam>
    /// <returns>The formatted compensation endpoint name.</returns>
    string CompensateActivity<T, TLog>()
        where T : class
        where TLog : class;

    /// <summary>
    /// Normalizes an arbitrary name using the formatter's casing and separator rules.
    /// </summary>
    /// <param name="name">The name to normalize.</param>
    /// <returns>The normalized endpoint name.</returns>
    string SanitizeName(string name);
}
