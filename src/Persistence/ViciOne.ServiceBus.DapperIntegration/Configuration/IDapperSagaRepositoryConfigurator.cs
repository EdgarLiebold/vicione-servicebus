// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus;

using System.Data;


public interface IDapperSagaRepositoryConfigurator
{
    IsolationLevel IsolationLevel { set; }
}


public interface IDapperSagaRepositoryConfigurator<TSaga> :
    IDapperSagaRepositoryConfigurator
    where TSaga : class, ISaga
{
    /// <summary>
    /// Set the database context factory to allow customization of the Dapper interaction/queries
    /// </summary>
    DatabaseContextFactory<TSaga> ContextFactory { set; }
}
