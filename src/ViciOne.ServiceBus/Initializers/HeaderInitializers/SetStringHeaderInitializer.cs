using System;
using System.Reflection;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Initializers.HeaderInitializers;

/// <summary>
/// Set a header to a constant value from the input
/// </summary>
/// <typeparam name="TMessage"></typeparam>
/// <typeparam name="TInput"></typeparam>
public class SetStringHeaderInitializer<TMessage, TInput> :
    IHeaderInitializer<TMessage, TInput>
    where TMessage : class
    where TInput : class
{
    readonly string _headerName;
    readonly IReadProperty<TInput, string> _inputProperty;

    public SetStringHeaderInitializer(string headerName, PropertyInfo propertyInfo)
    {
        if (headerName == null)
            throw new ArgumentNullException(nameof(headerName));

        _headerName = headerName;

        _inputProperty = ReadPropertyCache<TInput>.GetProperty<string>(propertyInfo);
    }

    public Task ApplyAsync(InitializeContext<TMessage, TInput> context, SendContext sendContext, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); var inputPropertyValue = _inputProperty.Get(context.Input);

        sendContext.Headers.Set(_headerName, inputPropertyValue);

        return Task.CompletedTask;
    }
}
