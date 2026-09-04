using System.Threading.Tasks;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>
/// Provides container scope for the consumer, either at the general level or the message-specific level.
/// </summary>
public interface IConsumeScopeProvider :
    IProbeSite
{
    /// <summary>
    /// Gets scope.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    ValueTask<IConsumeScopeContext> GetScopeAsync(ConsumeContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets scope.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    ValueTask<IConsumeScopeContext<T>> GetScopeAsync<T>(ConsumeContext<T> context, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Gets scope.
    /// </summary>
    /// <typeparam name="TConsumer">The t consumer type.</typeparam>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    ValueTask<IConsumerConsumeScopeContext<TConsumer, T>> GetScopeAsync<TConsumer, T>(ConsumeContext<T> context, CancellationToken cancellationToken = default)
        where TConsumer : class
        where T : class;
}
