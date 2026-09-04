using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

public interface IOutboxContextFactory<TContext> :
    IProbeSite
    where TContext : class
{
    Task Send<T>(ConsumeContext<T> context, OutboxConsumeOptions options, IPipe<OutboxConsumeContext<T>> next)
        where T : class;
}
