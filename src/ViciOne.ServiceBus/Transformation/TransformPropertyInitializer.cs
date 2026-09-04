using System;
using System.Reflection;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Transformation;

/// <summary>
/// Set a message property using the property provider for the property value
/// </summary>
/// <typeparam name="TMessage"></typeparam>
/// <typeparam name="TInput"></typeparam>
/// <typeparam name="TProperty"></typeparam>
public class TransformPropertyInitializer<TMessage, TInput, TProperty> :
    IPropertyInitializer<TMessage, TInput>
    where TMessage : class
    where TInput : class
{
    readonly IWriteProperty<TMessage, TProperty> _messageProperty;
    readonly IPropertyProvider<TInput, TProperty> _propertyProvider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="propertyProvider">The property provider value.</param>
    /// <param name="propertyInfo">The property info value.</param>
    public TransformPropertyInitializer(IPropertyProvider<TInput, TProperty> propertyProvider, PropertyInfo propertyInfo)
    {
        if (propertyProvider == null)
            throw new ArgumentNullException(nameof(propertyProvider));

        if (propertyInfo == null)
            throw new ArgumentNullException(nameof(propertyInfo));

        _propertyProvider = propertyProvider;

        _messageProperty = WritePropertyCache<TMessage>.GetProperty<TProperty>(propertyInfo);
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task ApplyAsync(InitializeContext<TMessage, TInput> context, CancellationToken cancellationToken = default)
    {
        Task<TProperty?> propertyTask = _propertyProvider.GetPropertyAsync(context, cancellationToken: cancellationToken);
        if (propertyTask.IsCompleted)
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
