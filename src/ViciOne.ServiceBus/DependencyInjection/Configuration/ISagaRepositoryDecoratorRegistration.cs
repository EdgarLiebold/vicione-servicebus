namespace ViciOne.ServiceBus.Configuration
{
    /// <summary>
    /// Registration seam used to decorate a saga repository without coupling the runtime core to testing or diagnostics packages.
    /// </summary>
    public interface ISagaRepositoryDecoratorRegistration<TSaga>
        where TSaga : class, ISaga
    {
        ISagaRepository<TSaga> DecorateSagaRepository(ISagaRepository<TSaga> repository);
    }
}
