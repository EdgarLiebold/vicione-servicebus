using System.Threading.Tasks;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Provides container scope for the consumer, either at the general level or the message-specific level.</summary>
public interface IConsumeScopeProvider :
    IProbeSite
{
    /// <summary>Gets scope.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    ValueTask<IConsumeScopeContext> GetScopeAsync(ConsumeContext context, CancellationToken cancellationToken = default);

    /// <summary>Gets scope.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    ValueTask<IConsumeScopeContext<T>> GetScopeAsync<T>(ConsumeContext<T> context, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Gets scope.</summary>
    /// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    ValueTask<IConsumerConsumeScopeContext<TConsumer, T>> GetScopeAsync<TConsumer, T>(ConsumeContext<T> context, CancellationToken cancellationToken = default)
        where TConsumer : class
        where T : class;
}
