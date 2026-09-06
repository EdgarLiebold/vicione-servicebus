using System.Threading.Tasks;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.Futures;

/// <summary>Provides extension methods for future variable.</summary>
public static class FutureVariableExtensions
{
    /// <summary>Sets variable.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TValue">The value stored by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="factory">The factory invoked by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the set variable outcome.</returns>
    public static async Task<TValue> SetVariableAsync<T, TValue>(this BehaviorContext<FutureState, T> context, string key,
        AsyncEventMessageFactory<FutureState, T, TValue> factory, CancellationToken cancellationToken = default)
        where T : class
        where TValue : class
    {
        cancellationToken.ThrowIfCancellationRequested(); var value = await factory(context).ConfigureAwait(false);

        context.Saga.Variables[key] = value;

        return value;
    }

    /// <summary>Sets variable.</summary>
    /// <typeparam name="TValue">The value stored by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="factory">The factory invoked by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the set variable outcome.</returns>
    public static async Task<TValue> SetVariableAsync<TValue>(this BehaviorContext<FutureState> context, string key,
        AsyncEventMessageFactory<FutureState, TValue> factory, CancellationToken cancellationToken = default)
        where TValue : class
    {
        cancellationToken.ThrowIfCancellationRequested(); var value = await factory(context).ConfigureAwait(false);

        context.Saga.Variables[key] = value;

        return value;
    }

    /// <summary>Sets variable.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TValue">The value stored by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="factory">The factory invoked by the operation.</param>
    /// <returns>The t value produced by the operation.</returns>
    public static TValue SetVariable<T, TValue>(this BehaviorContext<FutureState, T> context, string key,
        EventMessageFactory<FutureState, T, TValue> factory)
        where T : class
        where TValue : class
    {
        var value = factory(context);

        context.Saga.Variables[key] = value;

        return value;
    }

    /// <summary>Sets variable.</summary>
    /// <typeparam name="TValue">The value stored by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="factory">The factory invoked by the operation.</param>
    /// <returns>The t value produced by the operation.</returns>
    public static TValue SetVariable<TValue>(this BehaviorContext<FutureState> context, string key, EventMessageFactory<FutureState, TValue> factory)
        where TValue : class
    {
        var value = factory(context);

        context.Saga.Variables[key] = value;

        return value;
    }

    /// <summary>Sets variable.</summary>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TValue">The value stored by the member.</typeparam>
    /// <param name="binder">The binder.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="valueFactory">The value factory.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static EventActivityBinder<FutureState, TData> SetVariable<TData, TValue>(this EventActivityBinder<FutureState, TData> binder, string key,
        EventMessageFactory<FutureState, TData, TValue> valueFactory)
        where TData : class
        where TValue : class
    {
        return binder.Add(new ActionActivity<FutureState, TData>(context => context.SetVariable(key, valueFactory)));
    }

    /// <summary>Sets variable.</summary>
    /// <typeparam name="TValue">The value stored by the member.</typeparam>
    /// <param name="binder">The binder.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="valueFactory">The value factory.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static EventActivityBinder<FutureState> SetVariable<TValue>(this EventActivityBinder<FutureState> binder, string key,
        EventMessageFactory<FutureState, TValue> valueFactory)
        where TValue : class
    {
        return binder.Add(new ActionActivity<FutureState>(context => context.SetVariable(key, valueFactory)));
    }

    /// <summary>Sets variable.</summary>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TValue">The value stored by the member.</typeparam>
    /// <param name="binder">The binder.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="valueFactory">The value factory.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static EventActivityBinder<FutureState, TData> SetVariable<TData, TValue>(this EventActivityBinder<FutureState, TData> binder, string key,
        AsyncEventMessageFactory<FutureState, TData, TValue> valueFactory)
        where TData : class
        where TValue : class
    {
        return binder.Add(new AsyncActivity<FutureState, TData>(context => context.SetVariableAsync(key, valueFactory)));
    }

    /// <summary>Sets variable.</summary>
    /// <typeparam name="TValue">The value stored by the member.</typeparam>
    /// <param name="binder">The binder.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="valueFactory">The value factory.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static EventActivityBinder<FutureState> SetVariable<TValue>(this EventActivityBinder<FutureState> binder, string key,
        AsyncEventMessageFactory<FutureState, TValue> valueFactory)
        where TValue : class
    {
        return binder.Add(new AsyncActivity<FutureState>(context => context.SetVariableAsync(key, valueFactory)));
    }

    /// <summary>Sets variable.</summary>
    /// <typeparam name="TValue">The value stored by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="value">The value to process.</param>
    public static void SetVariable<TValue>(this BehaviorContext<FutureState> context, string key, TValue value)
        where TValue : class
    {
        context.Saga.Variables[key] = value;
    }

    /// <summary>Attempts to get variable.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="result">Receives the result produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public static bool TryGetVariable<T>(this BehaviorContext<FutureState> context, string key, [NotNullWhen(true)] out T? result)
        where T : class
    {
        if (context.Saga.HasVariables())
            return context.SerializerContext.TryGetValue(context.Saga.Variables, key, out result);

        result = default;
        return false;
    }
}
