// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Transactions
{
    using System.Threading.Tasks;


    public interface ITransactionalBus :
        IBus
    {
        Task Release();
    }
}
