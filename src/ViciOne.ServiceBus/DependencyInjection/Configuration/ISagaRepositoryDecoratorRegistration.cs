namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Registration seam used to decorate a saga repository without coupling the runtime core to testing or diagnostics packages.
/// </summary>
public interface ISagaRepositoryDecoratorRegistration<TSaga>
    where TSaga : class, ISaga
{
    /// <summary>
    /// Performs the decorate saga repository operation.
    /// </summary>
    /// <param name="repository">The repository value.</param>
    /// <returns>The result of the operation.</returns>
    ISagaRepository<TSaga> DecorateSagaRepository(ISagaRepository<TSaga> repository);
}
