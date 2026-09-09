using System;
using System.Reflection;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Initializers.PropertyInitializers;

/// <summary>Populates one message property from a property-value provider.</summary>
/// <typeparam name="TMessage">The message contract being initialized.</typeparam>
/// <typeparam name="TInput">The input-object type.</typeparam>
/// <typeparam name="TProperty">The populated property type.</typeparam>
public class ProviderPropertyInitializer<TMessage, TInput, TProperty> :
    IPropertyInitializer<TMessage, TInput>
    where TMessage : class
    where TInput : class
{
    readonly IWriteProperty<TMessage, TProperty> _messageProperty;
    readonly IPropertyProvider<TInput, TProperty> _propertyProvider;

    /// <summary>Creates an initializer for a writable message property.</summary>
    /// <param name="propertyProvider">The provider that resolves the property value.</param>
    /// <param name="propertyInfo">The writable message property.</param>
    public ProviderPropertyInitializer(IPropertyProvider<TInput, TProperty>? propertyProvider, PropertyInfo? propertyInfo)
    {
        if (propertyProvider == null)
            throw new ArgumentNullException(nameof(propertyProvider));

        if (propertyInfo == null)
            throw new ArgumentNullException(nameof(propertyInfo));

        _propertyProvider = propertyProvider;

        _messageProperty = WritePropertyCache<TMessage>.GetProperty<TProperty>(propertyInfo);
    }

    /// <summary>Resolves and assigns the property when the runtime message type owns it.</summary>
    /// <param name="context">The message and input object used by the property provider.</param>
    /// <param name="cancellationToken">The token that cancels value resolution.</param>
    /// <returns>A task that completes after property resolution and assignment.</returns>
    public Task ApplyAsync(InitializeContext<TMessage, TInput> context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
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
