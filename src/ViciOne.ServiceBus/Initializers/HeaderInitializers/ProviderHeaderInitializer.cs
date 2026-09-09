using System;
using System.Reflection;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Initializers.HeaderInitializers;

/// <summary>Populates one send-context property from a message-initializer value provider.</summary>
/// <typeparam name="TMessage">The message contract being initialized.</typeparam>
/// <typeparam name="TInput">The input-object type.</typeparam>
/// <typeparam name="TProperty">The send-context property type.</typeparam>
public class ProviderHeaderInitializer<TMessage, TInput, TProperty> :
    IHeaderInitializer<TMessage, TInput>
    where TMessage : class
    where TInput : class
{
    readonly IWriteProperty<SendContext, TProperty> _messageProperty;
    readonly IPropertyProvider<TInput, TProperty> _propertyProvider;

    /// <summary>Creates an initializer for a writable send-context property.</summary>
    /// <param name="propertyProvider">The provider that resolves the property value.</param>
    /// <param name="propertyInfo">The writable send-context property.</param>
    public ProviderHeaderInitializer(IPropertyProvider<TInput, TProperty> propertyProvider, PropertyInfo propertyInfo)
    {
        if (propertyProvider == null)
            throw new ArgumentNullException(nameof(propertyProvider));

        if (propertyInfo == null)
            throw new ArgumentNullException(nameof(propertyInfo));

        _propertyProvider = propertyProvider;

        _messageProperty = WritePropertyCache<SendContext>.GetProperty<TProperty>(propertyInfo);
    }

    /// <summary>Resolves and assigns the send-context property.</summary>
    /// <param name="context">The initialized message and input object used by the property provider.</param>
    /// <param name="sendContext">The outgoing context whose property is assigned.</param>
    /// <param name="cancellationToken">The token that cancels value resolution.</param>
    /// <returns>A task that completes after property resolution and assignment.</returns>
    public Task ApplyAsync(InitializeContext<TMessage, TInput> context, SendContext sendContext, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(sendContext);
        Task<TProperty?> propertyTask = _propertyProvider.GetPropertyAsync(context, cancellationToken: cancellationToken);
        if (propertyTask.IsCompletedSuccessfully)
        {
            _messageProperty.Set(sendContext, propertyTask.Result!);
            return Task.CompletedTask;
        }

        return ApplyAsync(sendContext, propertyTask);
    }

    async Task ApplyAsync(SendContext sendContext, Task<TProperty?> propertyTask)
    {
        var propertyValue = await propertyTask.ConfigureAwait(false);

        _messageProperty.Set(sendContext, propertyValue!);
    }
}
