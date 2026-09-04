using System.Threading.Tasks;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>
/// Provides a scope consumer factory implementation.
/// </summary>
/// <typeparam name="TConsumer">The t consumer type.</typeparam>
public class ScopeConsumerFactory<TConsumer> :
    IConsumerFactory<TConsumer>
    where TConsumer : class
{
    readonly IConsumeScopeProvider _scopeProvider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="scopeProvider">The scope provider value.</param>
    public ScopeConsumerFactory(IConsumeScopeProvider scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task SendAsync<TMessage>(ConsumeContext<TMessage> context, IPipe<ConsumerConsumeContext<TConsumer, TMessage>> next)
        where TMessage : class
    {
        await using IConsumerConsumeScopeContext<TConsumer, TMessage> scope = await _scopeProvider.GetScopeAsync<TConsumer, TMessage>(context);

        await next.SendAsync(scope.Context).ConfigureAwait(false);
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        var scope = context.CreateConsumerFactoryScope<TConsumer>("scope");
        _scopeProvider.Probe(scope);
    }
}
