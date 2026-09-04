using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Defines the contract for outbox context factory.
/// </summary>
/// <typeparam name="TContext">The t context type.</typeparam>
public interface IOutboxContextFactory<TContext> :
    IProbeSite
    where TContext : class
{
    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="options">The options value.</param>
    /// <param name="next">The next value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task SendAsync<T>(ConsumeContext<T> context, OutboxConsumeOptions options, IPipe<OutboxConsumeContext<T>> next, CancellationToken cancellationToken = default)
        where T : class;
}
