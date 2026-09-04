using System;
using System.Reflection;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Initializers.PropertyInitializers;

/// <summary>
/// Provides a copy object property initializer implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
/// <typeparam name="TInput">The t input type.</typeparam>
/// <typeparam name="TInputProperty">The t input property type.</typeparam>
public class CopyObjectPropertyInitializer<TMessage, TInput, TInputProperty> :
    IPropertyInitializer<TMessage, TInput>
    where TMessage : class
    where TInput : class
{
    readonly IReadProperty<TInput, TInputProperty> _inputProperty;
    readonly IWriteProperty<TMessage, object> _messageProperty;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="messagePropertyInfo">The message property info value.</param>
    /// <param name="inputPropertyInfo">The input property info value.</param>
    public CopyObjectPropertyInitializer(PropertyInfo messagePropertyInfo, PropertyInfo inputPropertyInfo)
    {
        if (messagePropertyInfo == null)
            throw new ArgumentNullException(nameof(messagePropertyInfo));

        _inputProperty = ReadPropertyCache<TInput>.GetProperty<TInputProperty>(inputPropertyInfo);
        _messageProperty = WritePropertyCache<TMessage>.GetProperty<object>(messagePropertyInfo);
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task ApplyAsync(InitializeContext<TMessage, TInput> context, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); _messageProperty.Set(context.Message, _inputProperty.Get(context.Input));

        return Task.CompletedTask;
    }
}
