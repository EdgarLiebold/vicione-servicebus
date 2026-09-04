using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

public interface IOutboxContextFactory<TContext> :
    IProbeSite
    where TContext : class
{
    Task SendAsync<T>(ConsumeContext<T> context, OutboxConsumeOptions options, IPipe<OutboxConsumeContext<T>> next, CancellationToken cancellationToken = default)
        where T : class;
}
