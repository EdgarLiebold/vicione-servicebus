// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Transports
{
    using System.Threading.Tasks;


    public interface IPublishPipe :
        IProbeSite
    {
        Task Send<T>(PublishContext<T> context)
            where T : class;
    }
}
