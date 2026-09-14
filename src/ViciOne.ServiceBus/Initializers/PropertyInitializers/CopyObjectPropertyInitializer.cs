using System;
using System.Reflection;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals.Reflection;

namespace ViciOne.ServiceBus.Initializers.PropertyInitializers;

/// <summary>Copies an input property into an object-valued message property.</summary>
/// <typeparam name="TMessage">The message contract being populated.</typeparam>
/// <typeparam name="TInput">The input object containing the value.</typeparam>
/// <typeparam name="TInputProperty">The runtime value type preserved in the object property.</typeparam>
internal sealed class CopyObjectPropertyInitializer<TMessage, TInput, TInputProperty> :
    IPropertyInitializer<TMessage, TInput>
    where TMessage : class
    where TInput : class
{
    readonly IReadProperty<TInput, TInputProperty> _inputProperty;
    readonly IWriteProperty<TMessage, object> _messageProperty;

    /// <summary>Creates a mapping to an object-valued message property.</summary>
    /// <param name="messagePropertyInfo">The writable object-valued message property.</param>
    /// <param name="inputPropertyInfo">The readable input property.</param>
    public CopyObjectPropertyInitializer(PropertyInfo messagePropertyInfo, PropertyInfo inputPropertyInfo)
    {
        if (messagePropertyInfo == null)
            throw new ArgumentNullException(nameof(messagePropertyInfo));
        if (inputPropertyInfo == null)
            throw new ArgumentNullException(nameof(inputPropertyInfo));

        _inputProperty = ReadPropertyCache<TInput>.GetProperty<TInputProperty>(inputPropertyInfo);
        _messageProperty = WritePropertyCache<TMessage>.GetProperty<object>(messagePropertyInfo);
    }

    /// <summary>Copies the configured input value into the message.</summary>
    /// <param name="context">The message and input object.</param>
    /// <param name="cancellationToken">The token that cancels property assignment.</param>
    /// <returns>A task that completes after the value has been assigned.</returns>
    public Task ApplyAsync(InitializeContext<TMessage, TInput> context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);
        if (context.HasInput)
            _messageProperty.Set(context.Message, _inputProperty.Get(context.Input));

        return Task.CompletedTask;
    }
}
