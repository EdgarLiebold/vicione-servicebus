using System;
using System.Reflection;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals.Reflection;

namespace ViciOne.ServiceBus.Initializers.PropertyInitializers;

/// <summary>
/// Copies a same-type input property into a writable message property, including a null value.
/// </summary>
/// <typeparam name="TMessage">The message contract being populated.</typeparam>
/// <typeparam name="TInput">The input object containing the value.</typeparam>
/// <typeparam name="TProperty">The shared input and message property type.</typeparam>
internal sealed class CopyPropertyInitializer<TMessage, TInput, TProperty> :
    IPropertyInitializer<TMessage, TInput>
    where TMessage : class
    where TInput : class
{
    readonly IReadProperty<TInput, TProperty> _inputProperty;
    readonly IWriteProperty<TMessage, TProperty> _messageProperty;

    /// <summary>Creates a same-type input-to-message property mapping.</summary>
    /// <param name="messagePropertyInfo">The writable message property.</param>
    /// <param name="inputPropertyInfo">The readable input property.</param>
    public CopyPropertyInitializer(PropertyInfo messagePropertyInfo, PropertyInfo inputPropertyInfo)
    {
        if (messagePropertyInfo == null)
            throw new ArgumentNullException(nameof(messagePropertyInfo));
        if (inputPropertyInfo == null)
            throw new ArgumentNullException(nameof(inputPropertyInfo));

        _inputProperty = ReadPropertyCache<TInput>.GetProperty<TProperty>(inputPropertyInfo);
        _messageProperty = WritePropertyCache<TMessage>.GetProperty<TProperty>(messagePropertyInfo);
    }

    /// <summary>Copies the input property into the message when input is available.</summary>
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
