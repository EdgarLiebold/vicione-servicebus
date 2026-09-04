using System;
using System.Reflection;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Initializers.PropertyInitializers;

/// <summary>
/// Set a message property by copying the input property (of the same type), regardless of whether
/// the input property value is null, etc.
/// </summary>
/// <typeparam name="TMessage"></typeparam>
/// <typeparam name="TInput"></typeparam>
/// <typeparam name="TProperty"></typeparam>
public class CopyPropertyInitializer<TMessage, TInput, TProperty> :
    IPropertyInitializer<TMessage, TInput>
    where TMessage : class
    where TInput : class
{
    readonly IReadProperty<TInput, TProperty> _inputProperty;
    readonly IWriteProperty<TMessage, TProperty> _messageProperty;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="messagePropertyInfo">The message property info value.</param>
    /// <param name="inputPropertyInfo">The input property info value.</param>
    public CopyPropertyInitializer(PropertyInfo messagePropertyInfo, PropertyInfo inputPropertyInfo)
    {
        if (messagePropertyInfo == null)
            throw new ArgumentNullException(nameof(messagePropertyInfo));

        _inputProperty = ReadPropertyCache<TInput>.GetProperty<TProperty>(inputPropertyInfo);
        _messageProperty = WritePropertyCache<TMessage>.GetProperty<TProperty>(messagePropertyInfo);
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task ApplyAsync(InitializeContext<TMessage, TInput> context, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); if (context.HasInput)
            _messageProperty.Set(context.Message, _inputProperty.Get(context.Input));

        return Task.CompletedTask;
    }
}
