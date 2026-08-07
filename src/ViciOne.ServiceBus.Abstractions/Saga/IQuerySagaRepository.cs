// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;


    public interface IQuerySagaRepository<TSaga> :
        IProbeSite
        where TSaga : class, ISaga
    {
        Task<IEnumerable<Guid>> Find(ISagaQuery<TSaga> query);
    }
}
