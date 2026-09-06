using System;
using System.Reflection;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Initializers.HeaderInitializers;

/// <summary>Set a header to a constant value from the input.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <typeparam name="TInput">The input type.</typeparam>
public class SetStringHeaderInitializer<TMessage, TInput> :
    IHeaderInitializer<TMessage, TInput>
    where TMessage : class
    where TInput : class
{
    readonly string _headerName;
    readonly IReadProperty<TInput, string> _inputProperty;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="headerName">The header name.</param>
    /// <param name="propertyInfo">The property info.</param>
    public SetStringHeaderInitializer(string headerName, PropertyInfo propertyInfo)
    {
        if (headerName == null)
            throw new ArgumentNullException(nameof(headerName));

        _headerName = headerName;

        _inputProperty = ReadPropertyCache<TInput>.GetProperty<string>(propertyInfo);
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="sendContext">The send context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ApplyAsync(InitializeContext<TMessage, TInput> context, SendContext sendContext, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); var inputPropertyValue = _inputProperty.Get(context.Input);

        sendContext.Headers.Set(_headerName, inputPropertyValue);

        return Task.CompletedTask;
    }
}
