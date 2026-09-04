namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Defines the contract for endpoint name formatter.
/// </summary>
public interface IEndpointNameFormatter
{
    /// <summary>
    /// The separator string used between words
    /// </summary>
    string Separator { get; }

    /// <summary>
    /// Generate a temporary endpoint name, containing the specified tag
    /// </summary>
    /// <param name="tag"></param>
    /// <returns></returns>
    string TemporaryEndpoint(string tag);

    /// <summary>
    /// Consumes r.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    string Consumer<T>()
        where T : class, IConsumer;

    /// <summary>
    /// Performs the message operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    string Message<T>()
        where T : class;

    /// <summary>
    /// Performs the saga operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    string Saga<T>()
        where T : class, ISaga;

    /// <summary>
    /// Performs the execute activity operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <typeparam name="TArguments">The t arguments type.</typeparam>
    /// <returns>The result of the operation.</returns>
    string ExecuteActivity<T, TArguments>()
        where T : class, IExecuteActivity<TArguments>
        where TArguments : class;

    /// <summary>
    /// Performs the compensate activity operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <typeparam name="TLog">The t log type.</typeparam>
    /// <returns>The result of the operation.</returns>
    string CompensateActivity<T, TLog>()
        where T : class, ICompensateActivity<TLog>
        where TLog : class;

    /// <summary>
    /// Clean up a name so that it matches the formatting.
    /// For instance, SubmitOrderControl -> submit-order-control (kebab case)
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    string SanitizeName(string name);
}
