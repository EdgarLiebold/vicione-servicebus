using System;
using System.Reflection;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Initializers.PropertyInitializers;

/// <summary>Copies an awaited input property into an object-valued message property.</summary>
/// <typeparam name="TMessage">The message contract being populated.</typeparam>
/// <typeparam name="TInput">The input object containing the task.</typeparam>
/// <typeparam name="TInputProperty">The task result type.</typeparam>
internal sealed class CopyAsyncObjectPropertyInitializer<TMessage, TInput, TInputProperty> :
    IPropertyInitializer<TMessage, TInput>
    where TMessage : class
    where TInput : class
{
    readonly IReadProperty<TInput, Task<TInputProperty>> _inputProperty;
    readonly IWriteProperty<TMessage, object> _messageProperty;

    /// <summary>Creates a mapping between a task-valued input property and an object-valued message property.</summary>
    /// <param name="messagePropertyInfo">The writable object-valued message property.</param>
    /// <param name="inputPropertyInfo">The readable task-valued input property.</param>
    public CopyAsyncObjectPropertyInitializer(PropertyInfo messagePropertyInfo, PropertyInfo inputPropertyInfo)
    {
        if (messagePropertyInfo == null)
            throw new ArgumentNullException(nameof(messagePropertyInfo));
        if (inputPropertyInfo == null)
            throw new ArgumentNullException(nameof(inputPropertyInfo));

        _inputProperty = ReadPropertyCache<TInput>.GetProperty<Task<TInputProperty>>(inputPropertyInfo);
        _messageProperty = WritePropertyCache<TMessage>.GetProperty<object>(messagePropertyInfo);
    }

    /// <summary>Awaits the input value with caller cancellation and assigns it to the message.</summary>
    /// <param name="context">The message and task-bearing input object.</param>
    /// <param name="cancellationToken">The token that cancels waiting for the input task.</param>
    /// <returns>A task that completes after the input task result has been assigned.</returns>
    public Task ApplyAsync(InitializeContext<TMessage, TInput> context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);
        if (!context.HasInput)
            return Task.CompletedTask;

        Task<TInputProperty> valueTask = _inputProperty.Get(context.Input)
            ?? throw new InvalidOperationException("The input task property returned null.");
        if (valueTask.IsCompletedSuccessfully)
        {
            _messageProperty.Set(context.Message, valueTask.GetAwaiter().GetResult());

            return Task.CompletedTask;
        }

        async Task SetPropertyAsync()
        {
            var value = await valueTask.WaitAsync(cancellationToken).ConfigureAwait(false);

            _messageProperty.Set(context.Message, value);
        }

        return SetPropertyAsync();
    }
}
