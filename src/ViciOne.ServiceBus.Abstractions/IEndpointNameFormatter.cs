namespace ViciOne.ServiceBus.Advanced;

/// <summary>Formats endpoint name values.</summary>
public interface IEndpointNameFormatter
{
    /// <summary>The separator string used between words.</summary>
    string Separator { get; }

    /// <summary>Generate a temporary endpoint name, containing the specified tag.</summary>
    /// <param name="tag">The tag.</param>
    /// <returns>The string produced by the operation.</returns>
    string TemporaryEndpoint(string tag);

    /// <summary>Formats the endpoint name for the selected consumer type.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The string produced by the operation.</returns>
    string Consumer<T>()
        where T : class, IConsumer;

    /// <summary>Applies the message configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The string produced by the operation.</returns>
    string Message<T>()
        where T : class;

    /// <summary>Applies the saga configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The string produced by the operation.</returns>
    string Saga<T>()
        where T : class;

    /// <summary>Executes activity.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <returns>The string produced by the operation.</returns>
    string ExecuteActivity<T, TArguments>()
        where T : class
        where TArguments : class;

    /// <summary>Compensates activity.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <returns>The string produced by the operation.</returns>
    string CompensateActivity<T, TLog>()
        where T : class
        where TLog : class;

    /// <summary>
    /// Clean up a name so that it matches the formatting.
    /// For instance, SubmitOrderControl -> submit-order-control (kebab case).
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns>The string produced by the operation.</returns>
    string SanitizeName(string name);
}
