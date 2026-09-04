namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for saga connector.
/// </summary>
public interface ISagaConnector
{
    /// <summary>
    /// Creates saga specification.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    ISagaSpecification<T> CreateSagaSpecification<T>()
        where T : class, ISaga;

    /// <summary>
    /// Connects saga.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="consumePipe">The consume pipe value.</param>
    /// <param name="repository">The repository value.</param>
    /// <param name="specification">The specification value.</param>
    /// <returns>The result of the operation.</returns>
    ConnectHandle ConnectSaga<T>(IConsumePipeConnector consumePipe, ISagaRepository<T> repository, ISagaSpecification<T> specification)
        where T : class, ISaga;
}
