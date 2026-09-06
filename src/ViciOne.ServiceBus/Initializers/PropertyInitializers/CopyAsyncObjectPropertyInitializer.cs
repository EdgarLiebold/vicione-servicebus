using System;
using System.Reflection;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Initializers.PropertyInitializers;

/// <summary>Initializes copy async object property values.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <typeparam name="TInput">The input type.</typeparam>
/// <typeparam name="TInputProperty">The input property type.</typeparam>
public class CopyAsyncObjectPropertyInitializer<TMessage, TInput, TInputProperty> :
    IPropertyInitializer<TMessage, TInput>
    where TMessage : class
    where TInput : class
{
    readonly IReadProperty<TInput, Task<TInputProperty>> _inputProperty;
    readonly IWriteProperty<TMessage, object> _messageProperty;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="messagePropertyInfo">The message property info.</param>
    /// <param name="inputPropertyInfo">The input property info.</param>
    public CopyAsyncObjectPropertyInitializer(PropertyInfo messagePropertyInfo, PropertyInfo inputPropertyInfo)
    {
        if (messagePropertyInfo == null)
            throw new ArgumentNullException(nameof(messagePropertyInfo));

        _inputProperty = ReadPropertyCache<TInput>.GetProperty<Task<TInputProperty>>(inputPropertyInfo);
        _messageProperty = WritePropertyCache<TMessage>.GetProperty<object>(messagePropertyInfo);
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ApplyAsync(InitializeContext<TMessage, TInput> context, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); if (!context.HasInput)
            return Task.CompletedTask;

        Task<TInputProperty> valueTask = _inputProperty.Get(context.Input);
        if (valueTask.Status == TaskStatus.RanToCompletion)
        {
            _messageProperty.Set(context.Message, valueTask.Result);

            return Task.CompletedTask;
        }

        async Task SetPropertyAsync()
        {
            var value = await valueTask.ConfigureAwait(false);

            _messageProperty.Set(context.Message, value);
        }

        return SetPropertyAsync();
    }
}
