using System.Threading.Tasks;
using ViciOne.ServiceBus.DependencyInjection;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Provides a scope consume filter implementation.
/// </summary>
public class ScopeConsumeFilter :
    IFilter<ConsumeContext>
{
    readonly IConsumeScopeProvider _scopeProvider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="scopeProvider">The scope provider value.</param>
    public ScopeConsumeFilter(IConsumeScopeProvider scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task SendAsync(ConsumeContext context, IPipe<ConsumeContext> next)
    {
        await using var scope = await _scopeProvider.GetScopeAsync(context).ConfigureAwait(false);

        await next.SendAsync(scope.Context).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("scope");
    }
}
