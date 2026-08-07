// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus;

using System.Data;
using DapperIntegration.Saga;


public delegate DatabaseContext<TSaga> DatabaseContextFactory<TSaga>(IDbConnection connection, IDbTransaction transaction)
    where TSaga : class, ISaga;
