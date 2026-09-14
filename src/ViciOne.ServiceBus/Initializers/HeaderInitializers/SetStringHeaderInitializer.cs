using System;
using System.Reflection;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals.Reflection;

namespace ViciOne.ServiceBus.Initializers.HeaderInitializers;

/// <summary>Copies a string input property into a named outgoing header.</summary>
/// <typeparam name="TMessage">The initialized message contract.</typeparam>
/// <typeparam name="TInput">The input object containing the header value.</typeparam>
internal sealed class SetStringHeaderInitializer<TMessage, TInput> :
    IHeaderInitializer<TMessage, TInput>
    where TMessage : class
    where TInput : class
{
    readonly string _headerName;
    readonly IReadProperty<TInput, string> _inputProperty;

    /// <summary>Creates an input-property-to-named-header mapping.</summary>
    /// <param name="headerName">The header name.</param>
    /// <param name="propertyInfo">The readable string input property.</param>
    public SetStringHeaderInitializer(string headerName, PropertyInfo propertyInfo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(headerName);
        if (propertyInfo == null)
            throw new ArgumentNullException(nameof(propertyInfo));

        _headerName = headerName;

        _inputProperty = ReadPropertyCache<TInput>.GetProperty<string>(propertyInfo);
    }

    /// <summary>Copies the configured string value into the outgoing headers.</summary>
    /// <param name="context">The initialized message and input object.</param>
    /// <param name="sendContext">The outgoing context whose named header is assigned.</param>
    /// <param name="cancellationToken">The token that cancels header assignment.</param>
    /// <returns>A task that completes after the named header has been assigned.</returns>
    public Task ApplyAsync(InitializeContext<TMessage, TInput> context, SendContext sendContext, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(sendContext);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);
        if (!context.HasInput)
            return Task.CompletedTask;

        var inputPropertyValue = _inputProperty.Get(context.Input);

        sendContext.Headers.Set(_headerName, inputPropertyValue);

        return Task.CompletedTask;
    }
}
