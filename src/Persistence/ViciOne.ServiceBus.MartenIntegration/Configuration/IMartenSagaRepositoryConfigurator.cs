// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;
    using Marten;


    public interface IMartenSagaRepositoryConfigurator
    {
        [Obsolete("Use AddMarten to configure the connection. Visit https://github.com/EdgarLiebold/vicione-servicebus/obsolete for details.")]
        void Connection(string connectionString, Action<StoreOptions> configure = null);
    }


    public interface IMartenSagaRepositoryConfigurator<TSaga> :
        IMartenSagaRepositoryConfigurator
        where TSaga : class, ISaga
    {
    }
}
