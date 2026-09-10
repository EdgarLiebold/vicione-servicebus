using System;
using System.Reflection;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Transformation;

/// <summary>Sets a message property to the value produced by a transform provider.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <typeparam name="TInput">The input type.</typeparam>
/// <typeparam name="TProperty">The property type.</typeparam>
internal sealed class TransformPropertyInitializer<TMessage, TInput, TProperty> :
    IPropertyInitializer<TMessage, TInput>
    where TMessage : class
    where TInput : class
{
    readonly IWriteProperty<TMessage, TProperty> _messageProperty;
    readonly IPropertyProvider<TInput, TProperty> _propertyProvider;

    /// <summary>Creates an initializer for one writable message property.</summary>
    /// <param name="propertyProvider">The provider that produces the transformed property value.</param>
    /// <param name="propertyInfo">The writable target property.</param>
    public TransformPropertyInitializer(IPropertyProvider<TInput, TProperty> propertyProvider, PropertyInfo propertyInfo)
    {
        _propertyProvider = propertyProvider ?? throw new ArgumentNullException(nameof(propertyProvider));
        ArgumentNullException.ThrowIfNull(propertyInfo);

        _messageProperty = WritePropertyCache<TMessage>.GetProperty<TProperty>(propertyInfo);
    }

    /// <summary>Resolves and assigns the transformed property when this initializer owns the runtime message type.</summary>
    /// <param name="context">The message initialization context.</param>
    /// <param name="cancellationToken">The token used to cancel property resolution.</param>
    /// <returns>A task that completes after property resolution and conditional assignment.</returns>
    public Task ApplyAsync(InitializeContext<TMessage, TInput> context, CancellationToken cancellationToken = default)
    {
        Task<TProperty?> propertyTask = _propertyProvider.GetPropertyAsync(context, cancellationToken: cancellationToken);
        if (propertyTask.IsCompletedSuccessfully)
        {
            if (_messageProperty.TargetType == context.MessageType)
                _messageProperty.Set(context.Message, propertyTask.Result!);
            return Task.CompletedTask;
        }

        async Task ApplyAsync()
        {
            var propertyValue = await propertyTask.ConfigureAwait(false);

            if (_messageProperty.TargetType == context.MessageType)
                _messageProperty.Set(context.Message, propertyValue!);
        }

        return ApplyAsync();
    }
}
