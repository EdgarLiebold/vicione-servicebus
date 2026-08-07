// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;
    using System.Threading.Tasks;


    public interface ILoadSagaRepository<TSaga> :
        IProbeSite
        where TSaga : class, ISaga
    {
        Task<TSaga> Load(Guid correlationId);
    }
}
