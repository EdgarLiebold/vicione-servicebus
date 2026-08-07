// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.AzureCosmos.Saga
{
    using System;
    using Microsoft.Azure.Cosmos;


    public interface DatabaseContext<TSaga>
        where TSaga : class, ISaga
    {
        Container Container { get; }
        Action<QueryRequestOptions> QueryRequestOptions { get; }

        ItemRequestOptions GetItemRequestOptions();

        CosmosLinqSerializerOptions GetLinqSerializerOptions();
    }
}
