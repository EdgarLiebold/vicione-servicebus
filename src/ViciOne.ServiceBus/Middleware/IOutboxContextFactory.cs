// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Middleware
{
    using System.Threading.Tasks;


    public interface IOutboxContextFactory<TContext> :
        IProbeSite
        where TContext : class
    {
        Task Send<T>(ConsumeContext<T> context, OutboxConsumeOptions options, IPipe<OutboxConsumeContext<T>> next)
            where T : class;
    }
}
