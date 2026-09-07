namespace ViciOne.ServiceBus.Configuration;

/// <summary>Registration seam used to decorate a saga repository without coupling the runtime core to testing or diagnostics packages.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public interface ISagaRepositoryDecoratorRegistration<TSaga>
    where TSaga : class, ISaga
{
    /// <summary>Decorates saga repository.</summary>
    /// <param name="repository">The repository.</param>
    /// <returns>The saga repository produced by the operation.</returns>
    ISagaRepository<TSaga> DecorateSagaRepository(ISagaRepository<TSaga> repository);
}
