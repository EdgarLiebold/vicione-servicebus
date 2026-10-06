using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Creates outbox context instances.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public interface IOutboxContextFactory<TContext> :
    IProbeSite
    where TContext : class
{
    /// <summary>Coordinates outbox-aware consumption of the supplied message through the next pipeline stage according to the provider's inbox and delivery policy.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SendAsync<T>(ConsumeContext<T> context, OutboxConsumeOptions options, IPipe<OutboxConsumeContext<T>> next, CancellationToken cancellationToken = default)
        where T : class;
}
