// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Transports
{
    using System.Threading.Tasks;


    public interface ISendContextPipe
    {
        Task Send<T>(SendContext<T> context)
            where T : class;
    }
}
