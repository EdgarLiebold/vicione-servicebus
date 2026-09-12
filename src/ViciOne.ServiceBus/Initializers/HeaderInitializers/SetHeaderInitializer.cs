using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Initializers.HeaderInitializers;

/// <summary>Sets a named outgoing header from an initializer property provider.</summary>
/// <typeparam name="TMessage">The initialized message contract.</typeparam>
/// <typeparam name="TInput">The input object used to resolve the header.</typeparam>
/// <typeparam name="THeader">The header value type.</typeparam>
internal sealed class SetHeaderInitializer<TMessage, TInput, THeader> :
    IHeaderInitializer<TMessage, TInput>
    where TMessage : class
    where TInput : class
{
    readonly string _headerName;
    readonly IPropertyProvider<TInput, THeader> _provider;

    /// <summary>Creates a provider-backed named-header mapping.</summary>
    /// <param name="headerName">The header name.</param>
    /// <param name="provider">The provider that resolves the header value.</param>
    public SetHeaderInitializer(string headerName, IPropertyProvider<TInput, THeader> provider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(headerName);
        ArgumentNullException.ThrowIfNull(provider);

        _headerName = headerName;
        _provider = provider;
    }

    /// <summary>Resolves and assigns the named outgoing header.</summary>
    /// <param name="context">The initialized message and input object.</param>
    /// <param name="sendContext">The outgoing context whose header is assigned.</param>
    /// <param name="cancellationToken">The token that cancels value resolution.</param>
    /// <returns>A task that completes after the header value has been resolved and assigned.</returns>
    public Task ApplyAsync(InitializeContext<TMessage, TInput> context, SendContext sendContext, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(sendContext);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        Task<THeader?> propertyTask = _provider.GetPropertyAsync(context, cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The property provider returned a null task.");
        if (propertyTask.IsCompletedSuccessfully)
        {
            sendContext.Headers.Set(_headerName, propertyTask.GetAwaiter().GetResult());
            return Task.CompletedTask;
        }

        async Task SetHeaderAsync()
        {
            var value = await propertyTask.WaitAsync(cancellationToken).ConfigureAwait(false);

            sendContext.Headers.Set(_headerName, value);
        }

        return SetHeaderAsync();
    }
}
