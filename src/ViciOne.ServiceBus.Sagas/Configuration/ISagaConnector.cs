namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines the operations required by saga connector.</summary>
public interface ISagaConnector
{
    /// <summary>Creates saga specification.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The created saga specification.</returns>
    ISagaSpecification<T> CreateSagaSpecification<T>()
        where T : class, ISaga;

    /// <summary>Connects saga.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="consumePipe">The consume pipe.</param>
    /// <param name="repository">The repository.</param>
    /// <param name="specification">The specification.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectSaga<T>(IConsumePipeConnector consumePipe, ISagaRepository<T> repository, ISagaSpecification<T> specification)
        where T : class, ISaga;
}
