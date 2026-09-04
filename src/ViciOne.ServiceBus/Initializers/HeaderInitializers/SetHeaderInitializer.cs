using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Initializers.HeaderInitializers;

/// <summary>
/// Set a header to a constant value from the input
/// </summary>
/// <typeparam name="TMessage"></typeparam>
/// <typeparam name="TInput"></typeparam>
/// <typeparam name="THeader">The header type</typeparam>
public class SetHeaderInitializer<TMessage, TInput, THeader> :
    IHeaderInitializer<TMessage, TInput>
    where TMessage : class
    where TInput : class
{
    readonly string _headerName;
    readonly IPropertyProvider<TInput, THeader> _provider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="headerName">The header name value.</param>
    /// <param name="provider">The service provider.</param>
    public SetHeaderInitializer(string headerName, IPropertyProvider<TInput, THeader> provider)
    {
        if (headerName == null)
            throw new ArgumentNullException(nameof(headerName));

        _headerName = headerName;
        _provider = provider;
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="sendContext">The send context value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task ApplyAsync(InitializeContext<TMessage, TInput> context, SendContext sendContext, CancellationToken cancellationToken = default)
    {
        Task<THeader?> propertyTask = _provider.GetPropertyAsync(context, cancellationToken: cancellationToken);
        if (propertyTask.IsCompleted)
        {
            sendContext.Headers.Set(_headerName, propertyTask.Result);
            return Task.CompletedTask;
        }

        async Task ApplyAsync()
        {
            var value = await propertyTask.ConfigureAwait(false);

            sendContext.Headers.Set(_headerName, value);
        }

        return ApplyAsync();
    }
}
