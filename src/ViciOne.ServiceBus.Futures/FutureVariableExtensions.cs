using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.Futures;

/// <summary>Stores and retrieves typed values in a future's durable variable collection.</summary>
public static class FutureVariableExtensions
{
    /// <summary>Creates and stores a named durable value from a future event.</summary>
    /// <typeparam name="T">The event contract available to the value factory.</typeparam>
    /// <typeparam name="TValue">The stored value type.</typeparam>
    /// <param name="context">The future event context whose state receives the value.</param>
    /// <param name="key">The nonempty variable name.</param>
    /// <param name="factory">The asynchronous value factory.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The created value.</returns>
    public static async Task<TValue> SetVariableAsync<T, TValue>(this BehaviorContext<FutureState, T> context, string key,
        AsyncEventMessageFactory<FutureState, T, TValue> factory, CancellationToken cancellationToken = default)
        where T : class
        where TValue : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(factory);
        cancellationToken.ThrowIfCancellationRequested();
        var value = await factory(context).ConfigureAwait(false);
        ArgumentNullException.ThrowIfNull(value);

        context.Saga.Variables[key] = value;

        return value;
    }

    /// <summary>Creates and stores a named durable value from future state.</summary>
    /// <typeparam name="TValue">The stored value type.</typeparam>
    /// <param name="context">The future state context that receives the value.</param>
    /// <param name="key">The nonempty variable name.</param>
    /// <param name="factory">The asynchronous value factory.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The created value.</returns>
    public static async Task<TValue> SetVariableAsync<TValue>(this BehaviorContext<FutureState> context, string key,
        AsyncEventMessageFactory<FutureState, TValue> factory, CancellationToken cancellationToken = default)
        where TValue : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(factory);
        cancellationToken.ThrowIfCancellationRequested();
        var value = await factory(context).ConfigureAwait(false);
        ArgumentNullException.ThrowIfNull(value);

        context.Saga.Variables[key] = value;

        return value;
    }

    /// <summary>Creates and stores a named durable value from a future event.</summary>
    /// <typeparam name="T">The event contract available to the value factory.</typeparam>
    /// <typeparam name="TValue">The stored value type.</typeparam>
    /// <param name="context">The future event context whose state receives the value.</param>
    /// <param name="key">The nonempty variable name.</param>
    /// <param name="factory">The synchronous value factory.</param>
    /// <returns>The created value.</returns>
    public static TValue SetVariable<T, TValue>(this BehaviorContext<FutureState, T> context, string key,
        EventMessageFactory<FutureState, T, TValue> factory)
        where T : class
        where TValue : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(factory);
        var value = factory(context);
        ArgumentNullException.ThrowIfNull(value);

        context.Saga.Variables[key] = value;

        return value;
    }

    /// <summary>Creates and stores a named durable value from future state.</summary>
    /// <typeparam name="TValue">The stored value type.</typeparam>
    /// <param name="context">The future state context that receives the value.</param>
    /// <param name="key">The nonempty variable name.</param>
    /// <param name="factory">The synchronous value factory.</param>
    /// <returns>The created value.</returns>
    public static TValue SetVariable<TValue>(this BehaviorContext<FutureState> context, string key, EventMessageFactory<FutureState, TValue> factory)
        where TValue : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(factory);
        var value = factory(context);
        ArgumentNullException.ThrowIfNull(value);

        context.Saga.Variables[key] = value;

        return value;
    }

    /// <summary>Adds an activity that creates and stores a named durable value from an event.</summary>
    /// <typeparam name="TData">The event contract available to the value factory.</typeparam>
    /// <typeparam name="TValue">The stored value type.</typeparam>
    /// <param name="binder">The event binder to extend.</param>
    /// <param name="key">The nonempty variable name.</param>
    /// <param name="valueFactory">The synchronous value factory.</param>
    /// <returns>The same binder with the storage activity appended.</returns>
    public static EventActivityBinder<FutureState, TData> SetVariable<TData, TValue>(this EventActivityBinder<FutureState, TData> binder, string key,
        EventMessageFactory<FutureState, TData, TValue> valueFactory)
        where TData : class
        where TValue : class
    {
        ArgumentNullException.ThrowIfNull(binder);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(valueFactory);
        return binder.Add(new ActionActivity<FutureState, TData>(context => context.SetVariable(key, valueFactory)));
    }

    /// <summary>Adds an activity that creates and stores a named durable value from future state.</summary>
    /// <typeparam name="TValue">The stored value type.</typeparam>
    /// <param name="binder">The event binder to extend.</param>
    /// <param name="key">The nonempty variable name.</param>
    /// <param name="valueFactory">The synchronous value factory.</param>
    /// <returns>The same binder with the storage activity appended.</returns>
    public static EventActivityBinder<FutureState> SetVariable<TValue>(this EventActivityBinder<FutureState> binder, string key,
        EventMessageFactory<FutureState, TValue> valueFactory)
        where TValue : class
    {
        ArgumentNullException.ThrowIfNull(binder);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(valueFactory);
        return binder.Add(new ActionActivity<FutureState>(context => context.SetVariable(key, valueFactory)));
    }

    /// <summary>Adds an asynchronous activity that creates and stores a named durable value from an event.</summary>
    /// <typeparam name="TData">The event contract available to the value factory.</typeparam>
    /// <typeparam name="TValue">The stored value type.</typeparam>
    /// <param name="binder">The event binder to extend.</param>
    /// <param name="key">The nonempty variable name.</param>
    /// <param name="valueFactory">The asynchronous value factory.</param>
    /// <returns>The same binder with the storage activity appended.</returns>
    public static EventActivityBinder<FutureState, TData> SetVariableAwaited<TData, TValue>(this EventActivityBinder<FutureState, TData> binder, string key,
        AsyncEventMessageFactory<FutureState, TData, TValue> valueFactory)
        where TData : class
        where TValue : class
    {
        ArgumentNullException.ThrowIfNull(binder);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(valueFactory);
        return binder.Add(new AsyncActivity<FutureState, TData>(context => context.SetVariableAsync(key, valueFactory)));
    }

    /// <summary>Adds an asynchronous activity that creates and stores a named durable value from future state.</summary>
    /// <typeparam name="TValue">The stored value type.</typeparam>
    /// <param name="binder">The event binder to extend.</param>
    /// <param name="key">The nonempty variable name.</param>
    /// <param name="valueFactory">The asynchronous value factory.</param>
    /// <returns>The same binder with the storage activity appended.</returns>
    public static EventActivityBinder<FutureState> SetVariableAwaited<TValue>(this EventActivityBinder<FutureState> binder, string key,
        AsyncEventMessageFactory<FutureState, TValue> valueFactory)
        where TValue : class
    {
        ArgumentNullException.ThrowIfNull(binder);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(valueFactory);
        return binder.Add(new AsyncActivity<FutureState>(context => context.SetVariableAsync(key, valueFactory)));
    }

    /// <summary>Stores a named durable value in future state.</summary>
    /// <typeparam name="TValue">The stored value type.</typeparam>
    /// <param name="context">The future state context that receives the value.</param>
    /// <param name="key">The nonempty variable name.</param>
    /// <param name="value">The value to store.</param>
    public static void SetVariable<TValue>(this BehaviorContext<FutureState> context, string key, TValue value)
        where TValue : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);
        context.Saga.Variables[key] = value;
    }

    /// <summary>Attempts to read and convert a named durable value from future state.</summary>
    /// <typeparam name="T">The requested value type.</typeparam>
    /// <param name="context">The future state context that contains the variables.</param>
    /// <param name="key">The nonempty variable name.</param>
    /// <param name="result">Receives the converted value when found.</param>
    /// <returns><see langword="true" /> when the value exists and can be converted; otherwise, <see langword="false" />.</returns>
    public static bool TryGetVariable<T>(this BehaviorContext<FutureState> context, string key, [NotNullWhen(true)] out T? result)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (context.Saga.HasVariables())
        {
            try
            {
                return context.SerializerContext.TryGetValue(context.Saga.Variables, key, out result);
            }
            catch (Exception exception) when (IsConversionFailure(exception))
            {
                result = default;
                return false;
            }
        }

        result = default;
        return false;
    }

    static bool IsConversionFailure(Exception exception)
    {
        return exception is not OperationCanceledException
            and not OutOfMemoryException
            and not StackOverflowException
            and not AccessViolationException;
    }
}
